using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Results;
using KSailCalc.Tests.TestHelpers;

namespace KSailCalc.Tests.Calculation;

/// <summary>
/// Direct unit tests for the pure builders extracted out of CalculatorService (Refactor story R-A).
/// Being testable without the orchestrator is the point of the extraction — these cover the rules
/// the golden snapshots only exercise indirectly.
/// </summary>
public class ResultBuildersTests
{
    // ─── helpers ────────────────────────────────────────────────────────────────

    private static EngineCombination Combo(
        int me = 1, int ae = 1, double mePower = 4000, double sgPower = 0, double aePower = 500,
        double meLoad = 0.8, double aeLoad = 0.6, double meFoc = 0.8, double aeFoc = 0.1)
        => new()
        {
            ActiveMeCount = me,
            ActiveAeCount = ae,
            MePowerKw = mePower,
            SgPowerKw = sgPower,
            AePowerKw = aePower,
            MeLoadPercent = meLoad,
            AeLoadPercent = aeLoad,
            MeFocTonPerHour = meFoc,
            AeFocTonPerHour = aeFoc,
            FocTonPerHour = meFoc + aeFoc
        };

    private static ModePipelineResult Mode(
        OperationalMode mode, double hours,
        EngineCombination optimal, EngineCombination baseline,
        double l2SavingsPerHour = 0, double l3SavingsPerYear = 0)
        => new(
            mode,
            new Level1Result
            {
                OptimalCombination = optimal,
                BaselineCombination = baseline,
                OptimalFocTonPerHour = optimal.FocTonPerHour,
                BaselineFocTonPerHour = baseline.FocTonPerHour
            },
            new Level2Result { Level2SavingsTonPerHour = l2SavingsPerHour },
            new Level3Result { DrcSavingsTonPerYear = l3SavingsPerYear },
            hours,
            null);

    // ─── SavingsAggregator ──────────────────────────────────────────────────────

    [Fact]
    public void SavingsAggregator_WeightsEveryFocFigureByModeHours()
    {
        var modes = new List<ModePipelineResult>
        {
            Mode(OperationalMode.Transit, hours: 100,
                optimal: Combo(meFoc: 0.8, aeFoc: 0.1),
                baseline: Combo(meFoc: 1.0, aeFoc: 0.2)),
            Mode(OperationalMode.Port, hours: 10,
                optimal: Combo(meFoc: 0.0, aeFoc: 0.3),
                baseline: Combo(meFoc: 0.0, aeFoc: 0.5))
        };

        var foc = SavingsAggregator.CalculateFocBreakdown(modes);

        foc.BaselineFoc.Should().BeApproximately(1.2 * 100 + 0.5 * 10, 1e-9);
        foc.BaselineMeFoc.Should().BeApproximately(1.0 * 100 + 0.0 * 10, 1e-9);
        foc.BaselineAeFoc.Should().BeApproximately(0.2 * 100 + 0.5 * 10, 1e-9);
        foc.OptimalMeFoc.Should().BeApproximately(0.8 * 100 + 0.0 * 10, 1e-9);
        foc.OptimalAeFoc.Should().BeApproximately(0.1 * 100 + 0.3 * 10, 1e-9);
    }

    [Fact]
    public void SavingsAggregator_L1AndL2ScaleWithHours_L3IsAlreadyAnnual()
    {
        var modes = new List<ModePipelineResult>
        {
            Mode(OperationalMode.Transit, hours: 100,
                optimal: Combo(meFoc: 0.8, aeFoc: 0.1),   // 0.9 t/h
                baseline: Combo(meFoc: 1.0, aeFoc: 0.2),  // 1.2 t/h
                l2SavingsPerHour: 0.05,
                l3SavingsPerYear: 7)
        };

        var savings = SavingsAggregator.CalculateSavings(modes);

        savings.L1.Should().BeApproximately((1.2 - 0.9) * 100, 1e-9);
        savings.L2.Should().BeApproximately(0.05 * 100, 1e-9);
        savings.L3.Should().Be(7, "L3 DRC savings are already tons/year, not per hour");
    }

    [Theory]
    [InlineData(1, 30)]
    [InlineData(2, 35)]
    [InlineData(3, 42)]
    public void SavingsBreakdown_TotalIsCumulativeUpToTheTierLevel(int highestLevel, double expected)
    {
        new SavingsBreakdown(30, 5, 7).TotalUpTo(highestLevel).Should().Be(expected);
    }

    // ─── PowerDemandsBuilder ────────────────────────────────────────────────────

    [Fact]
    public void PowerDemandsBuilder_AveragesPowerOverAllHoursButLoadOnlyOverActiveHours()
    {
        // Transit runs ME+AE; Port runs AE only. The ME average load must NOT be diluted by the
        // port hours, while the ME average POWER is a plain hours-weighted average over both.
        var modes = new List<ModePipelineResult>
        {
            Mode(OperationalMode.Transit, hours: 100,
                optimal: Combo(me: 1, ae: 1, mePower: 4000, aePower: 500, meLoad: 0.80, aeLoad: 0.60),
                baseline: Combo()),
            Mode(OperationalMode.Port, hours: 100,
                optimal: Combo(me: 0, ae: 1, mePower: 0, aePower: 300, meLoad: 0, aeLoad: 0.40),
                baseline: Combo())
        };

        var input = CalculatorInputBuilder.Default().Build();
        var demands = PowerDemandsBuilder.Build(
            modes, input, sailResult: null);

        demands.MainEnginePowerKw.Should().BeApproximately(2000, 1e-9, "4000 kW over half the hours");
        demands.MeAverageLoadPercent.Should().BeApproximately(80, 1e-9, "port hours have no ME running");
        demands.AeAverageLoadPercent.Should().BeApproximately(50, 1e-9, "(60 + 40) / 2");
        demands.MeInstalled.Should().Be(10000);
        demands.AeInstalled.Should().Be(2400);
        demands.ModeBreakdowns.Should().HaveCount(2);
    }

    [Fact]
    public void PowerDemandsBuilder_EmitsAnEmptyDpBreakdown_WhenDpIsEnabledWithZeroHours()
    {
        var modes = new List<ModePipelineResult>
        {
            Mode(OperationalMode.Transit, hours: 100, optimal: Combo(), baseline: Combo()),
            Mode(OperationalMode.DP, hours: 0, optimal: Combo(), baseline: Combo())
        };

        var input = CalculatorInputBuilder.Default().WithDPMode(0, 500, 2000).Build();
        var demands = PowerDemandsBuilder.Build(
            modes, input, sailResult: null);

        demands.ModeBreakdowns.Should().HaveCount(2);
        demands.ModeBreakdowns.Last().Mode.Should().Be(OperationalMode.DP);
        demands.ModeBreakdowns.Last().Hours.Should().Be(0);
    }

    // ─── TierResultBuilder ──────────────────────────────────────────────────────

    private static TierInputs Tier(SavingsBreakdown savings, CalculatorSettings? settings = null)
        => new(
            CalculatorInputBuilder.Default().WithFuelPrice(600).Build(),
            settings ?? new CalculatorSettings { Co2Factor = 3.206, UsdToNokRate = 10, RoiAnalysisYears = 10 },
            new FocBreakdown(BaselineFoc: 1000, BaselineMeFoc: 800, BaselineAeFoc: 200,
                OptimalMeFoc: 760, OptimalAeFoc: 190),
            savings,
            Combo(meLoad: 0.80, aeLoad: 0.60),
            new Level2Details
            {
                OptimalSetpoints = new List<GeneratorSetpoint>
                {
                    new() { GeneratorType = GeneratorType.AE, LoadPercent = 0.70, CapacityKw = 800, PowerKw = 560 }
                }
            },
            new Level3Details { DrcSavingsTonPerYear = 7 });

    private static readonly IntegrationLevelConfig Pricing =
        new() { IntegrationLevelId = 1, IemsPriceNOK = 1_000_000, CommissioningNOK = 500_000 };

    [Fact]
    public void TierResultBuilder_AdvancedReportsOnlyL1AndCarriesNoDetailPanels()
    {
        var result = TierResultBuilder.Build(
            Tier(new SavingsBreakdown(30, 5, 7)), IntegrationTier.Advanced, Pricing);

        result.FuelSavings.Should().Be(30);
        result.Level1SavingsTonPerYear.Should().Be(30);
        result.Level2SavingsTonPerYear.Should().Be(0);
        result.Level3SavingsTonPerYear.Should().Be(0);
        result.Level2Details.Should().BeNull();
        result.Level3Details.Should().BeNull();
    }

    [Fact]
    public void TierResultBuilder_ProAddsL2AndItsPanel_ButNotL3()
    {
        var result = TierResultBuilder.Build(
            Tier(new SavingsBreakdown(30, 5, 7)), IntegrationTier.Pro, Pricing);

        result.FuelSavings.Should().Be(35);
        result.Level2SavingsTonPerYear.Should().Be(5);
        result.Level3SavingsTonPerYear.Should().Be(0);
        result.Level2Details.Should().NotBeNull();
        result.Level3Details.Should().BeNull();
    }

    [Fact]
    public void TierResultBuilder_PremiumAddsEveryLevelAndBothPanels()
    {
        var result = TierResultBuilder.Build(
            Tier(new SavingsBreakdown(30, 5, 7)), IntegrationTier.Premium, Pricing);

        result.FuelSavings.Should().Be(42);
        result.Level3SavingsTonPerYear.Should().Be(7);
        result.Level2Details.Should().NotBeNull();
        result.Level3Details.Should().NotBeNull();
    }

    [Fact]
    public void TierResultBuilder_AeLoadComesFromLevel2Setpoints_OnlyForTiersThatIncludeLevel2()
    {
        var advanced = TierResultBuilder.Build(
            Tier(new SavingsBreakdown(30, 5, 7)), IntegrationTier.Advanced, Pricing);
        var pro = TierResultBuilder.Build(
            Tier(new SavingsBreakdown(30, 5, 7)), IntegrationTier.Pro, Pricing);

        advanced.AuxiliaryEngineLoadPercent.Should().BeApproximately(60, 1e-9, "L1 optimal combination");
        pro.AuxiliaryEngineLoadPercent.Should().BeApproximately(70, 1e-9, "L2 optimized setpoint");
        advanced.MainEngineLoadPercent.Should().BeApproximately(80, 1e-9, "ME load is never overridden by L2");
    }

    [Fact]
    public void TierResultBuilder_ComputesFinancialsFromThePricingRow()
    {
        var result = TierResultBuilder.Build(
            Tier(new SavingsBreakdown(30, 5, 7)), IntegrationTier.Premium, Pricing);

        var expectedInvestment = (1_000_000d + 500_000d) / 10;  // NOK → USD
        var expectedCostSavings = 42 * 600d;

        result.TotalInvestment.Should().BeApproximately(expectedInvestment, 1e-9);
        result.AnnualCostSavings.Should().BeApproximately(expectedCostSavings, 1e-9);
        result.PaybackPeriod.Should().BeApproximately(expectedInvestment / expectedCostSavings, 1e-9);
        result.Roi.Should().BeApproximately(
            ((expectedCostSavings * 10 - expectedInvestment) / expectedInvestment) * 100, 1e-9);
    }

    [Fact]
    public void TierResultBuilder_SplitsOptimizedFocByTheOptimalPlantsOwnMeAeRatio()
    {
        // Foc fixture: optimal ME 760 / AE 190 ⇒ ME ratio 0.8. Baseline 1000, savings 42.
        var result = TierResultBuilder.Build(
            Tier(new SavingsBreakdown(30, 5, 7)), IntegrationTier.Premium, Pricing);

        result.OptimizedFOC.Should().BeApproximately(958, 1e-9);
        result.OptimizedME.Should().BeApproximately(958 * 0.8, 1e-9);
        result.OptimizedAE.Should().BeApproximately(958 * 0.2, 1e-9);
    }
}
