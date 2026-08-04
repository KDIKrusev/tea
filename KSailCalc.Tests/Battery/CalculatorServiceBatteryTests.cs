using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Tests.TestHelpers;
using Microsoft.Extensions.Options;

namespace KSailCalc.Tests.Battery;

/// <summary>
/// Story: Battery Increment B — pipeline wiring, dual-scenario benefit, API contract.
/// AC numbers reference docs/stories/brownfield-battery-b-pipeline-wiring.md.
/// </summary>
public class CalculatorServiceBatteryTests
{
    private const double Precision = 1e-6;

    /// <summary>Excel reference plant (story A AC1 scenario, sized so L1 has valid combinations).</summary>
    private static CalculatorInputBuilder ExcelPlant() => CalculatorInputBuilder.Default()
        .WithMainEngines(24000, 2)
        .WithShaftGenerators(1000)
        .WithPropulsionPower(11463)
        .WithSeaMargin(0)
        .WithTransitMode(5000, 3800);

    /// <summary>
    /// Same plant with larger aux engines so several ME×AE combinations are valid
    /// (needed to exercise the "third highest" baseline rule).
    /// </summary>
    private static CalculatorInputBuilder RichPlant() => ExcelPlant().WithAuxiliaryEngines(2000, 3);

    // ── AC1: zero regression ─────────────────────────────────────────────────

    [Fact]
    public async Task Calculate_NullBattery_And_ZeroPowerBattery_ProduceIdenticalResults_AndNoBatteryDetails()
    {
        var factory = TestServiceFactory.Create();

        var withoutBattery = await factory.CalculatorService.CalculateAllVariantsAsync(
            CalculatorInputBuilder.Default().Build());
        var withInertBattery = await factory.CalculatorService.CalculateAllVariantsAsync(
            CalculatorInputBuilder.Default().WithBattery(0, 0).Build());

        withoutBattery.BatteryDetails.Should().BeNull();
        withInertBattery.BatteryDetails.Should().BeNull();

        withInertBattery.BaselineFOC.Should().Be(withoutBattery.BaselineFOC);
        withInertBattery.Advanced.FuelSavings.Should().Be(withoutBattery.Advanced.FuelSavings);
        withInertBattery.Pro.FuelSavings.Should().Be(withoutBattery.Pro.FuelSavings);
        withInertBattery.Premium.FuelSavings.Should().Be(withoutBattery.Premium.FuelSavings);
        withInertBattery.Level1Details!.SelectedBaselineIndex
            .Should().Be(withoutBattery.Level1Details!.SelectedBaselineIndex);
    }

    [Fact]
    public async Task Calculate_BatteryForPortOnly_DoesNotAffectTransitOptimization()
    {
        var factory = TestServiceFactory.Create();
        var noBattery = await factory.CalculatorService.CalculateAllVariantsAsync(
            CalculatorInputBuilder.Default().Build());

        // Battery relevant for Port, but Port hours are 0 in the default builder →
        // no mode uses it; Transit numbers must be untouched.
        var portBattery = await factory.CalculatorService.CalculateAllVariantsAsync(
            CalculatorInputBuilder.Default().WithBattery(500, 1000, OperationalMode.Port).Build());

        portBattery.Level1Details!.OptimalFocTonPerHour
            .Should().Be(noBattery.Level1Details!.OptimalFocTonPerHour);
        portBattery.BaselineFOC.Should().Be(noBattery.BaselineFOC);
    }

    // ── AC2: demand adjustment ───────────────────────────────────────────────

    [Fact]
    public async Task Level1_WithBatteryAdjustment_CarriesHigherDemand()
    {
        var factory = TestServiceFactory.Create();
        var input = CalculatorInputBuilder.Default().Build();

        var without = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit);
        var with = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit, batteryAdjustment: new BatteryL1Adjustment(100, 50));

        with.OptimalCombination.MePowerKw.Should().BeGreaterThan(without.OptimalCombination.MePowerKw);
        with.OptimalFocTonPerHour.Should().BeGreaterThan(without.OptimalFocTonPerHour);
    }

    [Fact]
    public async Task Calculate_ActiveTransitBattery_RaisesBaselineDemandVsNoBattery()
    {
        var factory = TestServiceFactory.Create();

        var noBattery = await factory.CalculatorService.CalculateAllVariantsAsync(ExcelPlant().Build());
        var withBattery = await factory.CalculatorService.CalculateAllVariantsAsync(
            ExcelPlant().WithBattery(1260, 2000, OperationalMode.Transit).Build());

        // Adjusted demand (avg + uncovered reserve) ⇒ higher optimal FOC than the plain avg demand
        withBattery.Level1Details!.OptimalFocTonPerHour
            .Should().BeGreaterThan(noBattery.Level1Details!.OptimalFocTonPerHour);
    }

    // ── AC3: baseline rule (D1 "third highest") ──────────────────────────────

    [Fact]
    public async Task Level1_WithBatteryAdjustment_DefaultsBaselineToThirdHighest()
    {
        var factory = TestServiceFactory.Create();
        var input = RichPlant().Build();

        var result = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit, batteryAdjustment: new BatteryL1Adjustment(0, 0));

        result.AllValidCombinations.Count.Should().BeGreaterThanOrEqualTo(3,
            "the scenario must have enough combinations to exercise the rule");
        result.SelectedBaselineIndex.Should().Be(result.AllValidCombinations.Count - 3);
    }

    [Fact]
    public async Task Level1_WithBatteryAdjustment_ExplicitBaselineIndexStillWins()
    {
        var factory = TestServiceFactory.Create();
        var input = RichPlant().Build();

        var result = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit, baselineIndex: 0,
            batteryAdjustment: new BatteryL1Adjustment(0, 0));

        result.SelectedBaselineIndex.Should().Be(0);
    }

    [Fact]
    public async Task Level1_WithBatteryAdjustment_FewerThanThreeCombinations_ClampsBaselineToZero()
    {
        var factory = TestServiceFactory.Create();
        // ExcelPlant has exactly one valid combination (AE 800×3 can only cover hotel with all 3 on)
        var input = ExcelPlant().Build();

        var result = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit, batteryAdjustment: new BatteryL1Adjustment(0, 0));

        result.AllValidCombinations.Count.Should().BeLessThan(3);
        result.SelectedBaselineIndex.Should().Be(0);
    }

    [Fact]
    public async Task Level1_WithoutBattery_KeepsWorstCombinationAsDefaultBaseline()
    {
        var factory = TestServiceFactory.Create();
        var input = RichPlant().Build();

        var result = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit);

        result.AllValidCombinations.Count.Should().BeGreaterThanOrEqualTo(3);
        result.SelectedBaselineIndex.Should().Be(result.AllValidCombinations.Count - 1);
    }

    // ── AC4: dual-scenario benefit (R3a) ─────────────────────────────────────

    [Fact]
    public async Task Calculate_ActiveBattery_ReportsNonNegativeBenefit_WithConsistentCost()
    {
        var factory = TestServiceFactory.Create();
        var input = ExcelPlant().WithBattery(1260, 2000, OperationalMode.Transit).Build();

        var result = await factory.CalculatorService.CalculateAllVariantsAsync(input);

        result.BatteryDetails.Should().NotBeNull();
        // Reference demand (avg + ΣH) is strictly higher than battery demand (avg + ΣL),
        // so the benefit must be strictly positive in this scenario, not just non-negative.
        result.BatteryDetails!.BenefitFocTonPerYear.Should().BeGreaterThan(0);
        result.BatteryDetails.BenefitCostPerYear.Should().BeApproximately(
            result.BatteryDetails.BenefitFocTonPerYear * input.FuelPrice, Precision);
    }

    // ── ME/AE split must describe the OPTIMIZED plant, not the baseline ──────

    [Fact]
    public async Task OptimizedFuelSplit_WhenOptimumRunsNoAuxEngines_AssignsNoFuelToAux()
    {
        var factory = TestServiceFactory.Create();
        // Shaft generators alone cover the whole hotel ⇒ every valid combination runs 0 AE,
        // so the optimum definitely has none (mirrors the live Test-4 scenario).
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(24000, 2)
            .WithShaftGenerators(4000)
            .WithAuxiliaryEngines(4000, 4)
            .WithPropulsionPower(11463).WithSeaMargin(0)
            .WithTransitMode(5000, 3800)
            .WithBattery(1260, 2000, OperationalMode.Transit).Build();

        var result = await factory.CalculatorService.CalculateAllVariantsAsync(input);

        result.Level1Details!.ActiveAeCount.Should().Be(0, "the optimum runs no auxiliary engines");
        result.Advanced.AuxiliaryEngineLoadPercent.Should().Be(0);
        result.Advanced.OptimizedAE.Should().Be(0, "no AE running ⇒ no AE fuel may be reported");
        result.Advanced.OptimizedME.Should().BeApproximately(
            result.Advanced.OptimizedFOC, Precision,
            "with no AE, the whole optimized FOC belongs to the main engines");
    }

    [Fact]
    public async Task OptimizedFuelSplit_FollowsOptimalCombinationRatio_NotBaseline()
    {
        var factory = TestServiceFactory.Create();
        var input = ExcelPlant().WithAuxiliaryEngines(2000, 3)
            .WithBattery(1260, 2000, OperationalMode.Transit).Build();

        // The optimum's own ME/AE fuel split is the reference the variants must follow
        var l1 = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit, batteryAdjustment: new BatteryL1Adjustment(372.5475, 72.2));
        var optimal = l1.OptimalCombination;
        var expectedMeRatio = optimal.MeFocTonPerHour / (optimal.MeFocTonPerHour + optimal.AeFocTonPerHour);

        var result = await factory.CalculatorService.CalculateAllVariantsAsync(input);

        foreach (var variant in new[] { result.Advanced, result.Pro, result.Premium })
        {
            (variant.OptimizedME + variant.OptimizedAE).Should().BeApproximately(
                variant.OptimizedFOC, Precision, "the split must be exhaustive");
            (variant.OptimizedME / variant.OptimizedFOC).Should().BeApproximately(
                expectedMeRatio, 1e-9, "the split must mirror the optimized plant, not the baseline");
            variant.OptimizedME.Should().BeGreaterThanOrEqualTo(0);
            variant.OptimizedAE.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    // ── AC5: contract — computed Spinning Reserve / Peak Shaving ─────────────

    [Fact]
    public async Task Calculate_ExcelReferenceBattery_ReportsAllocationTotalsInBatteryDetails()
    {
        var factory = TestServiceFactory.Create();
        var input = ExcelPlant().WithBattery(1260, 2000, OperationalMode.Transit).Build();

        var result = await factory.CalculatorService.CalculateAllVariantsAsync(input);

        result.BatteryDetails.Should().NotBeNull();
        result.BatteryDetails!.PowerKw.Should().Be(1260);
        result.BatteryDetails.CapacityKwh.Should().Be(2000);
        result.BatteryDetails.SpinningReserveKw.Should().BeApproximately(444.7475, Precision);
        result.BatteryDetails.PeakShavingKw.Should().BeApproximately(204.4025, Precision);
        result.BatteryDetails.ModeAllocations.Should().ContainSingle(a => a.Mode == OperationalMode.Transit);
    }

    // ── Allocation override plumbing (QA carry-over #3 + R3a reference) ──────

    [Fact]
    public void Allocate_PropulsionOverride_ReplacesPropulsionRowAverage()
    {
        var service = new BatteryAllocationService(Options.Create(new BatterySettings()));
        var input = ExcelPlant().WithBattery(1260, 2000, OperationalMode.Transit).Build();

        var overridden = service.Allocate(OperationalMode.Transit, input, propulsionOverrideKw: 10000);

        var propulsion = overridden.Loads.Single(l => l.Load == BatteryLoadType.Propulsion);
        propulsion.AverageLoadKw.Should().Be(10000);
        propulsion.VariationKw.Should().BeApproximately(500, Precision); // 10000 × 0.05
    }

    [Fact]
    public void Allocate_ZeroBudgetOverride_ProducesFullUncoveredVariation_EvenWhenBatteryActive()
    {
        var service = new BatteryAllocationService(Options.Create(new BatterySettings()));
        var input = ExcelPlant().WithBattery(1260, 2000, OperationalMode.Transit).Build();

        var reference = service.Allocate(OperationalMode.Transit, input, budgetOverrideKw: 0);

        reference.PeakShavingBandKw.Should().Be(0);
        reference.AdditionalSpinningReserveKw.Should().BeApproximately(649.15, Precision); // ΣH
    }

    // ── AC6: validation rules ────────────────────────────────────────────────

    [Theory]
    [InlineData(-1, 100, "Battery power cannot be negative")]
    [InlineData(100, -1, "Battery capacity cannot be negative")]
    [InlineData(100, 0, "Battery capacity (kWh) is required when battery power is greater than 0")]
    public void Validate_InvalidBatteryNumbers_ProducesError(double powerKw, double capacityKwh, string expectedError)
    {
        var service = new ValidationService();
        var input = CalculatorInputBuilder.Default()
            .WithBattery(powerKw, capacityKwh, OperationalMode.Transit).Build();

        var result = service.ValidateInput(input);

        result.Valid.Should().BeFalse();
        result.Errors.Should().Contain(expectedError);
    }

    [Fact]
    public void Validate_BatteryModeOutsideSketch_ProducesError()
    {
        var service = new ValidationService();
        var input = CalculatorInputBuilder.Default()
            .WithBattery(500, 1000, OperationalMode.Anchor).Build();

        var result = service.ValidateInput(input);

        result.Valid.Should().BeFalse();
        result.Errors.Should().Contain("Battery relevant modes must be Transit, DP or Port");
    }

    [Fact]
    public void Validate_BatteryDpModeWithoutDpEnabled_ProducesError()
    {
        var service = new ValidationService();
        var input = CalculatorInputBuilder.Default().WithoutDPMode()
            .WithBattery(500, 1000, OperationalMode.DP).Build();

        var result = service.ValidateInput(input);

        result.Valid.Should().BeFalse();
        result.Errors.Should().Contain("Battery cannot apply to DP mode when DP mode is not enabled");
    }

    [Fact]
    public void Validate_BatteryWithoutRelevantModes_ProducesWarningNotError()
    {
        var service = new ValidationService();
        var input = CalculatorInputBuilder.Default().WithBattery(500, 1000).Build();

        var result = service.ValidateInput(input);

        result.Valid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Type == "battery" && w.Message.Contains("no relevant modes"));
    }

    [Fact]
    public void Validate_BatteryCapacityBelowThirtyMinuteSustain_ProducesWarningNotError()
    {
        var service = new ValidationService();
        var input = CalculatorInputBuilder.Default()
            .WithBattery(1000, 400, OperationalMode.Transit).Build(); // 400 < 1000 × 0.5

        var result = service.ValidateInput(input);

        result.Valid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Type == "battery" && w.Message.Contains("30 minutes"));
    }

    [Fact]
    public void Validate_WellConfiguredBattery_ProducesNoBatteryErrorsOrWarnings()
    {
        var service = new ValidationService();
        var input = CalculatorInputBuilder.Default()
            .WithBattery(1000, 2000, OperationalMode.Transit).Build();

        var result = service.ValidateInput(input);

        result.Valid.Should().BeTrue();
        result.Warnings.Should().NotContain(w => w.Type == "battery");
    }
}
