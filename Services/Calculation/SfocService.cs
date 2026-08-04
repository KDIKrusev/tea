using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Helpers;
using KSailCalc.Api.Services.Interfaces;

namespace KSailCalc.Api.Services.Calculation;

/// <summary>
/// Resolves the SFOC curves a calculation needs from the cached engine catalogue.
///
/// It used to also answer "what is the SFOC at this one load?" on demand — a method that re-filtered
/// and re-sorted the whole curve on every call, inside the Level 1 candidate loop, and swallowed any
/// failure into a silent 220 g/kWh. Nothing calls that any more: the levels take a resolved
/// <see cref="EngineFuelCurves"/> and interpolate synchronously, so the lookup happens once per
/// request and a failure surfaces instead of being papered over.
/// </summary>
public class SfocService : ISfocService
{
    private readonly IAppDataAggregationService _appDataAggregationService;
    private readonly ILogger<SfocService> _logger;

    public SfocService(IAppDataAggregationService appDataAggregationService, ILogger<SfocService> logger)
    {
        _appDataAggregationService = appDataAggregationService;
        _logger = logger;
    }

    public async Task<EngineFuelCurves> GetCurvesAsync(CalculatorInput input)
    {
        var appData = await _appDataAggregationService.GetInitialAppDataAsync();

        return new EngineFuelCurves(
            Resolve(appData, EngineCategory.Main, input.MainEngineTypeId),
            Resolve(appData, EngineCategory.Auxiliary, input.AuxEngineTypeId));
    }

    /// <summary>
    /// The engine's working points, ready to interpolate. An empty result is a valid state — every
    /// load then reads the fallback SFOC — but it is worth saying out loud once, because the numbers
    /// that follow will look plausible and be meaningless.
    /// </summary>
    private List<SfocDataPoint> Resolve(AppInitialData appData, EngineCategory category, int engineTypeId)
    {
        var points = WorkingPoints(RawCurve(appData, category, engineTypeId));

        if (points.Count == 0)
        {
            _logger.LogWarning(
                "No SFOC data for {EngineType} engine ID {EngineTypeId} — every load in this calculation " +
                "will use the fallback {Fallback} g/kWh",
                category, engineTypeId, SfocInterpolationHelper.DefaultSfocFallback);
        }

        return points;
    }

    /// <summary>
    /// The engine's stored curve, or null when the category is unknown or no such engine exists.
    /// </summary>
    private static List<SfocDataPoint>? RawCurve(
        AppInitialData appData, EngineCategory engineType, int engineTypeId) => engineType switch
    {
        EngineCategory.Main => appData.EngineTypes.MainEngines
            .FirstOrDefault(e => e.Id == engineTypeId)?.SfocData,
        EngineCategory.Auxiliary => appData.EngineTypes.AuxiliaryEngines
            .FirstOrDefault(e => e.Id == engineTypeId)?.SfocData,
        _ => null
    };

    /// <summary>
    /// Working points only (Load &gt; 0), ascending — the precondition
    /// <see cref="SfocInterpolationHelper.Interpolate"/> documents.
    /// </summary>
    private static List<SfocDataPoint> WorkingPoints(List<SfocDataPoint>? raw)
        => raw is { Count: > 0 }
            ? raw.Where(p => p.Load > 0).OrderBy(x => x.Load).ToList()
            : new List<SfocDataPoint>();
}
