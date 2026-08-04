using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Services.Interfaces;

public interface ILevel1OptimizationService
{
    /// <summary>
    /// Evaluate all valid ME/SG/AE ON/OFF combinations for the given mode and
    /// return the optimal (lowest FOC) and baseline configurations.
    /// Default baseline: highest-FOC combination; with a battery adjustment: third-highest (D1).
    /// </summary>
    /// <param name="curves">
    /// Pre-resolved SFOC curves. Passing them in keeps the candidate loop free of awaits and of the
    /// per-call filtering and sorting the async lookup used to repeat for every combination.
    /// </param>
    /// <param name="batteryAdjustment">
    /// Uncovered spinning reserve added to the mode loads (battery active for this mode).
    /// Non-null also switches the default baseline to sorted[max(0, Count−3)].
    /// </param>
    Level1Result FindOptimalCombination(CalculatorInput input, EngineFuelCurves curves, OperationalMode mode,
        double? overridePropulsionKw = null, int? baselineIndex = null,
        BatteryL1Adjustment? batteryAdjustment = null);
}
