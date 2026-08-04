using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace KSailCalc.Api.Repositories;

/// <summary>
/// Repository for hybrid relational schema (EngineType, IntegrationLevel, VesselType tables)
/// 
/// Relational structure:
/// - EngineType table (Main + Auxiliary combined)
/// - IntegrationLevel table (pure relational, no JSON)
/// - VesselType table (with FK to engines, JSON for profiles/curves)
/// 
/// Caches data in-memory for the lifetime of the scoped service.
/// </summary>
public sealed class HybridConfigRepository : BaseRepository, IKSailCalcConfigRepository, IDisposable
{
    private Dictionary<EngineCategory, List<EngineType>>? _cachedEngines;
    private List<IntegrationLevelConfig>? _cachedIntegrationLevels;
    private List<VesselType>? _cachedVesselTypes;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public HybridConfigRepository(IConfiguration configuration) : base(configuration) { }

    public void ClearCache()
    {
        // Under the same lock the loaders take: this is a singleton, so a clear can otherwise race
        // a load in flight and leave a half-populated cache behind.
        _loadLock.Wait();
        try
        {
            _cachedEngines = null;
            _cachedIntegrationLevels = null;
            _cachedVesselTypes = null;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>
    /// Double-checked lock around a cached load: return the cache if it is populated, otherwise take
    /// the lock, re-check (another caller may have filled it while we waited) and load.
    ///
    /// The three loaders used to spell this out individually, which is three chances to get the
    /// re-check wrong. The cache field is assigned only after a successful load, so a failed load
    /// leaves the cache empty and is retried — same as before.
    /// </summary>
    private async Task<T> GetOrLoadAsync<T>(Func<T?> read, Action<T> store, Func<Task<T>> load)
        where T : class
    {
        var cached = read();
        if (cached != null)
            return cached;

        await _loadLock.WaitAsync();
        try
        {
            cached = read();
            if (cached != null)
                return cached;

            var loaded = await load();
            store(loaded);
            return loaded;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    #region Integration Levels

    /// <summary>
    /// Get all integration levels from IntegrationLevel table
    /// Pure relational query - no JSON parsing needed
    /// </summary>
    public Task<List<IntegrationLevelConfig>> GetIntegrationLevelConfigsAsync() => GetOrLoadAsync(
        () => _cachedIntegrationLevels,
        loaded => _cachedIntegrationLevels = loaded,
        async () =>
        {
            var levels = new List<IntegrationLevelConfig>();

            using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                SELECT IntegrationLevelId, LevelName,
                       IemsPriceNOK, CommissioningNOK, Description, IsActive
                FROM IntegrationLevel
                WHERE IsActive = 1
                ORDER BY IntegrationLevelId", connection);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                levels.Add(new IntegrationLevelConfig
                {
                    IntegrationLevelId = reader.GetInt32("IntegrationLevelId"),
                    LevelName = reader.GetString("LevelName"),
                    IemsPriceNOK = (double)reader.GetDecimal("IemsPriceNOK"),
                    CommissioningNOK = (double)reader.GetDecimal("CommissioningNOK"),
                    Description = reader.GetStringOrNull("Description"),
                    IsActive = reader.GetBoolean("IsActive")
                });
            }

            return levels;
        });

    #endregion

    #region Engine Types

    /// <summary>
    /// Get all engines from EngineType table, cached by category
    /// </summary>
    private Task<Dictionary<EngineCategory, List<EngineType>>> LoadAllEnginesAsync() => GetOrLoadAsync(
        () => _cachedEngines,
        loaded => _cachedEngines = loaded,
        async () =>
        {
            var engines = new Dictionary<EngineCategory, List<EngineType>>
            {
                [EngineCategory.Main] = new List<EngineType>(),
                [EngineCategory.Auxiliary] = new List<EngineType>()
            };

            using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                SELECT EngineTypeId, EngineCategory, Name, MaxCapacityKW,
                       ShaftGeneratorMaxCapacityKW, Description, SfocDataJson, IsActive,
                       FuelFamily, Maker, Series, RatedPowerKW, Rpm, NoxTier
                FROM EngineType
                WHERE IsActive = 1
                ORDER BY EngineCategory, EngineTypeId", connection);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var sfocJson = reader.GetString("SfocDataJson");
                var sfocData = JsonSerializer.Deserialize<List<SfocDataPoint>>(sfocJson, JsonOptions)
                    ?? new List<SfocDataPoint>();

                var engine = new EngineType
                {
                    Id = reader.GetInt32("EngineTypeId"),
                    EngineCategory = Enum.Parse<EngineCategory>(reader.GetString("EngineCategory")),
                    Name = reader.GetString("Name"),
                    MaxCapacityKW = reader.GetDecimal("MaxCapacityKW"),
                    ShaftGeneratorMaxCapacityKW = reader.GetDecimalOrNull("ShaftGeneratorMaxCapacityKW"),
                    Description = reader.GetStringOrNull("Description"),
                    SfocData = sfocData,
                    IsActive = reader.GetBoolean("IsActive"),
                    FuelFamily = reader.GetStringOrNull("FuelFamily"),
                    Maker = reader.GetStringOrNull("Maker"),
                    Series = reader.GetStringOrNull("Series"),
                    RatedPowerKW = reader.GetDecimalOrNull("RatedPowerKW"),
                    Rpm = reader.GetInt32OrNull("Rpm"),
                    NoxTier = reader.GetStringOrNull("NoxTier")
                };

                var category = engine.EngineCategory;
                if (engines.TryGetValue(category, out var list))
                    list.Add(engine);
            }

            return engines;
        });

    /// <summary>
    /// Get main engines (EngineCategory = 'Main')
    /// </summary>
    public async Task<List<EngineType>> GetMainEnginesAsync()
    {
        var engines = await LoadAllEnginesAsync();
        return engines[EngineCategory.Main];
    }

    /// <summary>
    /// Get auxiliary engines (EngineCategory = 'Auxiliary')
    /// Converts to AuxiliaryEngineType for backward compatibility
    /// </summary>
    public async Task<List<AuxiliaryEngineType>> GetAuxiliaryEnginesAsync()
    {
        var engines = await LoadAllEnginesAsync();
        return engines[EngineCategory.Auxiliary].Select(e => new AuxiliaryEngineType
        {
            Id = e.Id,
            Name = e.Name,
            MaxCapacityKW = e.MaxCapacityKW,
            Description = e.Description,
            SfocData = e.SfocData,
            IsActive = e.IsActive,
            FuelFamily = e.FuelFamily,
            Maker = e.Maker,
            Series = e.Series,
            RatedPowerKW = e.RatedPowerKW
        }).ToList();
    }

    #endregion

    #region Vessel Types

    /// <summary>
    /// Get all vessel types from VesselType table
    /// Deserializes JSON for SpeedPowerCurve and OperationalProfile
    /// </summary>
    public Task<List<VesselType>> GetVesselTypesAsync() => GetOrLoadAsync(
        () => _cachedVesselTypes,
        loaded => _cachedVesselTypes = loaded,
        async () =>
        {
            var vessels = new List<VesselType>();

            using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                SELECT VesselTypeId, VesselTypeName, Category, SizeCategory, Unit, Description,
                       MainEngineTypeId, NumberOfMainEngines, MainEngineShaftGeneratorKW,
                       AuxEngineTypeId, NumberOfAuxEngines,
                       SpeedPowerCurveJson, SeaMarginPercent, OperationalProfileJson, IsActive,
                       ReferenceSize, MinSize, MaxSize
                FROM VesselType
                WHERE IsActive = 1
                ORDER BY VesselTypeId", connection);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var speedPowerJson = reader.GetString("SpeedPowerCurveJson");
                var speedPowerCurve = JsonSerializer.Deserialize<List<SpeedPowerPoint>>(speedPowerJson, JsonOptions)
                    ?? new List<SpeedPowerPoint>();

                var vesselTypeName = reader.GetString("VesselTypeName");
                var sizeCategory = reader.GetStringOrNull("SizeCategory");

                var operationalProfileJson = reader.GetString("OperationalProfileJson");
                var operationalProfile = JsonSerializer.Deserialize<VesselOperationalProfile>(operationalProfileJson, JsonOptions);
                
                // Populate VesselTypeName and SizeCategory which are not in JSON
                if (operationalProfile != null)
                {
                    operationalProfile.VesselTypeName = vesselTypeName;
                    operationalProfile.SizeCategory = sizeCategory ?? string.Empty;
                }

                var vessel = new VesselType
                {
                    Id = reader.GetInt32("VesselTypeId"),
                    VesselTypeName = vesselTypeName,
                    Category = reader.GetStringOrNull("Category"),
                    SizeCategory = sizeCategory,
                    Unit = reader.GetStringOrNull("Unit"),
                    Description = reader.GetStringOrNull("Description"),
                    MainEngine = new VesselEngineConfig
                    {
                        EngineTypeId = reader.GetInt32("MainEngineTypeId"),
                        NumberOfEngines = reader.GetInt32("NumberOfMainEngines"),
                        ShaftGeneratorMaxCapacityKW = reader.GetDecimalOrNull("MainEngineShaftGeneratorKW") ?? 0
                    },
                    AuxEngine = new VesselAuxEngineConfig
                    {
                        EngineTypeId = reader.GetInt32("AuxEngineTypeId"),
                        NumberOfEngines = reader.GetInt32("NumberOfAuxEngines")
                    },
                    SpeedPowerCurve = speedPowerCurve,
                    SeaMarginPercent = reader.GetDecimal("SeaMarginPercent"),
                    OperationalProfile = operationalProfile,
                    IsActive = reader.GetBoolean("IsActive"),
                    ReferenceSize = reader.GetDecimalOrNull("ReferenceSize"),
                    MinSize = reader.GetDecimalOrNull("MinSize"),
                    MaxSize = reader.GetDecimalOrNull("MaxSize")
                };

                vessels.Add(vessel);
            }

            return vessels;
        });

    /// <summary>
    /// Get operational profiles (extracted from VesselType.OperationalProfile)
    /// </summary>
    public async Task<List<VesselOperationalProfile>> GetOperationalProfilesAsync()
    {
        var vessels = await GetVesselTypesAsync();
        return vessels
            .Select(v => v.OperationalProfile)
            .Where(p => p != null)
            .Select(p => p!)
            .ToList();
    }

    #endregion

    /// <summary>The load lock is owned by this repository, so it is disposed with it.</summary>
    public void Dispose() => _loadLock.Dispose();
}
