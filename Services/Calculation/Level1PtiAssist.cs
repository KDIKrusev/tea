using KSailCalc.Api.Models;
using KSailCalc.Api.Services.Helpers;

namespace KSailCalc.Api.Services.Calculation;

/// <summary>
/// PTI (Power Take-In) propulsion assist — the shaft motor covering a main-engine deficit with power
/// from the auxiliary side.
///
/// Separated from <see cref="Level1OptimizationService"/> because it is the most specialised physics
/// in Level 1: it has its own Excel provenance (I4 transmission loss), its own decisions (D5 union
/// model, D-C2 relaxed direction exclusivity) and its own opt-in switch
/// (<see cref="CalculatorInput.MaxPtiPerEngineKw"/> = 0 ⇒ not modelled at all).
/// </summary>
internal static class Level1PtiAssist
{
    /// <summary>
    /// When the assigned ME power exceeds ME capacity and PTI is configured, cover the deficit
    /// with shaft motors: PTI power moves to the aux side grossed up by the transmission loss
    /// (Excel I4, default 5%). Returns false when the combination remains infeasible.
    ///
    /// PTI capacity spans ALL INSTALLED machines (Increment G, decision D5 — union model):
    /// the Excel allows PTI only on idle engines' machines (N-column: electric drive of the
    /// non-running shaft, e.g. row 59), while running-shaft boost is real marine practice —
    /// the union covers both physics. Direction exclusivity stays relaxed in aggregate (D-C2).
    ///
    /// Returns the combination with the PTI outcome applied — the available headroom always, plus
    /// the assist and the redistributed ME/AE load when a deficit was covered — or <c>null</c> when
    /// the combination remains infeasible.
    /// </summary>
    internal static EngineCombination? TryApply(
        EngineCombination combo, CalculatorInput input, double ptiLossFactor)
    {
        var ptiCapacity = input.MeCount * input.MaxPtiPerEngineKw;
        var withHeadroom = combo with { AvailablePtiKw = ptiCapacity };

        if (combo.ActiveMeCount == 0 || ptiCapacity <= 0)
            return withHeadroom; // PTI not modelled / no engaged shaft — existing checks decide

        var meCapacity = combo.ActiveMeCount * input.MeCapacityPerEngine;
        var deficit = combo.MePowerKw - meCapacity;
        if (deficit <= PlantLimits.PowerToleranceKw)
            return withHeadroom; // ME carries everything; full PTI capacity stays available

        if (deficit > ptiCapacity + PlantLimits.PowerToleranceKw)
            return null; // "Insufficient pwr" even with full PTI

        // Move the deficit to the aux side with transmission losses
        var ptiAuxLoad = deficit * (1 + ptiLossFactor);
        var aeCapacity = combo.ActiveAeCount * input.AeCapacityPerEngine;
        var newAePower = combo.AePowerKw + ptiAuxLoad;
        if (combo.ActiveAeCount == 0 || newAePower > aeCapacity + PlantLimits.PowerToleranceKw)
            return null; // no aux headroom to feed the PTI motor

        return combo with
        {
            PtiPowerKw = deficit,
            AvailablePtiKw = ptiCapacity - deficit,
            MePowerKw = meCapacity,
            AePowerKw = newAePower,
            MeLoadPercent = CalculationHelpers.LoadPercent(meCapacity, meCapacity),
            AeLoadPercent = CalculationHelpers.LoadPercent(newAePower, aeCapacity)
        };
    }
}
