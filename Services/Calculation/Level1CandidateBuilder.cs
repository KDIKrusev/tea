using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Helpers;

namespace KSailCalc.Api.Services.Calculation;

/// <summary>
/// What the plant *could* be switched to, and what each of those states costs in load.
///
/// Two steps that belong together and nowhere else: enumerate every ME/SG/AE on-off combination,
/// then decide per combination whether it is structurally feasible and, if so, how the mode's
/// propulsion and hotel demand distribute over its engines.
///
/// Split out of <see cref="Level1OptimizationService"/> so the optimizer reads as
/// "generate → filter → cost → rank" instead of carrying the plant rules inline.
/// </summary>
internal static class Level1CandidateBuilder
{
    /// <summary>
    /// Every combination of:  ME: 0..N  ×  SG: off/on  ×  AE: 0..N
    /// Example: 2 ME, SG, 3 AE → (0,1,2) × (off,on) × (0,1,2,3) = 24 combinations
    /// </summary>
    internal static List<EngineCombination> Generate(CalculatorInput input)
    {
        var combinations = new List<EngineCombination>();

        for (int me = 0; me <= input.MeCount; me++)
            foreach (bool sg in new[] { false, true })
                for (int ae = 0; ae <= input.AeCount; ae++)
                    combinations.Add(new EngineCombination
                    {
                        ActiveMeCount = me,
                        SgEnabled = sg,
                        ActiveAeCount = ae
                    }
            );

        return combinations;
    }

    /// <summary>
    /// Decide whether a candidate is structurally feasible and, if it is, return a copy of it with
    /// the mode's loads distributed over its engines.
    ///
    /// Validity and distribution are one operation because they read the same four quantities
    /// (SG/AE capacity, SG/AE power). Splitting them meant computing that arithmetic twice per
    /// candidate and maintaining it in two places.
    ///
    /// Returns <c>null</c> for every structural rejection — the caller counts them all as
    /// <see cref="Level1RejectionTally.Structural"/>. Power sufficiency, aux overload and the
    /// battery PTI gate are decided by the caller after PTI assist has run, because they depend on
    /// its outcome.
    /// </summary>
    internal static EngineCombination? TryDistribute(
        EngineCombination combo, CalculatorInput input, OperationalMode mode,
        double propulsion, double hotel)
    {
        // ME=0 invalid for Transit/Maneuvering (vessel needs ME for propulsion)
        if ((mode == OperationalMode.Transit || mode == OperationalMode.Maneuvering) && combo.ActiveMeCount == 0)
            return null;

        // An installed shaft generator is always run (observation #1: this also forces an ME to
        // run in Port/Anchor to spin it — deliberate for now, logged in the QA scenarios' README).
        if (input.TotalSgCapacity > 0 && !combo.SgEnabled)
            return null;
        // SG ON without ME → physically impossible (SG is driven by ME shaft)
        if (combo.SgEnabled && combo.ActiveMeCount == 0)
            return null;

        // SG ON but SG capacity is 0 → meaningless, skip to avoid duplicate combos
        if (combo.SgEnabled && input.SgCapacityPerEngine <= 0)
            return null;

        // SG OFF but SG=ON variant for the same ME count would be valid →
        // operationally unrealistic (no reason to use AE when SG handles everything
        // and ME can absorb the additional shaft load)
        if (!combo.SgEnabled && input.SgCapacityPerEngine > 0 && combo.ActiveMeCount > 0)
        {
            var potentialSgCapacity = combo.ActiveMeCount * input.SgCapacityPerEngine;
            var potentialSgPower = Math.Min(hotel, potentialSgCapacity);
            var meCapacityForSg = combo.ActiveMeCount * input.MeCapacityPerEngine;
            // SG covers full hotel AND ME can handle propulsion + SG shaft load
            if (potentialSgCapacity >= hotel - PlantLimits.PowerToleranceKw && propulsion + potentialSgPower <= meCapacityForSg)
                return null;
        }

        // Note: per-combo capacities use ActiveXCount (not input.TotalXCapacity)
        // because not all engines may be ON in this combination
        var sgCapacity = combo.SgEnabled ? combo.ActiveMeCount * input.SgCapacityPerEngine : 0;
        var aeCapacity = combo.ActiveAeCount * input.AeCapacityPerEngine;
        var meCapacity = combo.ActiveMeCount * input.MeCapacityPerEngine;

        // SG covers hotel up to its capacity; AE covers the remainder up to AE capacity
        var sgPower = Math.Min(hotel, sgCapacity);
        var aePower = Math.Min(hotel - sgPower, aeCapacity);

        // Hotel must be fully covered by SG + AE (ME has no PTO)
        if (sgPower + aePower < hotel - PlantLimits.PowerToleranceKw)
            return null;

        // AE is ON but produces nothing → SG already covers full hotel, AE is idle
        if (combo.ActiveAeCount > 0 && aePower == 0)
            return null;

        // ME drives propulsion + SG shaft load only
        // ME cannot supply electricity for hotel without PTO (not modelled here)
        var mePower = propulsion + sgPower;

        return combo with
        {
            MePowerKw = mePower,
            SgPowerKw = sgPower,
            AePowerKw = aePower,
            MeLoadPercent = CalculationHelpers.LoadPercent(mePower, meCapacity),
            AeLoadPercent = CalculationHelpers.LoadPercent(aePower, aeCapacity)
        };
    }
}
