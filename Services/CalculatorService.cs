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
    private readonly CalculatorSettings _settings;

    public CalculatorService(
        IKSailCalcConfigRepository configRepository,
        ISailContributionService sailContributionService,
        ILevel1OptimizationService level1Service,
        ILevel2OptimizationService level2Service,
        ILevel3DrcService level3Service,
        IOptions<CalculatorSettings> settings)
    {
        _configRepository = configRepository;
        _sailContributionService = sailContributionService;
        _level1Service = level1Service;
        _level2Service = level2Service;
        _level3Service = level3Service;
        _settings = settings.Value;
    }

    #region Parameter Objects

    private record FocBreakdown(
        double BaselineFoc, double BaselineMeFoc, double BaselineAeFoc, double OptimalMeFoc);

    private record TierSavings(double Advanced, double Pro, double Premium,
        double L1Savings, double L2Savings, double L3Savings);

    private record BuildResultContext(
        CalculatorInput Input,
        IntegrationLevelConfig Config,
        FocBreakdown Foc,
        double FuelSavingsTon,
        Level2Details? L2Details,
        Level3Details? L3Details,
        CalculatorSettings Settings,
        double L1Savings,
        double L2Savings,
        double L3Savings,
        EngineCombination TransitOptimalCombo);

    private record ModePipelineResult(
        OperationalMode Mode, Level1Result L1, Level2Result L2, Level3Result L3, double Hours);

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
        modeResults.Add(new(OperationalMode.Transit, transit.L1, transit.L2, transit.L3, input.TransitHours));

        // Non-transit modes: only L1 for baseline/optimal FOC, no L2/L3 optimization
        var emptyL2 = new Level2Result();
        var emptyL3 = new Level3Result();

        if (input.DpEnabled && (input.DPHours ?? 0) > 0)
        {
            var l1 = await _level1Service.FindOptimalCombinationAsync(input, OperationalMode.DP);
            modeResults.Add(new(OperationalMode.DP, l1, emptyL2, emptyL3, input.DPHours ?? 0));
        }

        if (input.PortHours > 0)
        {
            var l1 = await _level1Service.FindOptimalCombinationAsync(input, OperationalMode.Port);
            modeResults.Add(new(OperationalMode.Port, l1, emptyL2, emptyL3, input.PortHours));
        }

        if (input.AnchorHours > 0)
        {
            var l1 = await _level1Service.FindOptimalCombinationAsync(input, OperationalMode.Anchor);
            modeResults.Add(new(OperationalMode.Anchor, l1, emptyL2, emptyL3, input.AnchorHours));
        }

        if (input.ManeuveringHours > 0)
        {
            var l1 = await _level1Service.FindOptimalCombinationAsync(input, OperationalMode.Maneuvering);
            modeResults.Add(new(OperationalMode.Maneuvering, l1, emptyL2, emptyL3, input.ManeuveringHours));
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
            BaselineFOC = foc.BaselineFoc,
            BaselineCO2 = Co2ForEngines(foc.BaselineMeFoc, foc.BaselineAeFoc, input, _settings),
            BaselineME = foc.BaselineMeFoc,
            BaselineAE = foc.BaselineAeFoc,

            // Per-variant results
            Advanced = BuildVariantResult(new(input, configMap["1"], foc, savings.Advanced,
                null, null, _settings, savings.L1Savings, 0, 0, transitOptimal)),
            Pro = BuildVariantResult(new(input, configMap["2"], foc, savings.Pro,
                l2Details, null, _settings, savings.L1Savings, savings.L2Savings, 0, transitOptimal)),
            Premium = BuildVariantResult(new(input, configMap["3"], foc, savings.Premium,
                l2Details, l3Details, _settings, savings.L1Savings, savings.L2Savings, savings.L3Savings, transitOptimal))
        };

        // Attach validation warnings
        if (warnings is { Count: > 0 })
        {
            result.Warnings.AddRange(warnings);
        }

        return result;
    }

    private async Task<(Level1Result L1, Level2Result L2, Level3Result L3)> RunOptimizationPipelineAsync(
        CalculatorInput input, OperationalMode mode, double? overridePropulsionKw)
    {
        var modeHours = GetModeHours(input, mode);
        var l1 = await _level1Service.FindOptimalCombinationAsync(input, mode, overridePropulsionKw, input.BaselineIndex);
        var l2 = await _level2Service.OptimizeLoadSetpointsAsync(l1, input);
        var l3 = await _level3Service.CalculateDrcSavingsAsync(l2, input, modeHours);
        return (l1, l2, l3);
    }

    private static double GetModeHours(CalculatorInput input, OperationalMode mode) => mode switch
    {
        OperationalMode.Transit => input.TransitHours,
        OperationalMode.DP => input.DPHours ?? 0,
        OperationalMode.Port => input.PortHours,
        OperationalMode.Anchor => input.AnchorHours,
        OperationalMode.Maneuvering => input.ManeuveringHours,
        _ => 0
    };

    #region FOC & Savings Aggregation

    private static FocBreakdown CalculateFocBreakdown(List<ModePipelineResult> modes)
    {
        return new FocBreakdown(
            BaselineFoc: modes.Sum(m => m.L1.BaselineFocTonPerHour * m.Hours),
            BaselineMeFoc: modes.Sum(m => m.L1.BaselineCombination.MeFocTonPerHour * m.Hours),
            BaselineAeFoc: modes.Sum(m => m.L1.BaselineCombination.AeFocTonPerHour * m.Hours),
            OptimalMeFoc: modes.Sum(m => m.L1.OptimalCombination.MeFocTonPerHour * m.Hours)
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
        var optimizedFoc = ctx.Foc.BaselineFoc - ctx.FuelSavingsTon;
        var financial = CalculateFinancials(ctx.FuelSavingsTon, ctx.Input.FuelPrice, ctx.Config, ctx.Settings);

        // Distribute optimized FOC proportionally between ME and AE based on baseline ratio
        var meRatio = ctx.Foc.BaselineFoc > 0 ? ctx.Foc.BaselineMeFoc / ctx.Foc.BaselineFoc : 0.5;
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
        if (ctx.L2Details?.OptimalSetpoints is { Count: > 0 })
        {
            var activeAeSetpoints = ctx.L2Details.OptimalSetpoints
                .Where(s => s.GeneratorType == GeneratorType.AE && s.LoadPercent > 0)
                .ToList();
            if (activeAeSetpoints.Count > 0)
                aeLoadPct = activeAeSetpoints.Average(s => s.LoadPercent) * 100;
        }

        return new VariantResult
        {
            OptimizedFOC = optimizedFoc,
            FuelSavings = ctx.FuelSavingsTon,
            FuelSavingsPercentage = ctx.Foc.BaselineFoc > 0 ? (ctx.FuelSavingsTon / ctx.Foc.BaselineFoc) * 100 : 0,
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
            MainEngineLoadPercent = meLoadPct,
            AuxiliaryEngineLoadPercent = aeLoadPct,
            Level2Details = ctx.L2Details,
            Level3Details = ctx.L3Details,
            Level1SavingsTonPerYear = ctx.L1Savings,
            Level2SavingsTonPerYear = ctx.L2Savings,
            Level3SavingsTonPerYear = ctx.L3Savings
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

        const int analysisYears = 10;
        var roi = investment > 0 ? ((costSavings * analysisYears - investment) / investment) * 100 : 0;

        return new FinancialMetrics(costSavings, investment, payback, roi);
    }

    /// <summary>
    /// Per-engine CO2 (tons): ME and AE FOC each multiplied by their fuel's factor.
    /// Falls back to the single Co2Factor when a fuel type is null/unknown (legacy-identical:
    /// equal factors collapse meFoc+aeFoc back to totalFoc * Co2Factor).
    /// </summary>
    private static double Co2ForEngines(double meFoc, double aeFoc, CalculatorInput input, CalculatorSettings settings)
        => meFoc * settings.Co2FactorFor(input.MainFuelType) + aeFoc * settings.Co2FactorFor(input.AuxFuelType);

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