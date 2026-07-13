using KSailCalc.Api.Models;
using KSailCalc.Api.Repositories.Interfaces;
using KSailCalc.Api.Services.Helpers;
using KSailCalc.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KSailCalc.Api.Services;

/// <summary>
/// Aggregates data from IKSailCalcConfigRepository and provides optimized caching.
/// All config data comes from ONE repository (one DB call).
/// </summary>
public class AppDataAggregationService : IAppDataAggregationService
{
    private readonly IKSailCalcConfigRepository _configRepository;
    private readonly IVesselResolutionService _resolutionService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AppDataAggregationService> _logger;
    private readonly CalculatorSettings _calculatorSettings;

    private const string CACHE_KEY = "AppInitialData";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24);

    public AppDataAggregationService(
        IKSailCalcConfigRepository configRepository,
        IVesselResolutionService resolutionService,
        IMemoryCache cache,
        ILogger<AppDataAggregationService> logger,
        IOptions<CalculatorSettings> calculatorSettings)
    {
        _configRepository = configRepository;
        _resolutionService = resolutionService;
        _cache = cache;
        _logger = logger;
        _calculatorSettings = calculatorSettings.Value;
    }

    public async Task<AppInitialData> GetInitialAppDataAsync()
    {
        if (_cache.TryGetValue(CACHE_KEY, out AppInitialData? cachedData) && cachedData != null)
        {
            _logger.LogDebug("Returning cached app data");
            return cachedData;
        }

        _logger.LogInformation("Loading fresh app data from database");

        // All of these read from the SAME cached Dictionary -- no extra DB calls
        var mainEngines = await _configRepository.GetMainEnginesAsync();
        var auxiliaryEngines = await _configRepository.GetAuxiliaryEnginesAsync();
        var vesselTypes = await _configRepository.GetVesselTypesAsync();
        var operationalProfiles = await _configRepository.GetOperationalProfilesAsync();

        var categories = vesselTypes
            .Where(vt => !string.IsNullOrEmpty(vt.Category))
            .GroupBy(vt => vt.Category!)
            .Select(g =>
            {
                var units = g.Select(vt => vt.Unit).Distinct().ToList();
                if (units.Count > 1)
                    _logger.LogWarning("Category {Category} has mixed units: {Units}",
                        g.Key, string.Join(", ", units));

                var refSizes = g.Where(vt => vt.ReferenceSize.HasValue)
                    .Select(vt => vt.ReferenceSize!.Value).ToList();
                var speeds = g.SelectMany(vt => vt.SpeedPowerCurve)
                    .Select(sp => sp.SpeedKnots).ToList();

                return new VesselCategoryData
                {
                    Name = g.Key,
                    Unit = units.FirstOrDefault() ?? string.Empty,
                    // Reference anchors define the interpolation range; bucket bounds are the fallback
                    SizeMin = refSizes.Count > 0 ? refSizes.Min() : g.Min(vt => vt.MinSize),
                    SizeMax = refSizes.Count > 0 ? refSizes.Max()
                        : (g.Any(vt => !vt.MaxSize.HasValue) ? null : g.Max(vt => vt.MaxSize)),
                    SpeedMin = speeds.Count > 0 ? speeds.Min() : 0,
                    SpeedMax = speeds.Count > 0 ? speeds.Max() : 0
                };
            })
            .OrderBy(c => c.Name)
            .ToList();

        var appData = new AppInitialData
        {
            Categories = categories,
            EngineTypes = new EngineTypesData
            {
                MainEngines = mainEngines,
                AuxiliaryEngines = auxiliaryEngines
            },
            OperationalProfiles = operationalProfiles,
            FuelDefaultPrices = _calculatorSettings.FuelDefaultPrices,
            Metadata = new AppDataMetadata
            {
                Version = "1.0",
                LoadedAt = DateTime.UtcNow,
                VesselTypeCount = vesselTypes.Count,
                MainEngineCount = mainEngines.Count,
                AuxiliaryEngineCount = auxiliaryEngines.Count,
                OperationalProfileCount = operationalProfiles.Count
            }
        };

        _cache.Set(CACHE_KEY, appData, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheExpiration,
            SlidingExpiration = TimeSpan.FromHours(6)
        });

        _logger.LogInformation(
            "App data loaded and cached - {VesselTypes} vessel types in {Categories} categories, {MainEngines} ME, {AuxEngines} AE, {Profiles} profiles",
            vesselTypes.Count, categories.Count, mainEngines.Count, auxiliaryEngines.Count, operationalProfiles.Count);

        return appData;
    }

    public async Task<FullVesselData?> GetFullVesselDataByCategoryAsync(string category, decimal size, decimal speed)
    {
        var cachedAppData = await GetInitialAppDataAsync();

        var resolution = await _resolutionService.ResolveAsync(category, size, speed);
        if (resolution == null)
        {
            _logger.LogWarning("No vessel records found for category {Category}", category);
            return null;
        }

        // Clone to avoid mutating the cached object (race condition with concurrent requests)
        var vesselConfigCopy = CloneVessel(resolution.BucketRecord);
        vesselConfigCopy.CalmWaterPowerKW = resolution.CalmWaterPowerKW;

        return await ComposeFullVesselDataAsync(cachedAppData, vesselConfigCopy, resolution.Info);
    }

    private Task<FullVesselData?> ComposeFullVesselDataAsync(
        AppInitialData cachedAppData, VesselType vesselConfigCopy, ResolutionInfo? resolution)
    {
        var operationalProfile = cachedAppData.OperationalProfiles
            .FirstOrDefault(p => p.VesselTypeName == vesselConfigCopy.VesselTypeName);

        if (operationalProfile == null)
        {
            _logger.LogWarning("No operational profile found for {VesselName}", vesselConfigCopy.VesselTypeName);
            return Task.FromResult<FullVesselData?>(null);
        }

        EngineType? mainEngineData = null;
        AuxiliaryEngineType? auxEngineData = null;

        if (vesselConfigCopy.MainEngine != null)
        {
            mainEngineData = cachedAppData.EngineTypes.MainEngines
                .FirstOrDefault(e => e.Id == vesselConfigCopy.MainEngine.EngineTypeId);
        }

        if (vesselConfigCopy.AuxEngine != null)
        {
            auxEngineData = cachedAppData.EngineTypes.AuxiliaryEngines
                .FirstOrDefault(e => e.Id == vesselConfigCopy.AuxEngine.EngineTypeId);
        }

        return Task.FromResult<FullVesselData?>(new FullVesselData
        {
            VesselConfig = vesselConfigCopy,
            OperationalProfile = operationalProfile,
            MainEngineData = mainEngineData,
            AuxEngineData = auxEngineData,
            Resolution = resolution
        });
    }

    private static VesselType CloneVessel(VesselType source) => new()
    {
        Id = source.Id,
        VesselTypeName = source.VesselTypeName,
        Category = source.Category,
        SizeCategory = source.SizeCategory,
        Unit = source.Unit,
        Description = source.Description,
        IsActive = source.IsActive,
        MainEngine = source.MainEngine,
        AuxEngine = source.AuxEngine,
        SpeedPowerCurve = source.SpeedPowerCurve,
        SeaMarginPercent = source.SeaMarginPercent,
        OperationalProfile = source.OperationalProfile,
        ReferenceSize = source.ReferenceSize,
        MinSize = source.MinSize,
        MaxSize = source.MaxSize
    };

    public void ClearCache()
    {
        _cache.Remove(CACHE_KEY);
        _configRepository.ClearCache();
        _logger.LogInformation("App data and repository caches cleared");
    }
}