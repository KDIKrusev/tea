using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Helpers;
using KSailCalc.Api.Services.Interfaces;

namespace KSailCalc.Api.Services;

/// <summary>
/// Level 1 optimization: evaluates all valid ME/SG/AE ON/OFF combinations,
/// distributes load, calculates FOC for each, and selects optimal + baseline.
/// </summary>
public class Level1OptimizationService : ILevel1OptimizationService
{
    private readonly ISfocService _sfocService;

    public Level1OptimizationService(ISfocService sfocService)
    {
        _sfocService = sfocService;
    }

    public async Task<Level1Result> FindOptimalCombinationAsync(
        CalculatorInput input, OperationalMode mode, double? overridePropulsionKw = null, int? baselineIndex = null)
    {
        var (propulsion, hotel) = GetModeLoads(input, mode);
        if (overridePropulsionKw.HasValue)
            propulsion = overridePropulsionKw.Value;

        var allCombinations = GenerateCombinations(input);
        var validCombinations = new List<EngineCombination>();

        foreach (var combo in allCombinations)
        {
            if (!IsValid(combo, input, mode, propulsion, hotel))
                continue;

            DistributeLoad(combo, input, propulsion, hotel);

            // AE load must not exceed 90% — otherwise L2 cannot find a valid distribution
            if (combo.ActiveAeCount > 0 && combo.AeLoadPercent > 0.90 + 0.001)
                continue;

            // Power sufficiency: ME must handle its assigned load
            var meCapacity = combo.ActiveMeCount * input.MeCapacityPerEngine;
            if (combo.ActiveMeCount > 0 && combo.MePowerKw > meCapacity)
                continue;
            if (combo.ActiveMeCount == 0 && combo.MePowerKw > 0)
                continue;

            combo.FocTonPerHour = await CalculateFocAsync(combo, input);
            validCombinations.Add(combo);
        }

        var sorted = validCombinations
            .OrderBy(c => c.FocTonPerHour)
            .ThenBy(c => c.ActiveMeCount + c.ActiveAeCount)
            .ToList();

        if (sorted.Count == 0)
            throw new InvalidOperationException($"No valid engine combinations found for {mode} mode");

        var optimal = sorted[0];
        // Default baseline = last (highest FOC) combination.
        // Can be overridden by baselineIndex parameter for user-selected baseline.
        int defaultBaselineIndex = sorted.Count - 1;
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

    #region Mode Loads

    private static (double propulsion, double hotel) GetModeLoads(CalculatorInput input, OperationalMode mode)
    {
        return mode switch
        {
            OperationalMode.Transit => (input.EffectivePropulsionPower, input.TransitHotelPowerKW),
            OperationalMode.DP => (input.RequiredDPPowerKW ?? 0, input.DPHotelPowerKW ?? 0),
            OperationalMode.Port => (0, input.PortHotelPowerKW),
            OperationalMode.Anchor => (0, input.AnchorHotelPowerKW),
            OperationalMode.Maneuvering => (input.ManeuveringPropulsionPowerKW, input.ManeuveringHotelPowerKW),
            _ => throw new ArgumentException($"Unknown mode: {mode}")
        };
    }

    #endregion

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

        // If there is a shaft generator 
        if(input.TotalSgCapacity > 0 & !combo.SgEnabled)
        {
            return false;
        }
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
            if (potentialSgCapacity >= hotel - 0.001 && propulsion + potentialSgPower <= meCapacityForSg)
                return false;
        }

        var sgCapacity = combo.SgEnabled ? combo.ActiveMeCount * input.SgCapacityPerEngine : 0;
        var aeCapacity = combo.ActiveAeCount * input.AeCapacityPerEngine;
        var sgPower = Math.Min(hotel, sgCapacity);
        var aePower = Math.Min(hotel - sgPower, aeCapacity);

        // Hotel must be fully covered by SG + AE (ME has no PTO)
        if (sgPower + aePower < hotel - 0.001)
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