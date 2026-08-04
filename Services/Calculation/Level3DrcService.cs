using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace KSailCalc.Api.Services.Calculation;

/// <summary>
/// Level 3: Dynamic Ramp Control (DRC). Calculates annual fuel savings from
/// reducing generator load spike variations by 20%.
///
/// Heavy consumers cause ±kW spikes every ~2 minutes. The non-linear SFOC curve
/// means spikes waste fuel vs constant load. DRC smooths the spikes.
///
/// Pure and synchronous: the SFOC curves arrive pre-resolved.
/// </summary>
public class Level3DrcService : ILevel3DrcService
{
    private readonly CalculatorSettings _settings;

    private const double DrcReductionFactor = 0.80;  // DRC reduces variation by 20%
    private const double HalfCycleHours = 1.0 / 60;  // 1 minute = 1/60 hour
    private const int CyclesPerHour = 30;             // 2-min cycle → 30 per hour

    public Level3DrcService(IOptions<CalculatorSettings> settings)
    {
        _settings = settings.Value;
    }

    public Level3Result CalculateDrcSavings(
        Level2Result level2Result, CalculatorInput input, EngineFuelCurves curves, double annualHours,
        double batteryHotelPeakShavingKw = 0)
    {
        // DRC applies to ALL generators (both SG and AE) per spec:
        // "These functions are applied towards shaft and auxiliary generators"
        var generators = level2Result.OptimalSetpoints.ToList();
        var activeGeneratorCount = generators.Count;

        // No generators → no spikes to reduce → savings = 0
        if (activeGeneratorCount == 0)
        {
            return new Level3Result
            {
                DrcSavingsTonPerYear = 0,
                ActiveGeneratorCount = 0,
                AnnualHours = annualHours
            };
        }

        var vesselVariation = input.HotelLoadVariationKw.HasValue && input.HotelLoadVariationKw.Value > 0
            ? input.HotelLoadVariationKw.Value
            : GetVesselVariation(input.VesselTypeName);

        // Increment E (Q4 working rule): the battery already shaves part of the hotel/mission
        // spikes — DRC only monetizes the residual variation, never the same band twice
        var batteryShaved = Math.Min(Math.Max(batteryHotelPeakShavingKw, 0), vesselVariation);

        // Variation is the total kW swing on the bus — use as-is, not divided per generator
        var variationKw = vesselVariation - batteryShaved;
        var reducedVariation = variationKw * DrcReductionFactor;

        double totalSavingsGramsPerCycle = 0;
        double totalFocWithoutDrc = 0;
        double totalFocWithDrc = 0;

        foreach (var gen in generators)
        {
            // Setpoints only ever come from Level 2, which always populates both PowerKw and
            // CapacityKw (an engine it switches off gets PowerKw = 0, not an absent value).
            // The former "derive them from the input" fallbacks could not fire.
            var focWithout = CalculateCycleFoc(
                gen.PowerKw, variationKw, gen.CapacityKw, curves, gen.GeneratorType.ToEngineCategory());
            var focWith = CalculateCycleFoc(
                gen.PowerKw, reducedVariation, gen.CapacityKw, curves, gen.GeneratorType.ToEngineCategory());

            totalFocWithoutDrc += focWithout;
            totalFocWithDrc += focWith;
            totalSavingsGramsPerCycle += Math.Max(0, focWithout - focWith);
        }

        // Annual savings: per-cycle saving × 30 cycles/h × annual hours
        var savingsGramsPerHour = totalSavingsGramsPerCycle * CyclesPerHour;
        var savingsTonPerYear = savingsGramsPerHour * annualHours / 1_000_000;

        return new Level3Result
        {
            DrcSavingsTonPerYear = savingsTonPerYear,
            WithoutDrcFocGramsPerCycle = totalFocWithoutDrc,
            WithDrcFocGramsPerCycle = totalFocWithDrc,
            VariationPerGeneratorKw = variationKw,
            ReducedVariationPerGeneratorKw = reducedVariation,
            BatteryShavedVariationKw = batteryShaved,
            ActiveGeneratorCount = activeGeneratorCount,
            AnnualHours = annualHours
        };
    }

    /// <summary>
    /// Calculate FOC (grams) for one 2-minute spike cycle on a single generator.
    /// Half cycle up (1 min) + half cycle down (1 min).
    /// </summary>
    private static double CalculateCycleFoc(
        double steadyLoadKw, double variationKw, double capacityKw,
        EngineFuelCurves curves, EngineCategory engineType)
    {
        // Ramp up load, clamped to [0, capacity]
        var loadUp = Math.Clamp(steadyLoadKw + variationKw, 0, capacityKw);
        // Ramp down load, clamped to [0, capacity]
        var loadDown = Math.Clamp(steadyLoadKw - variationKw, 0, capacityKw);

        var loadPctUp = capacityKw > 0 ? loadUp / capacityKw : 0;
        var loadPctDown = capacityKw > 0 ? loadDown / capacityKw : 0;

        var sfocUp = curves.Sfoc((decimal)loadPctUp, engineType);
        var sfocDown = curves.Sfoc((decimal)loadPctDown, engineType);

        // FOC per half cycle (1 minute = HalfCycleHours)
        var focUp = loadUp * sfocUp * HalfCycleHours;     // grams
        var focDown = loadDown * sfocDown * HalfCycleHours; // grams

        return focUp + focDown;
    }

    private double GetVesselVariation(string? vesselTypeName)
    {
        if (string.IsNullOrWhiteSpace(vesselTypeName))
            return _settings.DefaultVesselVariationKw;

        if (_settings.VesselVariations.TryGetValue(vesselTypeName, out var variation))
            return variation;

        // Substring match (e.g. "Bulk Carrier 10,000 dwt" → "Bulk Carrier")
        foreach (var kvp in _settings.VesselVariations)
        {
            if (vesselTypeName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return _settings.DefaultVesselVariationKw;
    }
}
