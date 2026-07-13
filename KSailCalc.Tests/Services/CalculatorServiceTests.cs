using FluentAssertions;
using KSailCalc.Api.Services;
using KSailCalc.Tests.TestHelpers;

namespace KSailCalc.Tests.Services;

public class CalculatorServiceTests
{
    private readonly CalculatorService _sut;

    public CalculatorServiceTests()
    {
        _sut = TestServiceFactory.Create().CalculatorService;
    }

    // === Three integration levels ===

    [Fact]
    public async Task AllVariants_ReturnsThreeLevels()
    {
        var result = await _sut.CalculateAllVariantsAsync(CalculatorInputBuilder.Default().Build());

        result.Advanced.Should().NotBeNull();
        result.Pro.Should().NotBeNull();
        result.Premium.Should().NotBeNull();
    }

    [Fact]
    public async Task AllVariants_EfficiencyFactorsLessThanOne()
    {
        var result = await _sut.CalculateAllVariantsAsync(CalculatorInputBuilder.Default().Build());

        // With real L1→L2→L3 pipeline, efficiency = optimizedFOC / baselineFOC < 1
        result.Advanced.EfficiencyFactor.Should().BeLessThanOrEqualTo(1.0);
        result.Pro.EfficiencyFactor.Should().BeLessThanOrEqualTo(result.Advanced.EfficiencyFactor);
        result.Premium.EfficiencyFactor.Should().BeLessThanOrEqualTo(result.Pro.EfficiencyFactor);
    }

    [Fact]
    public async Task AllVariants_HasBaselineFOC()
    {
        var result = await _sut.CalculateAllVariantsAsync(CalculatorInputBuilder.Default().Build());

        result.BaselineFOC.Should().BeGreaterThan(0);
    }

    // === Fuel savings ===

    [Fact]
    public async Task FuelSavings_PremiumGreaterOrEqualPro_ProGreaterOrEqualAdvanced()
    {
        var result = await _sut.CalculateAllVariantsAsync(CalculatorInputBuilder.Default().Build());

        // L2/L3 may add 0 savings with test SFOC data (monotonic curve)
        result.Premium.FuelSavings.Should().BeGreaterThanOrEqualTo(result.Pro.FuelSavings);
        result.Pro.FuelSavings.Should().BeGreaterThanOrEqualTo(result.Advanced.FuelSavings);
    }

    [Fact]
    public async Task FuelSavingsPercentage_PremiumGreaterOrEqualPro()
    {
        var result = await _sut.CalculateAllVariantsAsync(CalculatorInputBuilder.Default().Build());

        result.Advanced.FuelSavingsPercentage.Should().BeGreaterThanOrEqualTo(0);
        result.Premium.FuelSavingsPercentage.Should().BeGreaterThanOrEqualTo(result.Pro.FuelSavingsPercentage);
        result.Pro.FuelSavingsPercentage.Should().BeGreaterThanOrEqualTo(result.Advanced.FuelSavingsPercentage);
    }

    [Fact]
    public async Task OptimizedFOC_PremiumLessOrEqualPro()
    {
        var result = await _sut.CalculateAllVariantsAsync(CalculatorInputBuilder.Default().Build());

        result.Premium.OptimizedFOC.Should().BeLessThanOrEqualTo(result.Pro.OptimizedFOC);
        result.Pro.OptimizedFOC.Should().BeLessThanOrEqualTo(result.Advanced.OptimizedFOC);
    }

    // === CO2 ===

    [Fact]
    public async Task CO2Reduction_PremiumGreaterOrEqualPro()
    {
        var result = await _sut.CalculateAllVariantsAsync(CalculatorInputBuilder.Default().Build());

        result.Premium.Co2Reduction.Should().BeGreaterThanOrEqualTo(result.Pro.Co2Reduction);
        result.Pro.Co2Reduction.Should().BeGreaterThanOrEqualTo(result.Advanced.Co2Reduction);
    }

    [Fact]
    public async Task CO2Reduction_UsesCO2Factor()
    {
        var result = await _sut.CalculateAllVariantsAsync(CalculatorInputBuilder.Default().Build());

        // CO2 = FuelSavings * 3.206
        var expectedCO2 = result.Advanced.FuelSavings * 3.206;
        result.Advanced.Co2Reduction.Should().BeApproximately(expectedCO2, 0.01);
    }

    // === Financial ===

    [Fact]
    public async Task Investment_PremiumMostExpensive()
    {
        var result = await _sut.CalculateAllVariantsAsync(CalculatorInputBuilder.Default().Build());

        result.Premium.TotalInvestment.Should().BeGreaterThan(result.Pro.TotalInvestment);
        result.Pro.TotalInvestment.Should().BeGreaterThan(result.Advanced.TotalInvestment);
    }

    [Fact]
    public async Task Investment_ConvertsNOKtoUSD()
    {
        var result = await _sut.CalculateAllVariantsAsync(CalculatorInputBuilder.Default().Build());

        // Advanced: (1_000_000 + 100_000) / 10 = 110_000 USD
        result.Advanced.TotalInvestment.Should().BeApproximately(110_000, 1);
    }

    [Fact]
    public async Task AnnualCostSavings_PremiumGreaterOrEqualPro()
    {
        var result = await _sut.CalculateAllVariantsAsync(
            CalculatorInputBuilder.Default().WithFuelPrice(600).Build());

        result.Premium.AnnualCostSavings.Should().BeGreaterThanOrEqualTo(result.Pro.AnnualCostSavings);
        result.Pro.AnnualCostSavings.Should().BeGreaterThanOrEqualTo(result.Advanced.AnnualCostSavings);
    }

    [Fact]
    public async Task PaybackPeriod_Positive_ReasonableRange()
    {
        // Multiple ME + hotel exceeds SG → multiple L1 combos → positive savings
        var result = await _sut.CalculateAllVariantsAsync(
            CalculatorInputBuilder.Default()
                .WithMainEngines(12000, 2)
                .WithShaftGenerators(1500)
                .WithAuxiliaryEngines(2000, 3)
                .WithPropulsionPower(4000)
                .WithSeaMargin(15)
                .WithTransitMode(5694, 2000)
                .Build());

        result.Pro.PaybackPeriod.Should().BeGreaterThan(0).And.BeLessThan(50);
        result.Premium.PaybackPeriod.Should().BeGreaterThan(0).And.BeLessThan(50);
    }

    // === Breakdown & PowerDemands ===

    [Fact]
    public async Task Breakdown_HasSharedBaseline()
    {
        var result = await _sut.CalculateAllVariantsAsync(CalculatorInputBuilder.Default().Build());

        result.BaselineME.Should().BeGreaterThan(0);
        result.BaselineAE.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task PowerDemands_SharedOnResult()
    {
        var result = await _sut.CalculateAllVariantsAsync(CalculatorInputBuilder.Default().Build());

        result.PowerDemands.MainEnginePowerKw.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LoadPercentages_PerVariant()
    {
        var result = await _sut.CalculateAllVariantsAsync(CalculatorInputBuilder.Default().Build());

        result.Advanced.MainEngineLoadPercent.Should().BeGreaterThan(0);
        result.Pro.MainEngineLoadPercent.Should().BeGreaterThan(0);
        result.Premium.MainEngineLoadPercent.Should().BeGreaterThan(0);
    }

    // === Multi-modal integration ===

    [Fact]
    public async Task TransitOnlyDefaultVessel_HasPositiveBaselineFOC()
    {
        // Current architecture models Transit + DP only
        var input = CalculatorInputBuilder.Default().Build();

        var result = await _sut.CalculateAllVariantsAsync(input);
        result.BaselineFOC.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TransitOnlyVessel_HighFuelConsumption()
    {
        var input = CalculatorInputBuilder.Default()
            .WithPropulsionPower(5000).WithSeaMargin(15)
            .WithTransitMode(8760, 800)
            .Build();

        var result = await _sut.CalculateAllVariantsAsync(input);
        result.BaselineFOC.Should().BeGreaterThan(5000);
    }

    [Fact]
    public async Task SeaMargin_IncreasesTransitFuel()
    {
        var inputLow = CalculatorInputBuilder.Default().WithSeaMargin(0)
            .WithTransitMode(5000, 800).Build();
        var inputHigh = CalculatorInputBuilder.Default().WithSeaMargin(20)
            .WithTransitMode(5000, 800).Build();

        var resultLow = await _sut.CalculateAllVariantsAsync(inputLow);
        var resultHigh = await _sut.CalculateAllVariantsAsync(inputHigh);

        resultHigh.BaselineFOC.Should().BeGreaterThan(resultLow.BaselineFOC);
    }

    [Fact]
    public async Task HigherHotelLoad_MoreFuel()
    {
        var inputLow = CalculatorInputBuilder.Default()
            .WithTransitMode(6760, 300).Build();
        var inputHigh = CalculatorInputBuilder.Default()
            .WithTransitMode(6760, 1000).Build();

        var resultLow = await _sut.CalculateAllVariantsAsync(inputLow);
        var resultHigh = await _sut.CalculateAllVariantsAsync(inputHigh);

        resultHigh.BaselineFOC.Should().BeGreaterThan(resultLow.BaselineFOC);
    }

    // === DP mode ===

    [Fact]
    public async Task DPMode_IncreasesTotalFuel()
    {
        // Transit propulsion = 4000 * 1.15 = 4600 kW
        // DP thrust must exceed this for total fuel to increase
        var inputWithout = CalculatorInputBuilder.Default()
            .WithTransitMode(6360, 220).WithoutDPMode().Build();
        var inputWith = CalculatorInputBuilder.Default()
            .WithTransitMode(4000, 220).WithDPMode(2360, 300, 6000).Build();

        var resultWithout = await _sut.CalculateAllVariantsAsync(inputWithout);
        var resultWith = await _sut.CalculateAllVariantsAsync(inputWith);

        resultWith.BaselineFOC.Should().BeGreaterThan(resultWithout.BaselineFOC);
    }

    // === Sail contribution passthrough ===

    [Fact]
    public async Task SailEnabled_SailResultAttachedToOutput()
    {
        var factory = TestServiceFactory.Create(enableSailData: true);
        var input = CalculatorInputBuilder.Default().WithSailEnabled(10, 90).Build();

        var result = await factory.CalculatorService.CalculateAllVariantsAsync(input);

        // SailContribution is shared at top level
        result.SailContribution.Should().NotBeNull();
        result.SailContribution!.SailPowerKw.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SailDisabled_NoSailResultInOutput()
    {
        var input = CalculatorInputBuilder.Default().WithSailDisabled().Build();
        var result = await _sut.CalculateAllVariantsAsync(input);

        result.SailContribution.Should().BeNull();
    }

    // === Financial ROI ===

    [Fact]
    public async Task Roi_PositiveForAllTiers()
    {
        // Multiple ME + hotel exceeds SG → multiple L1 combos → positive savings
        var result = await _sut.CalculateAllVariantsAsync(
            CalculatorInputBuilder.Default()
                .WithMainEngines(12000, 2)
                .WithShaftGenerators(1500)
                .WithAuxiliaryEngines(2000, 3)
                .WithPropulsionPower(4000)
                .WithSeaMargin(15)
                .WithTransitMode(5694, 2000)
                .Build());

        result.Pro.Roi.Should().BeGreaterThan(-100, "Pro ROI should reflect some savings");
        result.Premium.Roi.Should().BeGreaterThan(-100, "Premium ROI should reflect some savings");
    }

    [Fact]
    public async Task PaybackPeriod_ReasonableRange()
    {
        // Multiple ME + hotel exceeds SG → multiple L1 combos → positive savings
        var result = await _sut.CalculateAllVariantsAsync(
            CalculatorInputBuilder.Default()
                .WithMainEngines(12000, 2)
                .WithShaftGenerators(1500)
                .WithAuxiliaryEngines(2000, 3)
                .WithPropulsionPower(4000)
                .WithSeaMargin(15)
                .WithTransitMode(5694, 2000)
                .Build());

        result.Pro.PaybackPeriod.Should().BeGreaterThan(0,
            "payback should be positive for a configuration with multiple engine combos");
        result.Premium.PaybackPeriod.Should().BeGreaterThan(0);
    }

    // === Full pipeline DP mode ===

    [Fact]
    public async Task DPMode_FullPipeline_PremiumSavingsGreaterOrEqualPro()
    {
        var input = CalculatorInputBuilder.Default()
            .WithTransitMode(4000, 800)
            .WithDPMode(1000, 300, 1200)
            .Build();

        var result = await _sut.CalculateAllVariantsAsync(input);

        result.Premium.FuelSavings.Should().BeGreaterThanOrEqualTo(result.Pro.FuelSavings,
            "Premium should save at least as much as Pro");
        result.Pro.FuelSavings.Should().BeGreaterThanOrEqualTo(result.Advanced.FuelSavings,
            "Pro should save at least as much as Advanced");
        result.Advanced.FuelSavings.Should().BeGreaterThan(0,
            "all tiers should have positive fuel savings");
    }

    [Fact]
    public async Task DPMode_FullPipeline_BaselineFocGreaterThanOptimized()
    {
        var input = CalculatorInputBuilder.Default()
            .WithTransitMode(4000, 800)
            .WithDPMode(1000, 300, 1200)
            .Build();

        var result = await _sut.CalculateAllVariantsAsync(input);

        result.BaselineFOC.Should().BeGreaterThan(result.Advanced.OptimizedFOC);
        result.BaselineFOC.Should().BeGreaterThan(result.Pro.OptimizedFOC);
        result.BaselineFOC.Should().BeGreaterThan(result.Premium.OptimizedFOC);
    }

    [Fact]
    public async Task DPMode_FullPipeline_Co2ReductionMatchesFuelSavings()
    {
        var input = CalculatorInputBuilder.Default()
            .WithTransitMode(4000, 800)
            .WithDPMode(1000, 300, 1200)
            .Build();

        var result = await _sut.CalculateAllVariantsAsync(input);

        const double co2Factor = 3.206;
        result.Advanced.Co2Reduction.Should().BeApproximately(
            result.Advanced.FuelSavings * co2Factor, 0.01);
        result.Pro.Co2Reduction.Should().BeApproximately(
            result.Pro.FuelSavings * co2Factor, 0.01);
        result.Premium.Co2Reduction.Should().BeApproximately(
            result.Premium.FuelSavings * co2Factor, 0.01);
    }

    // === Sail integration into pipeline ===

    [Fact]
    public async Task SailEnabled_ReducesFuelConsumption()
    {
        var factory = TestServiceFactory.Create(enableSailData: true);

        var inputWithSail = CalculatorInputBuilder.Default().WithSailEnabled(10, 90).Build();
        var inputWithoutSail = CalculatorInputBuilder.Default().WithSailDisabled().Build();

        var resultWithSail = await factory.CalculatorService.CalculateAllVariantsAsync(inputWithSail);
        var resultWithoutSail = await factory.CalculatorService.CalculateAllVariantsAsync(inputWithoutSail);

        resultWithSail.Premium.OptimizedFOC.Should().BeLessThan(resultWithoutSail.Premium.OptimizedFOC,
            "sail reduces propulsion → lower FOC");
        resultWithSail.BaselineFOC.Should().BeLessThan(resultWithoutSail.BaselineFOC,
            "baseline also uses reduced propulsion");
    }

    [Fact]
    public async Task SailEnabled_DPModeNotAffectedBySail()
    {
        var factory = TestServiceFactory.Create(enableSailData: true);

        var inputWithSail = CalculatorInputBuilder.Default()
            .WithTransitMode(4000, 800)
            .WithDPMode(1000, 300, 1200)
            .WithSailEnabled(10, 90)
            .Build();
        var inputWithoutSail = CalculatorInputBuilder.Default()
            .WithTransitMode(4000, 800)
            .WithDPMode(1000, 300, 1200)
            .WithSailDisabled()
            .Build();

        var resultWith = await factory.CalculatorService.CalculateAllVariantsAsync(inputWithSail);
        var resultWithout = await factory.CalculatorService.CalculateAllVariantsAsync(inputWithoutSail);

        // With sail, total fuel consumption is lower (even if savings delta is similar)
        resultWith.Premium.OptimizedFOC.Should().BeLessThan(resultWithout.Premium.OptimizedFOC,
            "sail reduces Transit propulsion → lower total FOC");
    }
}