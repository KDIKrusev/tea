using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Results;

/// <summary>
/// The "before iEMS" figures every panel is measured against: annual fuel and CO2, split main vs
/// auxiliary. The client shows them inside the tier cards and the comparison chart as the baseline
/// each variant improves on.
///
/// Per-engine CO2 rather than one total, because ME and AE may burn different fuels — the split is
/// computed here so the client never has to recompute it (and cannot get it wrong).
/// </summary>
internal static class BaselineFiguresBuilder
{
    /// <summary>Writes the baseline block onto the response.</summary>
    internal static void ApplyTo(
        AllVariantsCalculationResult result, FocBreakdown foc,
        CalculatorInput input, CalculatorSettings settings)
    {
        result.BaselineFOC = foc.BaselineFoc;
        result.BaselineME = foc.BaselineMeFoc;
        result.BaselineAE = foc.BaselineAeFoc;
        result.BaselineCO2 = Co2Calculator.ForEngines(foc.BaselineMeFoc, foc.BaselineAeFoc, input, settings);
        result.BaselineMeCO2 = Co2Calculator.ForMainEngines(foc.BaselineMeFoc, input, settings);
        result.BaselineAeCO2 = Co2Calculator.ForAuxEngines(foc.BaselineAeFoc, input, settings);
    }
}
