using KSailCalc.Api.Models;
using KSailCalc.Api.Services.Helpers;
using KSailCalc.Api.Services.Interfaces;

namespace KSailCalc.Api.Services.Calculation;

/// <summary>
/// Level 2: finds the optimal AE load distribution to minimize fuel consumption.
/// SG load is fixed from Level 1 (driven by ME shaft — not adjustable).
///
/// This class is the decision flow only — take Level 1's optimum, hand the aux demand to the search,
/// and turn what comes back into a result. The parts it delegates to:
/// <list type="bullet">
///   <item><see cref="Level2LoadSweep"/> — which split across the aux engines is cheapest</item>
///   <item><see cref="Level2SetpointBuilder"/> — what that answer looks like as generator setpoints</item>
/// </list>
///
/// Note on <see cref="Level2Result.Level2FocTonPerHour"/>: it can sit a hair ABOVE
/// <see cref="Level2Result.Level1FocTonPerHour"/>, because the sweep searches a 2% grid that need
/// not contain Level 1's own split. The reported savings are clamped at zero and this raw figure is
/// not on the wire, so the client never sees it. Documented in <c>Level2InvariantTests</c>.
/// </summary>
public class Level2OptimizationService : ILevel2OptimizationService
{
    private const double PowerTolerance = PlantLimits.PowerToleranceKw;

    public Level2Result OptimizeLoadSetpoints(
        Level1Result level1Result, CalculatorInput input, EngineFuelCurves curves)
    {
        var optimal = level1Result.OptimalCombination;
        var sgSetpoint = Level2SetpointBuilder.BuildFixedSgSetpoint(optimal, input, curves);

        var totalAeCount = optimal.ActiveAeCount;
        var demandKw = optimal.AePowerKw;

        // Nothing on the aux side to redistribute — Level 1's answer stands.
        if (totalAeCount == 0 || demandKw < PowerTolerance)
            return Level2SetpointBuilder.BuildPassThroughResult(optimal, sgSetpoint);

        var sfocData = curves.Auxiliary;
        var best = Level2LoadSweep.FindBestDistribution(
            totalAeCount, input.AeCapacityPerEngine, demandKw, sfocData);

        // No split inside the 10–90% window covers the demand — Level 1's answer stands.
        if (best is null)
            return Level2SetpointBuilder.BuildPassThroughResult(optimal, sgSetpoint);

        var savingsPerHour = Math.Max(0, optimal.AeFocTonPerHour - best.TotalFoc);
        var setpoints = Level2SetpointBuilder.BuildSetpoints(
            sgSetpoint, best, totalAeCount, input.AeCapacityPerEngine, sfocData);

        return new Level2Result
        {
            OptimalSetpoints = setpoints,
            OptimalTotalSfoc = setpoints.Sum(s => s.Sfoc),
            Level1FocTonPerHour = optimal.FocTonPerHour,
            Level2FocTonPerHour = optimal.MeFocTonPerHour + best.TotalFoc,
            Level2SavingsTonPerHour = savingsPerHour
        };
    }
}
