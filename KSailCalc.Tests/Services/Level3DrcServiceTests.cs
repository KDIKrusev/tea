using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services;
using KSailCalc.Api.Services.Interfaces;
using KSailCalc.Tests.TestHelpers;
using Microsoft.Extensions.Options;
using Moq;

namespace KSailCalc.Tests.Services;

public class Level3DrcServiceTests
{
    private readonly Level3DrcService _service;

    public Level3DrcServiceTests()
    {
        // Use a realistic U-shaped SFOC curve for both Main and Auxiliary engines
        var sfocMock = new Mock<ISfocService>();
        sfocMock.Setup(s => s.GetSfocForLoadAsync(It.IsAny<decimal>(), It.IsAny<EngineCategory>(), It.IsAny<int>()))
            .Returns<decimal, EngineCategory, int>((load, _, __) => Task.FromResult(RealisticAeSfoc(load)));

        _service = new Level3DrcService(sfocMock.Object, Options.Create(new CalculatorSettings()));
    }

    /// <summary>
    /// Simulates a realistic U-shaped SFOC curve for AE (from the Level 3 guide).
    /// </summary>
    private static double RealisticAeSfoc(decimal loadPct)
    {
        double l = (double)loadPct;
        // Piecewise linear matching the Level 3 guide values:
        // 0.10 → 228, 0.25 → 220, 0.50 → 198, 0.75 → 193, 0.85 → 193, 1.00 → 195
        (double load, double sfoc)[] points =
        [
            (0.00, 240), (0.10, 228), (0.25, 220), (0.375, 209),
            (0.40, 207), (0.50, 198), (0.60, 196), (0.625, 196),
            (0.75, 193), (0.85, 193), (1.00, 195)
        ];

        if (l <= points[0].load) return points[0].sfoc;
        if (l >= points[^1].load) return points[^1].sfoc;

        for (int i = 0; i < points.Length - 1; i++)
        {
            if (l >= points[i].load && l <= points[i + 1].load)
            {
                var t = (l - points[i].load) / (points[i + 1].load - points[i].load);
                return points[i].sfoc + t * (points[i + 1].sfoc - points[i].sfoc);
            }
        }
        return 200;
    }

    #region Helpers

    private static Level2Result BuildLevel2WithAes(int aeCount, double aeCapacity, double aeLoadPercent)
    {
        var setpoints = new List<GeneratorSetpoint>();
        for (int i = 0; i < aeCount; i++)
        {
            setpoints.Add(new GeneratorSetpoint
            {
                GeneratorType = GeneratorType.AE,
                CapacityKw = aeCapacity,
                LoadPercent = aeLoadPercent,
                PowerKw = aeCapacity * aeLoadPercent,
                Sfoc = 220
            });
        }

        return new Level2Result
        {
            OptimalSetpoints = setpoints,
            OptimalTotalSfoc = setpoints.Sum(s => s.Sfoc),
            Level2FocTonPerHour = 0.5,
            Level1FocTonPerHour = 0.55,
            Level2SavingsTonPerHour = 0.05
        };
    }

    private static CalculatorInput BuildBulkCarrierInput()
    {
        return CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(1000, 2)
            .WithPropulsionPower(2000)
            .WithSeaMargin(20)
            .WithTransitMode(5717, 3500)
            .WithVesselTypeName("Bulk Carrier")
            .Build();
    }

    #endregion

    #region Bulk Carrier Example (from Level 3 Guide)

    //[Fact]
    //public async Task BulkCarrier_VariationIs250Kw_SplitAcross2AEs()
    //{
    //    var level2 = BuildLevel2WithAes(2, 1000, 0.50); // 2 AE × 1000kW at 50%
    //    var input = BuildBulkCarrierInput();

    //    var result = await _service.CalculateDrcSavingsAsync(level2, input, input.TransitHours);

    //    result.VariationPerGeneratorKw.Should().Be(125, "250 kW split across 2 AEs");
    //    result.ReducedVariationPerGeneratorKw.Should().Be(100, "20% reduction of 125");
    //}

    //[Fact]
    //public async Task BulkCarrier_DrcSavingsArePositive()
    //{
    //    var level2 = BuildLevel2WithAes(2, 1000, 0.50);
    //    var input = BuildBulkCarrierInput();

    //    var result = await _service.CalculateDrcSavingsAsync(level2, input, input.TransitHours);

    //    result.DrcSavingsTonPerYear.Should().BeGreaterThan(0);
    //}

    //[Fact]
    //public async Task BulkCarrier_FocWithDrcLessThanWithout()
    //{
    //    var level2 = BuildLevel2WithAes(2, 1000, 0.50);
    //    var input = BuildBulkCarrierInput();

    //    var result = await _service.CalculateDrcSavingsAsync(level2, input, input.TransitHours);

    //    result.WithDrcFocGramsPerCycle.Should().BeLessThan(result.WithoutDrcFocGramsPerCycle);
    //}

    [Fact]
    public async Task BulkCarrier_ActiveAeCountIsCorrect()
    {
        var level2 = BuildLevel2WithAes(2, 1000, 0.50);
        var input = BuildBulkCarrierInput();

        var result = await _service.CalculateDrcSavingsAsync(level2, input, input.TransitHours);

        result.ActiveGeneratorCount.Should().Be(2);
    }

    #endregion

    #region Container (High Variation)

    //[Fact]
    //public async Task Container_VariationIs1500Kw()
    //{
    //    var level2 = BuildLevel2WithAes(3, 2000, 0.60);
    //    var input = CalculatorInputBuilder.Default()
    //        .WithMainEngines(12000, 1)
    //        .WithShaftGenerators(0)
    //        .WithAuxiliaryEngines(2000, 3)
    //        .WithPropulsionPower(5000)
    //        .WithSeaMargin(15)
    //        .WithTransitMode(5000, 3600)
    //        .WithVesselTypeName("Container")
    //        .Build();

    //    var result = await _service.CalculateDrcSavingsAsync(level2, input, input.TransitHours);

    //    result.VariationPerGeneratorKw.Should().Be(500, "1500 kW split across 3 generators");
    //    result.ReducedVariationPerGeneratorKw.Should().Be(400, "20% reduction of 500");
    //    result.DrcSavingsTonPerYear.Should().BeGreaterThan(0);
    //}

    #endregion

    #region SG-Only Scenario

    [Fact]
    public async Task SgOnly_DrcStillApplies_SavingsPositive()
    {
        // Level 2 with only SG, no AE — DRC applies to SG too
        var level2 = new Level2Result
        {
            OptimalSetpoints = new List<GeneratorSetpoint>
            {
                new() { GeneratorType = GeneratorType.SG, CapacityKw = 2000, LoadPercent = 0.75, PowerKw = 1500, Sfoc = 210 }
            },
            OptimalTotalSfoc = 210,
            Level2FocTonPerHour = 0.5,
            Level1FocTonPerHour = 0.55,
            Level2SavingsTonPerHour = 0.05
        };

        var input = BuildBulkCarrierInput();

        var result = await _service.CalculateDrcSavingsAsync(level2, input, input.TransitHours);

        result.DrcSavingsTonPerYear.Should().BeGreaterThan(0, "DRC applies to SG generators too");
        result.ActiveGeneratorCount.Should().Be(1, "1 SG generator is active");
    }

    [Fact]
    public async Task NoGenerators_SavingsAreZero()
    {
        // No generators at all
        var level2 = new Level2Result
        {
            OptimalSetpoints = new List<GeneratorSetpoint>(),
            OptimalTotalSfoc = 0,
            Level2FocTonPerHour = 0.5,
            Level1FocTonPerHour = 0.55,
            Level2SavingsTonPerHour = 0.05
        };

        var input = BuildBulkCarrierInput();

        var result = await _service.CalculateDrcSavingsAsync(level2, input, input.TransitHours);

        result.DrcSavingsTonPerYear.Should().Be(0);
        result.ActiveGeneratorCount.Should().Be(0);
    }

    #endregion

    #region Edge Cases — Clamping

    [Fact]
    public async Task NegativeLoadClamped_StillPositiveSavings()
    {
        // AE at very low load (10%), variation could push below 0
        // 1 AE × 1000kW at 10% = 100kW. Variation ±500 (default).
        // Down: 100 - 500 = -400 → clamped to 0
        var level2 = BuildLevel2WithAes(1, 1000, 0.10);
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(1000, 1)
            .WithPropulsionPower(2000)
            .WithSeaMargin(0)
            .WithTransitMode(5000, 100)
            .WithVesselTypeName("Unknown Vessel")
            .Build();

        var result = await _service.CalculateDrcSavingsAsync(level2, input, input.TransitHours);

        // Should not crash and savings should be ≥ 0
        result.DrcSavingsTonPerYear.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task OverCapacityClamped_StillPositiveSavings()
    {
        // AE at 90% of capacity, variation pushes above 100%
        // 1 AE × 500kW at 80% = 400kW. Container variation: ±1500 → 1500/1 = 1500.
        // Up: 400 + 1500 = 1900 → clamped to 500
        var level2 = BuildLevel2WithAes(1, 500, 0.80);
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(500, 1)
            .WithPropulsionPower(2000)
            .WithSeaMargin(0)
            .WithTransitMode(5000, 400)
            .WithVesselTypeName("Container")
            .Build();

        var result = await _service.CalculateDrcSavingsAsync(level2, input, input.TransitHours);

        result.DrcSavingsTonPerYear.Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region Default Vessel Type

    //[Fact]
    //public async Task UnknownVesselType_UsesDefaultVariation500()
    //{
    //    var level2 = BuildLevel2WithAes(2, 1000, 0.50);
    //    var input = CalculatorInputBuilder.Default()
    //        .WithMainEngines(12000, 1)
    //        .WithShaftGenerators(0)
    //        .WithAuxiliaryEngines(1000, 2)
    //        .WithPropulsionPower(2000)
    //        .WithSeaMargin(0)
    //        .WithTransitMode(5000, 1000)
    //        .WithVesselTypeName("Some Custom Vessel")
    //        .Build();

    //    var result = await _service.CalculateDrcSavingsAsync(level2, input, input.TransitHours);

    //    result.VariationPerGeneratorKw.Should().Be(250, "500 default / 2 AEs = 250");
    //}

    [Fact]
    public async Task NullVesselType_UsesDefaultVariation500()
    {
        var level2 = BuildLevel2WithAes(1, 1000, 0.50);
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(1000, 1)
            .WithPropulsionPower(2000)
            .WithSeaMargin(0)
            .WithTransitMode(5000, 500)
            .Build();

        var result = await _service.CalculateDrcSavingsAsync(level2, input, input.TransitHours);

        result.VariationPerGeneratorKw.Should().Be(500, "null vessel → 500 default / 1 AE = 500");
    }

    #endregion

    #region Integration: Full Chain L1 → L2 → L3

    [Fact]
    public async Task FullChain_Level1ToLevel3_ProducesPositiveSavings()
    {
        // Use the realistic U-shaped SFOC mock for ALL three levels
        var sfocMock = new Mock<ISfocService>();
        sfocMock.Setup(s => s.GetSfocForLoadAsync(It.IsAny<decimal>(), It.IsAny<EngineCategory>(), It.IsAny<int>()))
            .Returns<decimal, EngineCategory, int>((load, _, __) => Task.FromResult(RealisticAeSfoc(load)));

        // L2 also needs raw SFOC data for its synchronous sweep
        var sfocDataPoints = new List<SfocDataPoint>
        {
            new() { Load = 0.00, Sfoc = 240 }, new() { Load = 0.10, Sfoc = 228 },
            new() { Load = 0.25, Sfoc = 220 }, new() { Load = 0.50, Sfoc = 198 },
            new() { Load = 0.75, Sfoc = 193 }, new() { Load = 0.85, Sfoc = 193 },
            new() { Load = 1.00, Sfoc = 195 }
        };
        sfocMock.Setup(s => s.GetSfocDataAsync(It.IsAny<EngineCategory>(), It.IsAny<int>()))
            .ReturnsAsync(sfocDataPoints);

        var l1 = new Level1OptimizationService(sfocMock.Object);
        var l2 = new Level2OptimizationService(sfocMock.Object);
        var l3 = new Level3DrcService(sfocMock.Object, Options.Create(new CalculatorSettings()));

        // ME=3200 can't absorb hotel overflow → forces 2 AEs active
        // hotel=1500 > 1 AE capacity=1000 → needs exactly 2 AEs
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(3200, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(1000, 2)
            .WithPropulsionPower(3000)
            .WithSeaMargin(0)
            .WithTransitMode(5717, 1500)
            .WithVesselTypeName("Bulk Carrier")
            .Build();

        var level1 = await l1.FindOptimalCombinationAsync(input, OperationalMode.Transit);
        var level2 = await l2.OptimizeLoadSetpointsAsync(level1, input);
        var level3 = await l3.CalculateDrcSavingsAsync(level2, input, input.TransitHours);

        level3.ActiveGeneratorCount.Should().Be(2);
        level3.DrcSavingsTonPerYear.Should().BeGreaterThan(0);
    }

    #endregion
}
