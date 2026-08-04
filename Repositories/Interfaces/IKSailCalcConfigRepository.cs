using KSailCalc.Api.Models;

namespace KSailCalc.Api.Repositories.Interfaces;

/// <summary>
/// Repository for KSailCalc configuration data.
/// Reads from relational tables: EngineType, IntegrationLevel, VesselType
/// </summary>
public interface IKSailCalcConfigRepository
{
    Task<List<IntegrationLevelConfig>> GetIntegrationLevelConfigsAsync();
    Task<List<EngineType>> GetMainEnginesAsync();
    Task<List<AuxiliaryEngineType>> GetAuxiliaryEnginesAsync();
    Task<List<VesselType>> GetVesselTypesAsync();
    Task<List<VesselOperationalProfile>> GetOperationalProfilesAsync();
    void ClearCache();
}
