using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Catalog;

/// <summary>
/// Builds the answer to <c>GET /api/app-data/vessel-config</c>: one resolved vessel with its
/// operational profile, both engine records, and the audit trace explaining how the parametric
/// request was resolved.
///
/// Synchronous on purpose — everything it needs is already in the cached app data. It used to
/// return a <c>Task</c> built from <c>Task.FromResult</c>, which said "I do I/O" when it does not.
/// </summary>
internal static class FullVesselDataBuilder
{
    internal static FullVesselData? Build(
        AppInitialData appData, VesselResolution resolution, ILogger logger)
    {
        // Clone before touching it: the resolved record belongs to the shared cache, and writing the
        // interpolated power onto it would leak into concurrent requests.
        var vesselConfig = CloneVessel(resolution.BucketRecord);
        vesselConfig.CalmWaterPowerKW = resolution.CalmWaterPowerKW;

        var operationalProfile = appData.OperationalProfiles
            .FirstOrDefault(p => p.VesselTypeName == vesselConfig.VesselTypeName);

        if (operationalProfile == null)
        {
            logger.LogWarning("No operational profile found for {VesselName}", vesselConfig.VesselTypeName);
            return null;
        }

        return new FullVesselData
        {
            VesselConfig = vesselConfig,
            OperationalProfile = operationalProfile,
            MainEngineData = vesselConfig.MainEngine is null ? null
                : appData.EngineTypes.MainEngines.FirstOrDefault(e => e.Id == vesselConfig.MainEngine.EngineTypeId),
            AuxEngineData = vesselConfig.AuxEngine is null ? null
                : appData.EngineTypes.AuxiliaryEngines.FirstOrDefault(e => e.Id == vesselConfig.AuxEngine.EngineTypeId),
            Resolution = resolution.Info
        };
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
}
