using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Services.Results;

/// <summary>
/// One product tier. A tier is defined by two things only: which pricing row it reads, and the
/// highest optimization level it includes. Everything else about the tier — its total savings,
/// which per-level savings it reports, which detail panels travel with it — follows from
/// <see cref="HighestLevel"/> instead of being spelled out per tier.
/// </summary>
internal sealed record IntegrationTier(string ConfigKey, int HighestLevel)
{
    internal static readonly IntegrationTier Advanced = new("1", HighestLevel: 1);
    internal static readonly IntegrationTier Pro = new("2", HighestLevel: 2);
    internal static readonly IntegrationTier Premium = new("3", HighestLevel: 3);
}

/// <summary>
/// Everything the tiers share: the annual FOC and savings figures, the Transit optimum the client
/// displays, and the L2/L3 detail panels a tier may or may not include.
/// </summary>
internal sealed record TierInputs(
    CalculatorInput Input,
    CalculatorSettings Settings,
    FocBreakdown Foc,
    SavingsBreakdown Savings,
    EngineCombination TransitOptimalCombination,
    Level2Details Level2Details,
    Level3Details Level3Details);

/// <summary>
/// Turns the shared annual figures into what one product tier reports: savings, CO2, financials
/// and the engine load percentages. Pure function — no configuration lookup, no I/O.
/// </summary>
internal static class TierResultBuilder
{
    internal static VariantResult Build(TierInputs ctx, IntegrationTier tier, IntegrationLevelConfig config)
    {
        // Each tier adds one optimization level to the one below it; levels above the tier's own
        // contribute nothing and are reported as zero.
        var l1Savings = ctx.Savings.L1;
        var l2Savings = tier.HighestLevel >= 2 ? ctx.Savings.L2 : 0;
        var l3Savings = tier.HighestLevel >= 3 ? ctx.Savings.L3 : 0;
        var totalSavingsTon = ctx.Savings.TotalUpTo(tier.HighestLevel);

        // The detail panels travel with the levels the tier includes.
        var tierL2Details = tier.HighestLevel >= 2 ? ctx.Level2Details : null;
        var tierL3Details = tier.HighestLevel >= 3 ? ctx.Level3Details : null;

        var optimizedFoc = ctx.Foc.BaselineFoc - totalSavingsTon;
        var financial = CalculateFinancials(totalSavingsTon, ctx.Input.FuelPrice, config, ctx.Settings);

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
        var baselineCo2 = Co2Calculator.ForEngines(ctx.Foc.BaselineMeFoc, ctx.Foc.BaselineAeFoc, ctx.Input, ctx.Settings);
        var optimizedCo2 = Co2Calculator.ForEngines(optimizedMeFocBreakdown, optimizedAeFocBreakdown, ctx.Input, ctx.Settings);
        var co2Reduction = baselineCo2 - optimizedCo2;

        // ME/AE load % from Transit L1 optimal combination (what the client displays)
        var meLoadPct = ctx.TransitOptimalCombination.MeLoadPercent * 100;
        var aeLoadPct = ctx.TransitOptimalCombination.AeLoadPercent * 100;

        // For L2+ tiers, use the optimized AE load from Level 2 setpoints
        if (tierL2Details?.OptimalSetpoints is { Count: > 0 })
        {
            var activeAeSetpoints = tierL2Details.OptimalSetpoints
                .Where(s => s.GeneratorType == GeneratorType.AE && s.LoadPercent > 0)
                .ToList();
            if (activeAeSetpoints.Count > 0)
                aeLoadPct = activeAeSetpoints.Average(s => s.LoadPercent) * 100;
        }

        return new VariantResult
        {
            OptimizedFOC = optimizedFoc,
            FuelSavings = totalSavingsTon,
            FuelSavingsPercentage = ctx.Foc.BaselineFoc > 0 ? (totalSavingsTon / ctx.Foc.BaselineFoc) * 100 : 0,
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
            OptimizedMeCO2 = Co2Calculator.ForMainEngines(optimizedMeFocBreakdown, ctx.Input, ctx.Settings),
            OptimizedAeCO2 = Co2Calculator.ForAuxEngines(optimizedAeFocBreakdown, ctx.Input, ctx.Settings),
            MainEngineLoadPercent = meLoadPct,
            AuxiliaryEngineLoadPercent = aeLoadPct,
            Level2Details = tierL2Details,
            Level3Details = tierL3Details,
            Level1SavingsTonPerYear = l1Savings,
            Level2SavingsTonPerYear = l2Savings,
            Level3SavingsTonPerYear = l3Savings
        };
    }

    private record FinancialMetrics(
        double AnnualCostSavingsUsd, double TotalInvestmentUsd, double PaybackPeriod, double Roi);

    // CO2 is computed per-engine in Build (fuel-aware). This handles cost/investment only.
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
}

/// <summary>
/// Per-engine CO2 (tons): ME and AE FOC each multiplied by their fuel's factor.
/// Falls back to the single Co2Factor when a fuel type is null/unknown (legacy-identical:
/// equal factors collapse meFoc+aeFoc back to totalFoc * Co2Factor).
/// Used both for the shared baseline figures and per tier.
/// </summary>
internal static class Co2Calculator
{
    internal static double ForEngines(double meFoc, double aeFoc, CalculatorInput input, CalculatorSettings settings)
        => ForMainEngines(meFoc, input, settings) + ForAuxEngines(aeFoc, input, settings);

    /// <summary>ME CO2 (tons) with the main fuel's factor — the single source for per-engine CO2.</summary>
    internal static double ForMainEngines(double meFoc, CalculatorInput input, CalculatorSettings settings)
        => meFoc * settings.Co2FactorFor(input.MainFuelType);

    /// <summary>AE CO2 (tons) with the aux fuel's factor — the single source for per-engine CO2.</summary>
    internal static double ForAuxEngines(double aeFoc, CalculatorInput input, CalculatorSettings settings)
        => aeFoc * settings.Co2FactorFor(input.AuxFuelType);
}
