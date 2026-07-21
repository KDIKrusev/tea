using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services;
using KSailCalc.Tests.TestHelpers;

namespace KSailCalc.Tests.Services;

public class Level1OptimizationServiceTests
{
    private readonly Level1OptimizationService _service;

    public Level1OptimizationServiceTests()
    {
        var factory = TestServiceFactory.Create();
        _service = new Level1OptimizationService(factory.SfocService, Microsoft.Extensions.Options.Options.Create(new KSailCalc.Api.Models.BatterySettings()));
    }

    #region Combination Count & Filtering

    [Fact]
    public async Task Transit_BulkCarrierSetup_Returns2ValidCombinations_WhenSgCoversHotel()
    {
        // Arrange: 2 ME(12000), SG(1500), 3 AE(500) → 24 total generated
        // hotel=165 kW, sgCapacity=1500 per ME → SG covers full hotel
        // SG=OFF combos filtered (SG=ON variant dominates for both ME=1 and ME=2)
        // Valid combos:
        //   ME=1,SG=ON,AE=0  →  ME can handle 2274+165=2439 (< 12000)
        //   ME=2,SG=ON,AE=0  →  ME can handle 2274+165=2439 (< 24000)
        //   Total: 2
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 2)
            .WithShaftGenerators(1500)
            .WithAuxiliaryEngines(500, 3)
            .WithPropulsionPower(1895)
            .WithSeaMargin(20)
            .WithTransitMode(5717, 165)
            .Build();

        // Act
        var result = await _service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        // Assert
        result.AllValidCombinations.Should().HaveCount(2);
        result.AllValidCombinations.Should().OnlyContain(c => c.SgEnabled);
    }

    [Fact]
    public async Task Transit_SingleMeNoSg_FiltersSgOnCombinations()
    {
        // Arrange: 1 ME, SG=0, 2 AE(800) → SG ON combos filtered out
        // hotel=400 → AE=0 can't cover hotel, AE=1(800) OK, AE=2(1600) OK
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(5000, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(800, 2)
            .WithPropulsionPower(2000)
            .WithSeaMargin(15)
            .WithTransitMode(5000, 400)
            .Build();

        var result = await _service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        // ME=1 only. SG filtered (capacity=0). AE=0 rejected (hotel uncovered). AE=1,2 → 2 combos
        result.AllValidCombinations.Should().HaveCount(2);
        result.AllValidCombinations.Should().OnlyContain(c => !c.SgEnabled);
        result.AllValidCombinations.Should().OnlyContain(c => c.ActiveAeCount >= 1);
    }

    [Fact]
    public async Task Transit_AllMeZeroCombinations_AreExcluded()
    {
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(5000, 2)
            .WithShaftGenerators(500)
            .WithAuxiliaryEngines(800, 2)
            .WithPropulsionPower(2000)
            .WithSeaMargin(10)
            .WithTransitMode(6000, 300)
            .Build();

        var result = await _service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        result.AllValidCombinations.Should().OnlyContain(c => c.ActiveMeCount > 0);
    }

    #endregion

    #region Load Distribution

    [Fact]
    public async Task Transit_1ME_SgOn_0AE_LoadDistributionCorrect()
    {
        // 1 ME × 12,000, SG 1,500, 3 AE × 500
        // Propulsion = 1895 × 1.20 = 2274. Hotel = 165.
        // SG covers 165 hotel. ME = 2274 + 165 = 2439. AE = 0.
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 2)
            .WithShaftGenerators(1500)
            .WithAuxiliaryEngines(500, 3)
            .WithPropulsionPower(1895)
            .WithSeaMargin(20)
            .WithTransitMode(5717, 165)
            .Build();

        var result = await _service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        var combo = result.AllValidCombinations.Single(
            c => c.ActiveMeCount == 1 && c.SgEnabled && c.ActiveAeCount == 0);

        combo.MePowerKw.Should().BeApproximately(2439, 1);
        combo.SgPowerKw.Should().BeApproximately(165, 1);
        combo.AePowerKw.Should().BeApproximately(0, 0.01);
        combo.MeLoadPercent.Should().BeApproximately(2439.0 / 12000, 0.001);
    }

    [Fact]
    public async Task Transit_1ME_SgOff_1AE_LoadDistributionCorrect()
    {
        // Hotel=2000 > SG capacity=1500 → SG=OFF+AE combos are valid (SG can't cover full hotel)
        // ME=1, SG=OFF, AE=1: ME = propulsion only = 2274. AE = hotel = 2000.
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 2)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(2500, 3)
            .WithPropulsionPower(1895)
            .WithSeaMargin(20)
            .WithTransitMode(5717, 2000)
            .Build();

        var result = await _service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        var combo = result.AllValidCombinations.Single(
            c => c.ActiveMeCount == 1 && !c.SgEnabled && c.ActiveAeCount == 1);

        combo.MePowerKw.Should().BeApproximately(2274, 1);
        combo.AePowerKw.Should().BeApproximately(2000, 1);
        combo.MeLoadPercent.Should().BeApproximately(2274.0 / 12000, 0.001);
        combo.AeLoadPercent.Should().BeApproximately(2000.0 / 2500, 0.001);
    }

    [Fact]
    public async Task Transit_2ME_SgOff_0AE_RejectedWhenHotelNotCovered()
    {
        // ME=2, SG=OFF, AE=0 → no one covers hotel=165 kW → invalid
        // ME cannot supply electricity for hotel without SG or PTO
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 2)
            .WithShaftGenerators(1500)
            .WithAuxiliaryEngines(500, 3)
            .WithPropulsionPower(1895)
            .WithSeaMargin(20)
            .WithTransitMode(5717, 165)
            .Build();

        var result = await _service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        // No combo with SG=OFF and AE=0 should exist (hotel=165 can't be covered)
        result.AllValidCombinations.Should()
            .NotContain(c => !c.SgEnabled && c.ActiveAeCount == 0);
    }

    #endregion

    #region Optimal & Baseline Selection

    [Fact]
    public async Task Transit_OptimalHasLowestFoc()
    {
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 2)
            .WithShaftGenerators(1500)
            .WithAuxiliaryEngines(500, 3)
            .WithPropulsionPower(1895)
            .WithSeaMargin(20)
            .WithTransitMode(5717, 165)
            .Build();

        var result = await _service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        result.OptimalFocTonPerHour.Should().Be(result.AllValidCombinations.Min(c => c.FocTonPerHour));
    }


    [Fact]
    public async Task Transit_SavingsArePositive()
    {
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 2)
            .WithShaftGenerators(1500)
            .WithAuxiliaryEngines(500, 3)
            .WithPropulsionPower(1895)
            .WithSeaMargin(20)
            .WithTransitMode(5717, 165)
            .Build();

        var result = await _service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        var savings = (result.BaselineFocTonPerHour - result.OptimalFocTonPerHour) * 5717;
        savings.Should().BeGreaterThan(0);
    }

    #endregion

    #region DP Mode

    [Fact]
    public async Task DP_ValidCombinationsExcludeSgOnWithoutMe()
    {
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 2)
            .WithShaftGenerators(1500)
            .WithAuxiliaryEngines(500, 3)
            .WithPropulsionPower(1895)
            .WithSeaMargin(20)
            .WithTransitMode(5717, 165)
            .WithDPMode(1200, 300, 1200)
            .Build();

        var result = await _service.FindOptimalCombinationAsync(input, OperationalMode.DP);

        // SG ON + ME=0 should never appear
        result.AllValidCombinations.Should()
            .NotContain(c => c.SgEnabled && c.ActiveMeCount == 0);
    }

    [Fact]
    public async Task DP_MeZeroWithInsufficientAe_FilteredByPowerCheck()
    {
        // DP: propulsion 1200 kW. ME=0 → mePower=1200 but meCapacity=0 → filtered
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(5000, 1)
            .WithShaftGenerators(500)
            .WithAuxiliaryEngines(800, 2)
            .WithPropulsionPower(2000)
            .WithSeaMargin(15)
            .WithTransitMode(5000, 400)
            .WithDPMode(1200, 300, 1200)
            .Build();

        var result = await _service.FindOptimalCombinationAsync(input, OperationalMode.DP);

        // ME=0 combos should be filtered (propulsion > 0 can't go on ME=0)
        result.AllValidCombinations.Should().OnlyContain(c => c.ActiveMeCount > 0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Transit_HigherLoadEngines_OptimalPicksEfficientLoad()
    {
        // Use smaller engines so loads fall in the interpolation range of the SFOC curve
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(2000, 2)
            .WithShaftGenerators(500)
            .WithAuxiliaryEngines(500, 2)
            .WithPropulsionPower(1000)
            .WithSeaMargin(20)
            .WithTransitMode(6000, 400)
            .Build();

        var result = await _service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        // Optimal should have lower FOC than baseline
        result.OptimalFocTonPerHour.Should().BeLessThan(result.BaselineFocTonPerHour);
    }

    [Fact]
    public async Task Transit_PowerInsufficientCombinations_AreFiltered()
    {
        // Small engines, high demand → fewer valid combinations
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(1500, 2)
            .WithShaftGenerators(200)
            .WithAuxiliaryEngines(300, 2)
            .WithPropulsionPower(1200)
            .WithSeaMargin(20)
            .WithTransitMode(6000, 500)
            .Build();

        var result = await _service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        // Some combinations where 1 ME (1500 kW) can't cover propulsion (1440) + hotel overflow
        // should be filtered. All valid combos should have mePower <= meCapacity.
        result.AllValidCombinations.Should().OnlyContain(c =>
            c.MePowerKw <= c.ActiveMeCount * 1500.0 + 0.01);
    }

    [Fact]
    public async Task Transit_FocCalculation_MatchesExpectedFormula()
    {
        // Single ME + single AE, predictable loads
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(4000, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(1000, 1)
            .WithPropulsionPower(2000)
            .WithSeaMargin(0)
            .WithTransitMode(5000, 500)
            .Build();

        var result = await _service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        // ME load = 2000/4000 = 0.50 → SFOC = 214.097 (from real Excel data)
        // AE load = 500/1000 = 0.50 → SFOC = 228.148 (from real Excel data)
        // FOC = (2000*214.097 + 500*228.148) / 1_000_000 = 0.542268
        var combo = result.AllValidCombinations.Single(
            c => c.ActiveMeCount == 1 && !c.SgEnabled && c.ActiveAeCount == 1);

        combo.FocTonPerHour.Should().BeApproximately(0.542268, 0.001);
    }

    #endregion
}