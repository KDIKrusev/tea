using KSailCalc.Api.Models;
using KSailCalc.Api.Repositories.Interfaces;
using KSailCalc.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KSailCalc.Api.Services.Catalog;

/// <summary>
/// The catalogue cache: reads the reference data once, keeps it for 24 hours, and serves the two
/// endpoints the client's pickers need.
///
/// This class owns the caching and the assembly order only. What each answer LOOKS like belongs to
/// its own builder:
/// <list type="bullet">
///   <item><see cref="VesselCategoryBuilder"/> — the category list with its size/speed bounds</item>
///   <item><see cref="FullVesselDataBuilder"/> — one resolved vessel with profile and engines</item>
/// </list>
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

        var categories = VesselCategoryBuilder.Build(vesselTypes, _logger);

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

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "App data loaded and cached - {VesselTypes} vessel types in {Categories} categories, {MainEngines} ME, {AuxEngines} AE, {Profiles} profiles",
                vesselTypes.Count, categories.Count, mainEngines.Count, auxiliaryEngines.Count, operationalProfiles.Count);
        }

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

        return FullVesselDataBuilder.Build(cachedAppData, resolution, _logger);
    }

    public void ClearCache()
    {
        _cache.Remove(CACHE_KEY);
        _configRepository.ClearCache();
        _logger.LogInformation("App data and repository caches cleared");
    }
}
