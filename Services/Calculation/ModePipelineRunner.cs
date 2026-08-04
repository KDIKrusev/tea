using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Battery;
using KSailCalc.Api.Services.Helpers;
using KSailCalc.Api.Services.Interfaces;
using KSailCalc.Api.Services.Results;

namespace KSailCalc.Api.Services.Calculation;

/// <summary>
/// Runs the L1 → L2 → L3 pipeline per operational mode.
///
/// Two deliberate asymmetries live here, not in the mode registry:
/// Transit is the only mode that runs Levels 2 and 3 (decision D4/Q5 — Level 2 has no Excel
/// counterpart in other modes), and the user-pinned baseline applies to Transit only.
/// </summary>
public class ModePipelineRunner : IModePipelineRunner
{
    private readonly ISfocService _sfocService;
    private readonly ILevel1OptimizationService _level1Service;
    private readonly ILevel2OptimizationService _level2Service;
    private readonly ILevel3DrcService _level3Service;
    private readonly IBatteryAllocationService _batteryService;
    private readonly ILogger<ModePipelineRunner> _logger;

    public ModePipelineRunner(
        ISfocService sfocService,
        ILevel1OptimizationService level1Service,
        ILevel2OptimizationService level2Service,
        ILevel3DrcService level3Service,
        IBatteryAllocationService batteryService,
        ILogger<ModePipelineRunner> logger)
    {
        _sfocService = sfocService;
        _level1Service = level1Service;
        _level2Service = level2Service;
        _level3Service = level3Service;
        _batteryService = batteryService;
        _logger = logger;
    }

    /// <summary>
    /// What each mode decided, at Debug level. This is the trace a support question needs — "why
    /// did this vessel get these numbers" — and without it the pipeline is completely silent.
    /// Enable with <c>"KSailCalc.Api.Services.Calculation": "Debug"</c> in Logging:LogLevel.
    /// </summary>
    private void LogModeOutcome(OperationalMode mode, double hours, Level1Result l1, BatteryModeOutcome? battery)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
            return;

        var optimal = l1.OptimalCombination;
        var baseline = l1.BaselineCombination;

        _logger.LogDebug(
            "{Mode}: {Hours} h · optimal ME={OptME}/SG={OptSG}/AE={OptAE} @ {OptFoc:F6} t/h · " +
            "baseline[{BaselineIndex}] ME={BaseME}/SG={BaseSG}/AE={BaseAE} @ {BaseFoc:F6} t/h · " +
            "{Candidates} valid candidates",
            mode, hours,
            optimal.ActiveMeCount, optimal.SgEnabled, optimal.ActiveAeCount, l1.OptimalFocTonPerHour,
            l1.SelectedBaselineIndex,
            baseline.ActiveMeCount, baseline.SgEnabled, baseline.ActiveAeCount, l1.BaselineFocTonPerHour,
            l1.AllValidCombinations.Count);

        if (battery is not null)
        {
            _logger.LogDebug(
                "{Mode} battery: committed {Committed:F1} kW · peak-shaving band {Band:F1} kW · " +
                "uncovered reserve {Reserve:F1} kW · R3a benefit {Benefit:F2} t/yr",
                mode, battery.Allocation.CommittedBatteryKw, battery.Allocation.PeakShavingBandKw,
                battery.Allocation.AdditionalSpinningReserveKw, battery.BenefitTonPerYear);
        }
    }

    public async Task<List<ModePipelineResult>> RunAllModesAsync(
        CalculatorInput input, double? transitPropulsionOverrideKw)
    {
        // Both SFOC curves for the whole calculation, resolved once. This is the only await on the
        // optimization path — every level below is a synchronous pure function over these curves.
        var curves = await _sfocService.GetCurvesAsync(input);

        // Transit: full L1 → L2 → L3 pipeline (optimized, shown to client)
        // Other modes: L1 only (for FOC calculation), L2/L3 are not optimized
        var modeResults = new List<ModePipelineResult>();

        var transit = RunOptimizationPipeline(
            input, curves, OperationalMode.Transit, transitPropulsionOverrideKw);
        modeResults.Add(new(OperationalMode.Transit, transit.L1, transit.L2, transit.L3,
            input.TransitHours, transit.Battery));

        LogModeOutcome(OperationalMode.Transit, input.TransitHours, transit.L1, transit.Battery);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Transit L2: {Setpoints} setpoints · saves {L2Savings:F6} t/h · L3 DRC {L3Savings:F2} t/yr " +
                "on {Variation:F1} kW variation (battery shaved {Shaved:F1} kW)",
                transit.L2.OptimalSetpoints.Count, transit.L2.Level2SavingsTonPerHour,
                transit.L3.DrcSavingsTonPerYear, transit.L3.VariationPerGeneratorKw,
                transit.L3.BatteryShavedVariationKw);
        }

        // Non-transit modes: only L1 for baseline/optimal FOC, no L2/L3 optimization (D4/Q5).
        // The user-pinned baseline applies to Transit only — other modes keep their own default.
        var emptyL2 = new Level2Result();
        var emptyL3 = new Level3Result();

        foreach (var spec in OperationalModes.ExceptTransit)
        {
            if (!spec.IsActive(input))
                continue;

            var hours = spec.Hours(input);
            var (l1, battery) = RunL1(input, curves, spec.Mode, hours, null, null);
            modeResults.Add(new(spec.Mode, l1, emptyL2, emptyL3, hours, battery));

            LogModeOutcome(spec.Mode, hours, l1, battery);
        }

        return modeResults;
    }

    private (Level1Result L1, Level2Result L2, Level3Result L3, BatteryModeOutcome? Battery)
        RunOptimizationPipeline(CalculatorInput input, EngineFuelCurves curves, OperationalMode mode,
            double? overridePropulsionKw)
    {
        var modeHours = OperationalModes.HoursOf(input, mode);
        var (l1, battery) = RunL1(input, curves, mode, modeHours, overridePropulsionKw, input.BaselineIndex);
        var l2 = _level2Service.OptimizeLoadSetpoints(l1, input, curves);
        // Increment E (Q4 working rule): DRC monetizes only the variation the battery didn't shave
        var hotelBand = battery is null ? 0 : BatteryModeAdapter.HotelPeakShavingKw(battery.Allocation);
        var l3 = _level3Service.CalculateDrcSavings(l2, input, curves, modeHours, hotelBand);
        return (l1, l2, l3, battery);
    }

    /// <summary>
    /// Level 1 for one mode, battery-aware. Inactive battery → today's exact code path.
    /// Active battery → demand adjusted by the uncovered spinning reserve (Excel R8) and the
    /// "third highest" default baseline; additionally runs the no-battery reference scenario
    /// (budget 0 ⇒ full variation carried by gensets) to compute the R3a "Battery benefit".
    /// </summary>
    private (Level1Result L1, BatteryModeOutcome? Battery) RunL1(
        CalculatorInput input, EngineFuelCurves curves, OperationalMode mode, double modeHours,
        double? overridePropulsionKw, int? baselineIndex)
    {
        if (input.Battery?.AppliesTo(mode) != true)
            return (_level1Service.FindOptimalCombination(input, curves, mode, overridePropulsionKw, baselineIndex), null);

        // R3a — the battery's value is measured by running the SAME plant twice and comparing the
        // two optima. The two calls below are not a repetition; they are the two scenarios.
        var withBattery = OptimizeWithBatteryBudget(
            input, curves, mode, overridePropulsionKw, baselineIndex, budgetOverrideKw: null);

        var withoutBattery = OptimizeWithBatteryBudget(
            input, curves, mode, overridePropulsionKw,
            // The counterfactual is not steered by the user's pinned baseline — it must stay the
            // plant's own optimum, or the comparison stops being optimal-vs-optimal.
            baselineIndex: null,
            budgetOverrideKw: 0);

        var benefitTons = Math.Max(
            0, withoutBattery.L1.OptimalFocTonPerHour - withBattery.L1.OptimalFocTonPerHour) * modeHours;

        return (withBattery.L1, new BatteryModeOutcome(withBattery.Allocation, benefitTons));
    }

    /// <summary>
    /// Allocate a battery budget over the mode's loads, then optimize Level 1 against whatever the
    /// gensets are left carrying.
    /// </summary>
    /// <param name="budgetOverrideKw">
    /// <c>null</c> → the vessel's real battery power.
    /// <c>0</c> → the "no battery" counterfactual: the cascade still runs but covers nothing, so the
    /// FULL variation lands on the gensets as spinning reserve.
    ///
    /// Note this is NOT the same as passing no battery adjustment at all. That would model a plant
    /// which does not carry the variation on either side — a fiction that burns LESS fuel than the
    /// battery case and would make the benefit come out negative.
    /// </param>
    private (Level1Result L1, BatteryModeAllocation Allocation) OptimizeWithBatteryBudget(
        CalculatorInput input, EngineFuelCurves curves, OperationalMode mode,
        double? overridePropulsionKw, int? baselineIndex, double? budgetOverrideKw)
    {
        var allocation = _batteryService.Allocate(mode, input, budgetOverrideKw, overridePropulsionKw);

        var l1 = _level1Service.FindOptimalCombination(
            input, curves, mode, overridePropulsionKw, baselineIndex,
            BatteryModeAdapter.ToAdjustment(allocation));

        return (l1, allocation);
    }
}
