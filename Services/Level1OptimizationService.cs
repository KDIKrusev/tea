using System.Globalization;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Helpers;
using KSailCalc.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace KSailCalc.Api.Services;

/// <summary>
/// Level 1 optimization: evaluates all valid ME/SG/AE ON/OFF combinations,
/// distributes load, calculates FOC for each, and selects optimal + baseline.
/// </summary>
public class Level1OptimizationService : ILevel1OptimizationService
{
    private readonly ISfocService _sfocService;
    private readonly BatterySettings _batterySettings;

    public Level1OptimizationService(ISfocService sfocService, IOptions<BatterySettings> batterySettings)
    {
        _sfocService = sfocService;
        _batterySettings = batterySettings.Value;
    }

    public async Task<Level1Result> FindOptimalCombinationAsync(
        CalculatorInput input, OperationalMode mode, double? overridePropulsionKw = null, int? baselineIndex = null,
        BatteryL1Adjustment? batteryAdjustment = null)
    {
        var (propulsion, hotel) = OperationalModes.LoadsOf(input, mode);
        if (overridePropulsionKw.HasValue)
            propulsion = overridePropulsionKw.Value;

        // Battery: spinning reserve the battery could not cover is carried by the plant (Excel R8)
        if (batteryAdjustment is not null)
        {
            propulsion += batteryAdjustment.PropulsionReserveKw;
            hotel += batteryAdjustment.HotelReserveKw;
        }

        var allCombinations = GenerateCombinations(input);
        var validCombinations = new List<EngineCombination>();
        // Diagnostics: why combinations were rejected, so an empty result can explain itself (QA-C-1)
        var rejections = new RejectionTally();

        foreach (var combo in allCombinations)
        {
            if (!IsValid(combo, input, mode, propulsion, hotel))
            {
                rejections.Structural++;
                continue;
            }

            DistributeLoad(combo, input, propulsion, hotel);

            // PTI propulsion assist (Increment C, opt-in via MaxPtiPerEngineKw > 0):
            // an ME deficit may be covered by the shaft motor with power from the aux side
            if (!TryApplyPtiAssist(combo, input))
            {
                rejections.PtiAssist++;
                continue; // deficit exceeds PTI capacity or aux side cannot carry the PTI load
            }

            // AE load must not exceed 90% — otherwise L2 cannot find a valid distribution
            if (combo.ActiveAeCount > 0 && combo.AeLoadPercent > PlantLimits.MaxAuxLoadFraction + PlantLimits.PowerToleranceKw)
            {
                rejections.AuxOverloaded++;
                continue;
            }

            // Power sufficiency: ME must handle its assigned load (post-assist)
            var meCapacity = combo.ActiveMeCount * input.MeCapacityPerEngine;
            if (combo.ActiveMeCount > 0 && combo.MePowerKw > meCapacity + PlantLimits.PowerToleranceKw)
            {
                rejections.InsufficientPower++;
                continue;
            }
            if (combo.ActiveMeCount == 0 && combo.MePowerKw > 0)
            {
                rejections.Structural++;
                continue;
            }

            // Battery discharge gate (Excel "Insufficient PTI"): the battery's propulsion-side
            // peak-shaving band must fit through the remaining PTI capacity. Applies only when
            // PTI is modelled — with MaxPti = 0 the bus-level simplification stands (ADR-5).
            if (input.MaxPtiPerEngineKw > 0
                && batteryAdjustment is { PropulsionPeakShavingKw: > 0 }
                && combo.AvailablePtiKw + PlantLimits.PowerToleranceKw < batteryAdjustment.PropulsionPeakShavingKw)
            {
                rejections.BatteryPtiGate++;
                rejections.BestAvailablePtiKw = Math.Max(rejections.BestAvailablePtiKw, combo.AvailablePtiKw);
                continue;
            }

            combo.FocTonPerHour = await CalculateFocAsync(combo, input);
            validCombinations.Add(combo);
        }

        var sorted = validCombinations
            .OrderBy(c => c.FocTonPerHour)
            .ThenBy(c => c.ActiveMeCount + c.ActiveAeCount)
            .ToList();

        if (sorted.Count == 0)
            throw new NoValidCombinationException(
                mode, rejections.ExplainFor(mode, input, batteryAdjustment));

        var optimal = sorted[0];
        // Default baseline = last (highest FOC) combination.
        // With an active battery: third-highest (decision D1 — a battery vessel already operates
        // better than the theoretical worst case), clamped for small lists.
        // Can be overridden by baselineIndex parameter for user-selected baseline.
        int defaultBaselineIndex = batteryAdjustment is not null
            ? Math.Max(0, sorted.Count - 3)
            : sorted.Count - 1;
        int effectiveBaselineIndex = baselineIndex.HasValue && baselineIndex.Value >= 0 && baselineIndex.Value < sorted.Count
            ? baselineIndex.Value
            : defaultBaselineIndex;
        var baseline = sorted[effectiveBaselineIndex];

        return new Level1Result
        {
            OptimalCombination = optimal,
            BaselineCombination = baseline,
            OptimalFocTonPerHour = optimal.FocTonPerHour,
            BaselineFocTonPerHour = baseline.FocTonPerHour,
            AllValidCombinations = sorted,
            SelectedBaselineIndex = effectiveBaselineIndex
        };
    }

    #region Combination Generation

    private static List<EngineCombination> GenerateCombinations(CalculatorInput input)
    {
        // Every combination of:  ME: 0..N  ×  SG: off/on  ×  AE: 0..N
        // Example: 2 ME, SG, 3 AE → (0,1,2) × (off,on) × (0,1,2,3) = 24 combinations
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

    #endregion

    #region Validation

    private static bool IsValid(EngineCombination combo, CalculatorInput input, OperationalMode mode,
        double propulsion, double hotel)
    {
        // ME=0 invalid for Transit/Maneuvering (vessel needs ME for propulsion)
        if ((mode == OperationalMode.Transit || mode == OperationalMode.Maneuvering) && combo.ActiveMeCount == 0)
            return false;

        // An installed shaft generator is always run (observation #1: this also forces an ME to
        // run in Port/Anchor to spin it — deliberate for now, logged in the QA scenarios' README).
        if (input.TotalSgCapacity > 0 && !combo.SgEnabled)
            return false;
        // SG ON without ME → physically impossible (SG is driven by ME shaft)
        if (combo.SgEnabled && combo.ActiveMeCount == 0)
            return false;

        // SG ON but SG capacity is 0 → meaningless, skip to avoid duplicate combos
        if (combo.SgEnabled && input.SgCapacityPerEngine <= 0)
            return false;

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
                return false;
        }

        var sgCapacity = combo.SgEnabled ? combo.ActiveMeCount * input.SgCapacityPerEngine : 0;
        var aeCapacity = combo.ActiveAeCount * input.AeCapacityPerEngine;
        var sgPower = Math.Min(hotel, sgCapacity);
        var aePower = Math.Min(hotel - sgPower, aeCapacity);

        // Hotel must be fully covered by SG + AE (ME has no PTO)
        if (sgPower + aePower < hotel - PlantLimits.PowerToleranceKw)
            return false;

        // AE is ON but produces nothing → SG already covers full hotel, AE is idle
        if (combo.ActiveAeCount > 0 && aePower == 0)
            return false;

        return true;
    }

    #endregion

    #region Load Distribution

    private static void DistributeLoad(
        EngineCombination combo, CalculatorInput input, double propulsion, double hotel)
     {
        var sgCapacity = combo.SgEnabled
            ? combo.ActiveMeCount * input.SgCapacityPerEngine
            : 0;
        var aeCapacity = combo.ActiveAeCount * input.AeCapacityPerEngine;
        var meCapacity = combo.ActiveMeCount * input.MeCapacityPerEngine;

        // Note: per-combo capacities use ActiveXCount (not input.TotalXCapacity)
        // because not all engines may be ON in this combination

        // SG covers hotel up to its capacity
        var sgPower = Math.Min(hotel, sgCapacity);

        // AE covers remaining hotel up to AE capacity
        var remainingHotel = hotel - sgPower;
        var aePower = Math.Min(remainingHotel, aeCapacity);

        // ME drives propulsion + SG shaft load only
        // ME cannot supply electricity for hotel without PTO (not modelled here)
        var mePower = propulsion + sgPower;

        combo.MePowerKw = mePower;
        combo.SgPowerKw = sgPower;
        combo.AePowerKw = aePower;
        combo.MeLoadPercent = CalculationHelpers.LoadPercent(mePower, meCapacity);
        combo.AeLoadPercent = CalculationHelpers.LoadPercent(aePower, aeCapacity);
    }

    #endregion

    #region Diagnostics (QA-C-1)

    /// <summary>
    /// Counts why combinations were rejected and turns that into one actionable sentence.
    /// Ordered by how likely the cause is something the user just typed.
    /// </summary>
    private sealed class RejectionTally
    {
        public int Structural;         // ME=0 in Transit, hotel not coverable by SG+AE, idle AE…
        public int PtiAssist;          // ME deficit beyond PTI capacity, or aux cannot feed the PTI motor
        public int AuxOverloaded;      // AE above 90% load
        public int InsufficientPower;  // ME cannot carry its assigned load
        public int BatteryPtiGate;     // battery's propulsion band does not fit through PTI
        public double BestAvailablePtiKw;

        public string ExplainFor(OperationalMode mode, CalculatorInput input, BatteryL1Adjustment? battery)
        {
            if (BatteryPtiGate > 0 && battery is not null)
            {
                // Invariant culture: the UI is English, the server may run under any locale
                var required = battery.PropulsionPeakShavingKw.ToString("0.#", CultureInfo.InvariantCulture);
                var available = BestAvailablePtiKw.ToString("0.#", CultureInfo.InvariantCulture);
                var configured = input.MaxPtiPerEngineKw.ToString("0.#", CultureInfo.InvariantCulture);
                return $"the battery needs {required} kW of PTI capacity to shave propulsion peaks in "
                     + $"{mode} mode, but only {available} kW is available. Increase the PTI capacity "
                     + $"per main engine (currently {configured} kW), reduce the battery "
                     + $"power, or clear the PTI field to model the battery at switchboard level only.";
            }

            if (PtiAssist > 0 || InsufficientPower > 0)
                return $"the installed engines cannot carry the {mode} demand. Increase engine capacity or "
                     + "engine count, or reduce the propulsion/hotel power for this mode.";

            if (AuxOverloaded > 0)
                return $"the auxiliary engines would run above 90% load in {mode} mode. Increase auxiliary "
                     + "engine capacity or count, or reduce the hotel/mission power.";

            return $"no engine configuration can cover the {mode} demand. Check the engine capacities, "
                 + "engine counts and the power demands for this mode.";
        }
    }

    #endregion

    #region PTI (Increment C)

    /// <summary>
    /// When the assigned ME power exceeds ME capacity and PTI is configured, cover the deficit
    /// with shaft motors: PTI power moves to the aux side grossed up by the transmission loss
    /// (Excel I4, default 5%). Returns false when the combination remains infeasible.
    /// PTI capacity spans ALL INSTALLED machines (Increment G, decision D5 — union model):
    /// the Excel allows PTI only on idle engines' machines (N-column: electric drive of the
    /// non-running shaft, e.g. row 59), while running-shaft boost is real marine practice —
    /// the union covers both physics. Direction exclusivity stays relaxed in aggregate (D-C2).
    /// </summary>
    private bool TryApplyPtiAssist(EngineCombination combo, CalculatorInput input)
    {
        var ptiCapacity = input.MeCount * input.MaxPtiPerEngineKw;
        combo.AvailablePtiKw = ptiCapacity;

        if (combo.ActiveMeCount == 0 || ptiCapacity <= 0)
            return true; // PTI not modelled / no engaged shaft — existing checks decide

        var meCapacity = combo.ActiveMeCount * input.MeCapacityPerEngine;
        var deficit = combo.MePowerKw - meCapacity;
        if (deficit <= PlantLimits.PowerToleranceKw)
            return true; // ME carries everything; full PTI capacity stays available

        if (deficit > ptiCapacity + PlantLimits.PowerToleranceKw)
            return false; // "Insufficient pwr" even with full PTI

        // Move the deficit to the aux side with transmission losses
        var ptiAuxLoad = deficit * (1 + _batterySettings.PtiLossFactor);
        var aeCapacity = combo.ActiveAeCount * input.AeCapacityPerEngine;
        var newAePower = combo.AePowerKw + ptiAuxLoad;
        if (combo.ActiveAeCount == 0 || newAePower > aeCapacity + PlantLimits.PowerToleranceKw)
            return false; // no aux headroom to feed the PTI motor

        combo.PtiPowerKw = deficit;
        combo.AvailablePtiKw = ptiCapacity - deficit;
        combo.MePowerKw = meCapacity;
        combo.AePowerKw = newAePower;
        combo.MeLoadPercent = CalculationHelpers.LoadPercent(combo.MePowerKw, meCapacity);
        combo.AeLoadPercent = CalculationHelpers.LoadPercent(combo.AePowerKw, aeCapacity);
        return true;
    }

    #endregion

    #region FOC Calculation

    private async Task<double> CalculateFocAsync(EngineCombination combo, CalculatorInput input)
    {
        double meFoc = 0;
        double aeFoc = 0;

        if (combo.MePowerKw > 0 && combo.ActiveMeCount > 0)
        {
            var meSfoc = await _sfocService.GetSfocForLoadAsync(
                (decimal)combo.MeLoadPercent, EngineCategory.Main, input.MainEngineTypeId);
            meFoc = CalculationHelpers.FocTonPerHour(combo.MePowerKw, meSfoc);
        }

        if (combo.AePowerKw > 0 && combo.ActiveAeCount > 0)
        {
            var aeSfoc = await _sfocService.GetSfocForLoadAsync(
                (decimal)combo.AeLoadPercent, EngineCategory.Auxiliary, input.AuxEngineTypeId);
            aeFoc = CalculationHelpers.FocTonPerHour(combo.AePowerKw, aeSfoc);
        }

        combo.MeFocTonPerHour = meFoc;
        combo.AeFocTonPerHour = aeFoc;

        return meFoc + aeFoc;
    }

    #endregion
}