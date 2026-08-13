using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Helpers;
using KSailCalc.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace KSailCalc.Api.Services.Calculation;

/// <summary>
/// Level 1 optimization: evaluates all valid ME/SG/AE ON/OFF combinations,
/// distributes load, calculates FOC for each, and selects optimal + baseline.
///
/// This class is the pipeline — generate, filter, cost, rank. The parts it delegates to:
/// <list type="bullet">
///   <item><see cref="Level1CandidateBuilder"/> — which plant states exist and how load distributes</item>
///   <item><see cref="Level1PtiAssist"/> — shaft-motor assist when an ME is short (Increment C)</item>
///   <item><see cref="Level1RejectionTally"/> — why nothing survived, in words the user can act on</item>
///   <item><see cref="SelectBaseline"/> — which survivor the savings are measured against (D1)</item>
/// </list>
/// </summary>
public class Level1OptimizationService : ILevel1OptimizationService
{
    private readonly BatterySettings _batterySettings;
    private readonly double _electricPropulsionLossFactor;

    /// <param name="calculatorSettings">
    /// Optional so the many existing test call sites keep compiling; DI always supplies it.
    /// Only <see cref="CalculatorSettings.ElectricPropulsionLossFactor"/> is read (Epic E1).
    /// </param>
    public Level1OptimizationService(
        IOptions<BatterySettings> batterySettings,
        IOptions<CalculatorSettings>? calculatorSettings = null)
    {
        _batterySettings = batterySettings.Value;
        _electricPropulsionLossFactor = calculatorSettings?.Value.ElectricPropulsionLossFactor ?? 0;
    }

    public Level1Result FindOptimalCombination(
        CalculatorInput input, EngineFuelCurves curves, OperationalMode mode,
        double? overridePropulsionKw = null, int? baselineIndex = null,
        BatteryL1Adjustment? batteryAdjustment = null)
    {
        var (propulsion, hotel) = ResolveDemand(input, mode, overridePropulsionKw, batteryAdjustment);

        // Diagnostics: why combinations were rejected, so an empty result can explain itself (QA-C-1)
        var rejections = new Level1RejectionTally();
        var survivors = EvaluateCandidates(input, curves, mode, propulsion, hotel, batteryAdjustment, rejections);

        var sorted = survivors
            .OrderBy(c => c.FocTonPerHour)
            .ThenBy(c => c.ActiveMeCount + c.ActiveAeCount)
            .ToList();

        if (sorted.Count == 0)
            throw new NoValidCombinationException(
                mode, rejections.ExplainFor(mode, input, batteryAdjustment));

        var optimal = sorted[0];
        int effectiveBaselineIndex = SelectBaseline(
            sorted, baselineIndex, hasBatteryAdjustment: batteryAdjustment is not null);
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

    /// <summary>
    /// The propulsion and hotel demand this run must cover: the mode's own figures, optionally
    /// overridden on the propulsion side (sail), plus the spinning reserve the battery could not
    /// cover, which the plant carries instead (Excel R8).
    /// </summary>
    private static (double Propulsion, double Hotel) ResolveDemand(
        CalculatorInput input, OperationalMode mode, double? overridePropulsionKw,
        BatteryL1Adjustment? batteryAdjustment)
    {
        var (propulsion, hotel) = OperationalModes.LoadsOf(input, mode);
        if (overridePropulsionKw.HasValue)
            propulsion = overridePropulsionKw.Value;

        if (batteryAdjustment is not null)
        {
            propulsion += batteryAdjustment.PropulsionReserveKw;
            hotel += batteryAdjustment.HotelReserveKw;
        }

        return (propulsion, hotel);
    }

    /// <summary>
    /// The filter chain, in the order the rejection diagnostics depend on. Every candidate that
    /// survives every gate is costed and returned; every rejection is tallied so an empty result
    /// can explain itself.
    /// </summary>
    private List<EngineCombination> EvaluateCandidates(
        CalculatorInput input, EngineFuelCurves curves, OperationalMode mode,
        double propulsion, double hotel, BatteryL1Adjustment? batteryAdjustment,
        Level1RejectionTally rejections)
    {
        var survivors = new List<EngineCombination>();

        foreach (var candidate in Level1CandidateBuilder.Generate(input))
        {
            // Each stage hands the next one a NEW combination, so nothing is written twice and the
            // value you are looking at in the debugger belongs to exactly one stage.

            // Structural feasibility and load distribution in one pass — they share the same
            // plant arithmetic, so computing it twice is how the two drift apart.
            var distributed = Level1CandidateBuilder.TryDistribute(
                candidate, input, mode, propulsion, hotel, _electricPropulsionLossFactor);
            if (distributed is null)
            {
                rejections.Structural++;
                continue;
            }

            // PTI propulsion assist (Increment C, opt-in via MaxPtiPerEngineKw > 0):
            // an ME deficit may be covered by the shaft motor with power from the aux side
            var assisted = Level1PtiAssist.TryApply(distributed, input, _batterySettings.PtiLossFactor);
            if (assisted is null)
            {
                rejections.PtiAssist++;
                continue; // deficit exceeds PTI capacity or aux side cannot carry the PTI load
            }

            // AE load must not exceed 90% — otherwise L2 cannot find a valid distribution
            if (assisted.ActiveAeCount > 0 && assisted.AeLoadPercent > PlantLimits.MaxAuxLoadFraction + PlantLimits.PowerToleranceKw)
            {
                rejections.AuxOverloaded++;
                continue;
            }

            // Power sufficiency: ME must handle its assigned load (post-assist)
            var meCapacity = assisted.ActiveMeCount * input.MeCapacityPerEngine;
            if (assisted.ActiveMeCount > 0 && assisted.MePowerKw > meCapacity + PlantLimits.PowerToleranceKw)
            {
                rejections.InsufficientPower++;
                continue;
            }
            if (assisted.ActiveMeCount == 0 && assisted.MePowerKw > 0)
            {
                rejections.Structural++;
                continue;
            }

            // Battery discharge gate (Excel "Insufficient PTI"): the battery's propulsion-side
            // peak-shaving band must fit through the remaining PTI capacity. Applies only when
            // PTI is modelled — with MaxPti = 0 the bus-level simplification stands (ADR-5).
            if (input.MaxPtiPerEngineKw > 0
                && batteryAdjustment is { PropulsionPeakShavingKw: > 0 }
                && assisted.AvailablePtiKw + PlantLimits.PowerToleranceKw < batteryAdjustment.PropulsionPeakShavingKw)
            {
                rejections.BatteryPtiGate++;
                rejections.BestAvailablePtiKw = Math.Max(rejections.BestAvailablePtiKw, assisted.AvailablePtiKw);
                continue;
            }

            survivors.Add(WithFuelConsumption(assisted, curves));
        }

        return survivors;
    }

    /// <summary>
    /// Which combination the savings are measured against — an index into the FOC-sorted candidate
    /// list, so it depends on that list's ordering staying stable.
    ///
    /// Default: the last, i.e. highest-FOC, combination — the theoretical worst case.
    /// With an active battery: the third highest (decision D1 — a battery-equipped vessel already
    /// operates better than the theoretical worst case), clamped for short lists.
    /// A user-pinned index wins over both, but only when it actually addresses the list.
    /// </summary>
    internal static int SelectBaseline(
        IReadOnlyList<EngineCombination> sorted, int? requestedIndex, bool hasBatteryAdjustment)
    {
        int defaultIndex = hasBatteryAdjustment
            ? Math.Max(0, sorted.Count - 3)
            : sorted.Count - 1;

        return requestedIndex is int index && index >= 0 && index < sorted.Count
            ? index
            : defaultIndex;
    }

    /// <summary>
    /// The final stage: the combination priced in fuel, with its ME/AE split, from the resolved
    /// curves. This is the instance that goes into the result, so the optimum and the baseline are
    /// the very objects the caller sees in <c>AllValidCombinations</c>.
    /// </summary>
    private static EngineCombination WithFuelConsumption(EngineCombination combo, EngineFuelCurves curves)
    {
        double meFoc = 0;
        double aeFoc = 0;

        if (combo.MePowerKw > 0 && combo.ActiveMeCount > 0)
        {
            var meSfoc = curves.Sfoc((decimal)combo.MeLoadPercent, EngineCategory.Main);
            meFoc = CalculationHelpers.FocTonPerHour(combo.MePowerKw, meSfoc);
        }

        if (combo.AePowerKw > 0 && combo.ActiveAeCount > 0)
        {
            var aeSfoc = curves.Sfoc((decimal)combo.AeLoadPercent, EngineCategory.Auxiliary);
            aeFoc = CalculationHelpers.FocTonPerHour(combo.AePowerKw, aeSfoc);
        }

        return combo with
        {
            MeFocTonPerHour = meFoc,
            AeFocTonPerHour = aeFoc,
            FocTonPerHour = meFoc + aeFoc
        };
    }
}
