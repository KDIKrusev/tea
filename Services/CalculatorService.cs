using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Repositories.Interfaces;
using KSailCalc.Api.Services.Helpers;
using KSailCalc.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace KSailCalc.Api.Services;

/// <summary>
/// Orchestrates iEMS savings calculations.
/// Runs Level 1 → 2 → 3 optimization pipeline per operational mode,
/// then aggregates savings into three product tiers (Advanced, Pro, Premium).
/// </summary>
public class CalculatorService : ICalculatorService
{
    private readonly IKSailCalcConfigRepository _configRepository;
    private readonly ISailContributionService _sailContributionService;
    private readonly ILevel1OptimizationService _level1Service;
    private readonly ILevel2OptimizationService _level2Service;
    private readonly ILevel3DrcService _level3Service;
    private readonly IBatteryAllocationService _batteryService;
    private readonly CalculatorSettings _settings;

    public CalculatorService(
        IKSailCalcConfigRepository configRepository,
        ISailContributionService sailContributionService,
        ILevel1OptimizationService level1Service,
        ILevel2OptimizationService level2Service,
        ILevel3DrcService level3Service,
        IBatteryAllocationService batteryService,
        IOptions<CalculatorSettings> settings)
    {
        _configRepository = configRepository;
        _sailContributionService = sailContributionService;
        _level1Service = level1Service;
        _level2Service = level2Service;
        _level3Service = level3Service;
        _batteryService = batteryService;
        _settings = settings.Value;
    }

    #region Parameter Objects

    private record FocBreakdown(
        double BaselineFoc, double BaselineMeFoc, double BaselineAeFoc,
        double OptimalMeFoc, double OptimalAeFoc);

    private record TierSavings(double Advanced, double Pro, double Premium,
        double L1Savings, double L2Savings, double L3Savings);

    /// <summary>
    /// What one product tier reports. The tier decides which optimization levels it includes, so
    /// the savings components and the L2/L3 detail panels travel together with its price config.
    /// </summary>
    private record TierPlan(
        IntegrationLevelConfig Config,
        double TotalSavingsTon,
        double L1Savings,
        double L2Savings,
        double L3Savings,
        Level2Details? L2Details,
        Level3Details? L3Details);

    private record BuildResultContext(
        CalculatorInput Input,
        FocBreakdown Foc,
        CalculatorSettings Settings,
        EngineCombination TransitOptimalCombo,
        TierPlan Tier);

    private record ModePipelineResult(
        OperationalMode Mode, Level1Result L1, Level2Result L2, Level3Result L3, double Hours,
        BatteryModeOutcome? Battery);

    /// <summary>What the battery produced in one mode: its allocation and the R3a benefit.</summary>
    private record BatteryModeOutcome(BatteryModeAllocation Allocation, double BenefitTonPerYear);

    #endregion

    /// <summary>
    /// Calculate results for all integration levels in one call.
    /// Runs L1→L2→L3 pipeline for Transit (and optionally DP),
    /// then builds three product tiers with cumulative savings.
    /// </summary>
    public async Task<AllVariantsCalculationResult> CalculateAllVariantsAsync(CalculatorInput input, List<ValidationWarning>? warnings = null)
    {
        var pricingConfigs = await _configRepository.GetIntegrationLevelConfigsAsync();
        var capacities = BuildEngineCapacities(input);
        // Sail contribution reduces propulsion demand for Transit mode optimization
        var sailResult = await CalculateSailIfEnabledAsync(input);

        // Transit: full L1 → L2 → L3 pipeline (optimized, shown to client)
        // Other modes: L1 only (for FOC calculation), L2/L3 are not optimized
        var modeResults = new List<ModePipelineResult>();

        var transitPropulsion = sailResult?.TransitPropulsionAfterKw;
        var transit = await RunOptimizationPipelineAsync(input, OperationalMode.Transit, transitPropulsion);
        modeResults.Add(new(OperationalMode.Transit, transit.L1, transit.L2, transit.L3,
            input.TransitHours, transit.Battery));

        // Non-transit modes: only L1 for baseline/optimal FOC, no L2/L3 optimization (D4/Q5).
        // The user-pinned baseline applies to Transit only — other modes keep their own default.
        var emptyL2 = new Level2Result();
        var emptyL3 = new Level3Result();

        foreach (var spec in OperationalModes.ExceptTransit)
        {
            if (!spec.IsActive(input))
                continue;

            var hours = spec.Hours(input);
            var (l1, battery) = await RunL1Async(input, spec.Mode, hours, null, null);
            modeResults.Add(new(spec.Mode, l1, emptyL2, emptyL3, hours, battery));
        }

        // Aggregate savings across modes
        var foc = CalculateFocBreakdown(modeResults);
        var savings = CalculateTierSavings(modeResults);

        // Build per-tier details from Transit (primary mode shown to client)
        var transitResult = modeResults.First(m => m.Mode == OperationalMode.Transit);
        var l1Details = Level1Details.FromResult(transitResult.L1);
        // Note: BaselineMeLoadPercent, BaselineMePowerKw etc. come directly from
        // the Transit baseline combination via Level1Details.FromResult — no weighted
        // average needed since the client only shows Transit mode details.

        var l2Details = Level2Details.FromResult(transitResult.L2);
        var l3Details = Level3Details.FromResult(transitResult.L3);
        var powerDemands = BuildPowerDemands(modeResults, input, sailResult, capacities);

        // Build results for each tier
        var configMap = pricingConfigs.ToDictionary(c => c.IntegrationLevelId.ToString());
        var transitOptimal = transitResult.L1.OptimalCombination;

        var result = new AllVariantsCalculationResult
        {
            // Shared data (displayed once by the client)
            PowerDemands = powerDemands,
            Level1Details = l1Details,
            SailContribution = sailResult,
            BatteryDetails = BuildBatteryDetails(input, modeResults),
            BaselineFOC = foc.BaselineFoc,
            BaselineCO2 = Co2ForEngines(foc.BaselineMeFoc, foc.BaselineAeFoc, input, _settings),
            BaselineME = foc.BaselineMeFoc,
            BaselineAE = foc.BaselineAeFoc,
            BaselineMeCO2 = Co2ForMainEngines(foc.BaselineMeFoc, input, _settings),
            BaselineAeCO2 = Co2ForAuxEngines(foc.BaselineAeFoc, input, _settings),

            // Per-variant results: each tier adds one optimization level to the one below it
            Advanced = BuildVariantResult(new(input, foc, _settings, transitOptimal,
                new TierPlan(configMap["1"], savings.Advanced,
                    savings.L1Savings, 0, 0, null, null))),
            Pro = BuildVariantResult(new(input, foc, _settings, transitOptimal,
                new TierPlan(configMap["2"], savings.Pro,
                    savings.L1Savings, savings.L2Savings, 0, l2Details, null))),
            Premium = BuildVariantResult(new(input, foc, _settings, transitOptimal,
                new TierPlan(configMap["3"], savings.Premium,
                    savings.L1Savings, savings.L2Savings, savings.L3Savings, l2Details, l3Details)))
        };

        // Attach validation warnings
        if (warnings is { Count: > 0 })
        {
            result.Warnings.AddRange(warnings);
        }

        return result;
    }

    private async Task<(Level1Result L1, Level2Result L2, Level3Result L3, BatteryModeOutcome? Battery)>
        RunOptimizationPipelineAsync(CalculatorInput input, OperationalMode mode, double? overridePropulsionKw)
    {
        var modeHours = OperationalModes.HoursOf(input, mode);
        var (l1, battery) = await RunL1Async(input, mode, modeHours, overridePropulsionKw, input.BaselineIndex);
        var l2 = await _level2Service.OptimizeLoadSetpointsAsync(l1, input);
        // Increment E (Q4 working rule): DRC monetizes only the variation the battery didn't shave
        var hotelBand = battery is null ? 0 : HotelPeakShavingKw(battery.Allocation);
        var l3 = await _level3Service.CalculateDrcSavingsAsync(l2, input, modeHours, hotelBand);
        return (l1, l2, l3, battery);
    }

    #region Battery (Increment B — decisions D1/D2/D3, R3a dual-scenario)

    /// <summary>
    /// Level 1 for one mode, battery-aware. Inactive battery → today's exact code path.
    /// Active battery → demand adjusted by the uncovered spinning reserve (Excel R8) and the
    /// "third highest" default baseline; additionally runs the no-battery reference scenario
    /// (budget 0 ⇒ full variation carried by gensets) to compute the R3a "Battery benefit".
    /// </summary>
    private async Task<(Level1Result L1, BatteryModeOutcome? Battery)> RunL1Async(
        CalculatorInput input, OperationalMode mode, double modeHours,
        double? overridePropulsionKw, int? baselineIndex)
    {
        if (input.Battery?.AppliesTo(mode) != true)
            return (await _level1Service.FindOptimalCombinationAsync(input, mode, overridePropulsionKw, baselineIndex), null);

        var allocation = _batteryService.Allocate(mode, input, propulsionOverrideKw: overridePropulsionKw);
        var l1 = await _level1Service.FindOptimalCombinationAsync(
            input, mode, overridePropulsionKw, baselineIndex, ToAdjustment(allocation));

        var referenceAllocation = _batteryService.Allocate(
            mode, input, budgetOverrideKw: 0, propulsionOverrideKw: overridePropulsionKw);
        var referenceL1 = await _level1Service.FindOptimalCombinationAsync(
            input, mode, overridePropulsionKw, null, ToAdjustment(referenceAllocation));

        var benefitTons = Math.Max(0, referenceL1.OptimalFocTonPerHour - l1.OptimalFocTonPerHour) * modeHours;
        return (l1, new BatteryModeOutcome(allocation, benefitTons));
    }

    /// <summary>Hotel/mission-side ± band covered by the battery (offsets the L3 DRC variation).</summary>
    private static double HotelPeakShavingKw(BatteryModeAllocation allocation)
        => allocation.Loads
            .Where(l => !l.Load.IsThrustSide() && l.Function == BatteryFunction.PeakShaving)
            .Sum(l => l.CoveredBandKw);

    /// <summary>
    /// Uncovered reserve split by plant side: thrust-related loads raise the propulsion demand,
    /// electrical loads (hotel, mission consumers) raise the hotel demand.
    /// PropulsionPeakShavingKw = the battery's covered ± band on thrust loads — must flow through
    /// PTI when PTI is modelled (Increment C discharge gate).
    /// </summary>
    private static BatteryL1Adjustment ToAdjustment(BatteryModeAllocation allocation)
    {
        double propulsion = 0, hotel = 0, propulsionBand = 0;
        foreach (var load in allocation.Loads)
        {
            if (load.Load.IsThrustSide())
            {
                propulsion += load.UncoveredReserveKw;
                propulsionBand += load.CoveredBandKw;
            }
            else
            {
                hotel += load.UncoveredReserveKw;
            }
        }
        return new BatteryL1Adjustment(propulsion, hotel, propulsionBand);
    }

    private static BatteryDetails? BuildBatteryDetails(
        CalculatorInput input, List<ModePipelineResult> modes)
    {
        // Modes the battery did not apply to contribute nothing; no contributing mode at all
        // (e.g. battery assigned to Port with 0 port hours) ⇒ no panel, not an empty one (G2/B10).
        var outcomes = modes.Select(m => m.Battery).OfType<BatteryModeOutcome>().ToList();
        if (input.Battery?.IsActive != true || outcomes.Count == 0)
            return null;

        var benefit = outcomes.Sum(o => o.BenefitTonPerYear);
        return new BatteryDetails
        {
            CapacityKwh = input.Battery.CapacityKwh,
            PowerKw = input.Battery.PowerKw,
            SpinningReserveKw = outcomes.Sum(o => o.Allocation.AdditionalSpinningReserveKw),
            PeakShavingKw = outcomes.Sum(o => o.Allocation.PeakShavingBandKw),
            BenefitFocTonPerYear = benefit,
            BenefitCostPerYear = benefit * input.FuelPrice,
            ModeAllocations = outcomes.Select(o => o.Allocation).ToList()
        };
    }

    #endregion

    #region FOC & Savings Aggregation

    private static FocBreakdown CalculateFocBreakdown(List<ModePipelineResult> modes)
    {
        return new FocBreakdown(
            BaselineFoc: modes.Sum(m => m.L1.BaselineFocTonPerHour * m.Hours),
            BaselineMeFoc: modes.Sum(m => m.L1.BaselineCombination.MeFocTonPerHour * m.Hours),
            BaselineAeFoc: modes.Sum(m => m.L1.BaselineCombination.AeFocTonPerHour * m.Hours),
            OptimalMeFoc: modes.Sum(m => m.L1.OptimalCombination.MeFocTonPerHour * m.Hours),
            OptimalAeFoc: modes.Sum(m => m.L1.OptimalCombination.AeFocTonPerHour * m.Hours)
        );
    }

    private static TierSavings CalculateTierSavings(List<ModePipelineResult> modes)
    {
        var l1 = modes.Sum(m => (m.L1.BaselineFocTonPerHour - m.L1.OptimalFocTonPerHour) * m.Hours);
        var l2 = modes.Sum(m => m.L2.Level2SavingsTonPerHour * m.Hours);
        var l3 = modes.Sum(m => m.L3.DrcSavingsTonPerYear);

        return new TierSavings(
            Advanced: l1,
            Pro: l1 + l2,
            Premium: l1 + l2 + l3,
            L1Savings: l1,
            L2Savings: l2,
            L3Savings: l3);
    }

    #endregion

    #region Result Building

    private static VariantResult BuildVariantResult(BuildResultContext ctx)
    {
        var optimizedFoc = ctx.Foc.BaselineFoc - ctx.Tier.TotalSavingsTon;
        var financial = CalculateFinancials(ctx.Tier.TotalSavingsTon, ctx.Input.FuelPrice, ctx.Tier.Config, ctx.Settings);

        // Distribute optimized FOC between ME and AE using the OPTIMIZED plant's own split —
        // the baseline may run a different engine mix (e.g. baseline has AEs on while the optimum
        // runs none), which previously assigned fuel to engines reported as "not required".
        // Falls back to the baseline ratio only if the optimum has no FOC at all.
        var optimalTotalFoc = ctx.Foc.OptimalMeFoc + ctx.Foc.OptimalAeFoc;
        var meRatio = optimalTotalFoc > 0
            ? ctx.Foc.OptimalMeFoc / optimalTotalFoc
            : ctx.Foc.BaselineFoc > 0 ? ctx.Foc.BaselineMeFoc / ctx.Foc.BaselineFoc : 0.5;
        var optimizedMeFocBreakdown = optimizedFoc * meRatio;
        var optimizedAeFocBreakdown = optimizedFoc * (1 - meRatio);

        // Per-engine CO2: ME and AE may burn different fuels.
        // When fuel is null, Co2FactorFor returns the single Co2Factor for both → identical to legacy.
        var baselineCo2 = Co2ForEngines(ctx.Foc.BaselineMeFoc, ctx.Foc.BaselineAeFoc, ctx.Input, ctx.Settings);
        var optimizedCo2 = Co2ForEngines(optimizedMeFocBreakdown, optimizedAeFocBreakdown, ctx.Input, ctx.Settings);
        var co2Reduction = baselineCo2 - optimizedCo2;

        // ME/AE load % from Transit L1 optimal combination (what the client displays)
        var meLoadPct = ctx.TransitOptimalCombo.MeLoadPercent * 100;
        var aeLoadPct = ctx.TransitOptimalCombo.AeLoadPercent * 100;

        // For L2+ tiers, use the optimized AE load from Level 2 setpoints
        if (ctx.Tier.L2Details?.OptimalSetpoints is { Count: > 0 })
        {
            var activeAeSetpoints = ctx.Tier.L2Details.OptimalSetpoints
                .Where(s => s.GeneratorType == GeneratorType.AE && s.LoadPercent > 0)
                .ToList();
            if (activeAeSetpoints.Count > 0)
                aeLoadPct = activeAeSetpoints.Average(s => s.LoadPercent) * 100;
        }

        return new VariantResult
        {
            OptimizedFOC = optimizedFoc,
            FuelSavings = ctx.Tier.TotalSavingsTon,
            FuelSavingsPercentage = ctx.Foc.BaselineFoc > 0 ? (ctx.Tier.TotalSavingsTon / ctx.Foc.BaselineFoc) * 100 : 0,
            OptimizedCO2 = optimizedCo2,
            Co2Reduction = co2Reduction,
            Co2ReductionPercentage = baselineCo2 > 0 ? (co2Reduction / baselineCo2) * 100 : 0,
            AnnualCostSavings = financial.AnnualCostSavingsUsd,
            TotalInvestment = financial.TotalInvestmentUsd,
            PaybackPeriod = financial.PaybackPeriod,
            Roi = financial.Roi,
            EfficiencyFactor = ctx.Foc.BaselineFoc > 0 ? optimizedFoc / ctx.Foc.BaselineFoc : 1.0,
            OptimizedME = optimizedMeFocBreakdown,
            OptimizedAE = optimizedAeFocBreakdown,
            OptimizedMeCO2 = Co2ForMainEngines(optimizedMeFocBreakdown, ctx.Input, ctx.Settings),
            OptimizedAeCO2 = Co2ForAuxEngines(optimizedAeFocBreakdown, ctx.Input, ctx.Settings),
            MainEngineLoadPercent = meLoadPct,
            AuxiliaryEngineLoadPercent = aeLoadPct,
            Level2Details = ctx.Tier.L2Details,
            Level3Details = ctx.Tier.L3Details,
            Level1SavingsTonPerYear = ctx.Tier.L1Savings,
            Level2SavingsTonPerYear = ctx.Tier.L2Savings,
            Level3SavingsTonPerYear = ctx.Tier.L3Savings
        };
    }

    private record FinancialMetrics(
        double AnnualCostSavingsUsd, double TotalInvestmentUsd, double PaybackPeriod, double Roi);

    // CO2 is computed per-engine in BuildVariantResult (fuel-aware). This handles cost/investment only.
    private static FinancialMetrics CalculateFinancials(
        double fuelSavingsTon, double fuelPrice, IntegrationLevelConfig config, CalculatorSettings settings)
    {
        var costSavings = fuelSavingsTon * fuelPrice;
        var investment = (config.IemsPriceNOK + config.CommissioningNOK) / settings.UsdToNokRate;
        var payback = costSavings > 0 ? investment / costSavings : 0;

        var roi = investment > 0
            ? ((costSavings * settings.RoiAnalysisYears - investment) / investment) * 100
            : 0;

        return new FinancialMetrics(costSavings, investment, payback, roi);
    }

    /// <summary>
    /// Per-engine CO2 (tons): ME and AE FOC each multiplied by their fuel's factor.
    /// Falls back to the single Co2Factor when a fuel type is null/unknown (legacy-identical:
    /// equal factors collapse meFoc+aeFoc back to totalFoc * Co2Factor).
    /// </summary>
    private static double Co2ForEngines(double meFoc, double aeFoc, CalculatorInput input, CalculatorSettings settings)
        => Co2ForMainEngines(meFoc, input, settings) + Co2ForAuxEngines(aeFoc, input, settings);

    /// <summary>ME CO2 (tons) with the main fuel's factor — the single source for per-engine CO2.</summary>
    private static double Co2ForMainEngines(double meFoc, CalculatorInput input, CalculatorSettings settings)
        => meFoc * settings.Co2FactorFor(input.MainFuelType);

    /// <summary>AE CO2 (tons) with the aux fuel's factor — the single source for per-engine CO2.</summary>
    private static double Co2ForAuxEngines(double aeFoc, CalculatorInput input, CalculatorSettings settings)
        => aeFoc * settings.Co2FactorFor(input.AuxFuelType);

    #endregion

    #region Power Demands

    private static PowerDemands BuildPowerDemands(
        List<ModePipelineResult> modes,
        CalculatorInput input, SailContributionResult? sailResult,
        EngineCapacities capacities)
    {
        var totalHours = modes.Sum(m => m.Hours);

        var (mePower, sgPower, aePower) = CalculateWeightedPower(modes, totalHours);
        var (meAvgLoad, aeAvgLoad) = CalculateWeightedLoadPercent(modes);
        var modeBreakdowns = BuildModeBreakdowns(modes, input.DpEnabled, sailResult);

        return new PowerDemands
        {
            MainEnginePowerKw = mePower,
            ShaftGeneratorPowerKw = sgPower,
            AuxiliaryEnginePowerKw = aePower,
            TotalPowerKw = mePower + sgPower + aePower,
            TotalEnergyKwh = (mePower + sgPower + aePower) * totalHours,
            MeInstalled = capacities.MainEngineTotalCapacityKw,
            AeInstalled = capacities.AuxEnginesMaxPower,
            MeAverageLoadPercent = meAvgLoad,
            AeAverageLoadPercent = aeAvgLoad,
            ModeBreakdowns = modeBreakdowns
        };
    }

    private static (double me, double sg, double ae) CalculateWeightedPower(
        List<ModePipelineResult> modes, double totalHours)
    {
        if (totalHours <= 0) return (0, 0, 0);

        double me = modes.Sum(m => m.L1.OptimalCombination.MePowerKw * m.Hours);
        double sg = modes.Sum(m => m.L1.OptimalCombination.SgPowerKw * m.Hours);
        double ae = modes.Sum(m => m.L1.OptimalCombination.AePowerKw * m.Hours);

        return (me / totalHours, sg / totalHours, ae / totalHours);
    }

    /// <summary>
    /// Compute average load % weighted only by the hours each engine type is active.
    /// This avoids diluting load across the entire operation period when an engine
    /// only runs in certain modes (e.g. AE only in DP).
    /// </summary>
    private static (double meLoadPct, double aeLoadPct) CalculateWeightedLoadPercent(
        List<ModePipelineResult> modes)
    {
        double meNum = 0, meDen = 0;
        foreach (var m in modes)
        {
            if (m.L1.OptimalCombination.ActiveMeCount > 0 && m.Hours > 0)
            {
                meNum += m.L1.OptimalCombination.MeLoadPercent * m.Hours;
                meDen += m.Hours;
            }
        }

        double aeNum = 0, aeDen = 0;
        foreach (var m in modes)
        {
            if (m.L1.OptimalCombination.ActiveAeCount > 0 && m.Hours > 0)
            {
                aeNum += m.L1.OptimalCombination.AeLoadPercent * m.Hours;
                aeDen += m.Hours;
            }
        }

        return (
            meDen > 0 ? (meNum / meDen) * 100 : 0,
            aeDen > 0 ? (aeNum / aeDen) * 100 : 0);
    }

    private static List<ModePowerBreakdown> BuildModeBreakdowns(
        List<ModePipelineResult> modes, bool dpEnabled, SailContributionResult? sailResult)
    {
        var breakdowns = new List<ModePowerBreakdown>();

        foreach (var m in modes)
        {
            var sailPower = m.Mode == OperationalMode.Transit ? (sailResult?.SailPowerKw ?? 0) : 0;
            if (m.Hours > 0)
                breakdowns.Add(BuildModeBreakdown(m.L1.OptimalCombination, m.Mode, m.Hours, sailPower));
            else if (m.Mode == OperationalMode.DP && dpEnabled)
                breakdowns.Add(new ModePowerBreakdown { Mode = OperationalMode.DP });
        }

        return breakdowns;
    }

    private static ModePowerBreakdown BuildModeBreakdown(
        EngineCombination combo, OperationalMode mode, double hours, double sailPowerKw) => new()
        {
            Mode = mode,
            Hours = hours,
            PropulsionMainEngineKw = combo.MePowerKw - combo.SgPowerKw,
            PropulsionSailKw = sailPowerKw,
            HotelShaftGenKw = combo.SgPowerKw,
            HotelAuxGenKw = combo.AePowerKw,
            EnergyMainEngineKwh = combo.MePowerKw * hours,
            EnergyAuxGenKwh = combo.AePowerKw * hours
        };

    #endregion

    #region Engine Capacities

    private static EngineCapacities BuildEngineCapacities(CalculatorInput input)
    {
        var meTotal = input.TotalMeCapacity;
        var sgTotal = input.TotalSgCapacity;
        var aeTotal = input.TotalAeCapacity;

        if (meTotal <= 0 || aeTotal <= 0 || input.AeCount < 1)
            throw new ArgumentException("Invalid engine capacities: ME and AE must be > 0, AeCount >= 1");

        return new EngineCapacities(meTotal, sgTotal, aeTotal);
    }

    #endregion

    #region Sail

    // Note: SailInstalled is a UI-only flag; SailEnabled controls whether sail is included in calculations
    private async Task<SailContributionResult?> CalculateSailIfEnabledAsync(CalculatorInput input)
    {
        if (!input.SailEnabled
            || !input.TrueWindSpeed.HasValue || !input.WindAngleRelVessel.HasValue
            || input.TrueWindSpeed.Value <= 0 || input.VesselSpeedKnots <= 0)
            return null;

        return await _sailContributionService.CalculateSailContributionAsync(
            input.TrueWindSpeed.Value,
            input.WindAngleRelVessel.Value,
            input.VesselSpeedKnots,
            input.EffectivePropulsionPower);
    }

    #endregion
}