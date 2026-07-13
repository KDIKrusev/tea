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
public class HybridConfigRepository : BaseRepository, IKSailCalcConfigRepository
{
    private Dictionary<EngineCategory, List<EngineType>>? _cachedEngines;
    private List<IntegrationLevelConfig>? _cachedIntegrationLevels;
    private List<VesselType>? _cachedVesselTypes;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public HybridConfigRepository(IConfiguration configuration) : base(configuration) { }

    public void ClearCache()
    {
        _cachedEngines = null;
        _cachedIntegrationLevels = null;
        _cachedVesselTypes = null;
    }

    #region Integration Levels

    /// <summary>
    /// Get all integration levels from IntegrationLevel table
    /// Pure relational query - no JSON parsing needed
    /// </summary>
    public async Task<List<IntegrationLevelConfig>> GetIntegrationLevelConfigsAsync()
    {
        if (_cachedIntegrationLevels != null)
            return _cachedIntegrationLevels;

        await _loadLock.WaitAsync();
        try
        {
            if (_cachedIntegrationLevels != null)
                return _cachedIntegrationLevels;

            var levels = new List<IntegrationLevelConfig>();

            using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                SELECT IntegrationLevelId, LevelName, BaseEfficiencyFactor, 
                       IemsPriceNOK, CommissioningNOK, Description, IsActive
                FROM IntegrationLevel
                WHERE IsActive = 1
                ORDER BY IntegrationLevelId", connection);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                levels.Add(new IntegrationLevelConfig
                {
                    IntegrationLevelId = reader.GetInt32(0),
                    LevelName = reader.GetString(1),
                    BaseEfficiencyFactor = (double)reader.GetDecimal(2),
                    IemsPriceNOK = (double)reader.GetDecimal(3),
                    CommissioningNOK = (double)reader.GetDecimal(4),
                    Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                    IsActive = reader.GetBoolean(6)
                });
            }

            _cachedIntegrationLevels = levels;
            return levels;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    #endregion

    #region Engine Types

    /// <summary>
    /// Get all engines from EngineType table, cached by category
    /// </summary>
    private async Task<Dictionary<EngineCategory, List<EngineType>>> LoadAllEnginesAsync()
    {
        if (_cachedEngines != null)
            return _cachedEngines;

        await _loadLock.WaitAsync();
        try
        {
            if (_cachedEngines != null)
                return _cachedEngines;

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
                var sfocJson = reader.GetString(6);
                var sfocData = JsonSerializer.Deserialize<List<SfocDataPoint>>(sfocJson, JsonOptions) 
                    ?? new List<SfocDataPoint>();

                var engine = new EngineType
                {
                    Id = reader.GetInt32(0),
                    EngineCategory = Enum.Parse<EngineCategory>(reader.GetString(1)),
                    Name = reader.GetString(2),
                    MaxCapacityKW = reader.GetDecimal(3),
                    ShaftGeneratorMaxCapacityKW = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                    Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                    SfocData = sfocData,
                    IsActive = reader.GetBoolean(7),
                    FuelFamily = reader.IsDBNull(8) ? null : reader.GetString(8),
                    Maker = reader.IsDBNull(9) ? null : reader.GetString(9),
                    Series = reader.IsDBNull(10) ? null : reader.GetString(10),
                    RatedPowerKW = reader.IsDBNull(11) ? null : reader.GetDecimal(11),
                    Rpm = reader.IsDBNull(12) ? null : reader.GetInt32(12),
                    NoxTier = reader.IsDBNull(13) ? null : reader.GetString(13)
                };

                var category = engine.EngineCategory;
                if (engines.TryGetValue(category, out var list))
                    list.Add(engine);
            }

            _cachedEngines = engines;
            return engines;
        }
        finally
        {
            _loadLock.Release();
        }
    }

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
    public async Task<List<VesselType>> GetVesselTypesAsync()
    {
        if (_cachedVesselTypes != null)
            return _cachedVesselTypes;

        await _loadLock.WaitAsync();
        try
        {
            if (_cachedVesselTypes != null)
                return _cachedVesselTypes;

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
                var speedPowerJson = reader.GetString(11);
                var speedPowerCurve = JsonSerializer.Deserialize<List<SpeedPowerPoint>>(speedPowerJson, JsonOptions)
                    ?? new List<SpeedPowerPoint>();

                var vesselTypeName = reader.GetString(1);
                var sizeCategory = reader.IsDBNull(3) ? null : reader.GetString(3);
                
                var operationalProfileJson = reader.GetString(13);
                var operationalProfile = JsonSerializer.Deserialize<VesselOperationalProfile>(operationalProfileJson, JsonOptions);
                
                // Populate VesselTypeName and SizeCategory which are not in JSON
                if (operationalProfile != null)
                {
                    operationalProfile.VesselTypeName = vesselTypeName;
                    operationalProfile.SizeCategory = sizeCategory ?? string.Empty;
                }

                var vessel = new VesselType
                {
                    Id = reader.GetInt32(0),
                    VesselTypeName = vesselTypeName,
                    Category = reader.IsDBNull(2) ? null : reader.GetString(2),
                    SizeCategory = sizeCategory,
                    Unit = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                    MainEngine = new VesselEngineConfig
                    {
                        EngineTypeId = reader.GetInt32(6),
                        NumberOfEngines = reader.GetInt32(7),
                        ShaftGeneratorMaxCapacityKW = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8)
                    },
                    AuxEngine = new VesselAuxEngineConfig
                    {
                        EngineTypeId = reader.GetInt32(9),
                        NumberOfEngines = reader.GetInt32(10)
                    },
                    SpeedPowerCurve = speedPowerCurve,
                    SeaMarginPercent = reader.GetDecimal(12),
                    OperationalProfile = operationalProfile,
                    IsActive = reader.GetBoolean(14),
                    ReferenceSize = reader.IsDBNull(15) ? null : reader.GetDecimal(15),
                    MinSize = reader.IsDBNull(16) ? null : reader.GetDecimal(16),
                    MaxSize = reader.IsDBNull(17) ? null : reader.GetDecimal(17)
                };

                vessels.Add(vessel);
            }

            _cachedVesselTypes = vessels;
            return vessels;
        }
        finally
        {
            _loadLock.Release();
        }
    }

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
}
