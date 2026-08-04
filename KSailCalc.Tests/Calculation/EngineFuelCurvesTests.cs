using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Repositories.Interfaces;
using KSailCalc.Api.Services.Interfaces;
using KSailCalc.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace KSailCalc.Tests.Calculation;

/// <summary>
/// Story R-D: the SFOC curves are resolved once per calculation and the optimization levels read
/// them synchronously. These tests pin the two properties that matter — the lookup happens once,
/// and a missing curve still falls back to the documented default SFOC rather than throwing.
/// </summary>
public class EngineFuelCurvesTests
{
    // ─── AC3: resolved once, not once per SFOC lookup ───────────────────────────

    [Fact]
    public async Task AFullCalculation_ReadsTheEngineDataExactlyOnce()
    {
        var factory = TestServiceFactory.Create();
        // Create() itself resolves the convenience curves — start counting from the calculation.
        factory.AppDataMock.Invocations.Clear();

        var input = CalculatorInputBuilder.Default().Build();
        await factory.CalculatorService.CalculateAllVariantsAsync(input);

        factory.AppDataMock.Verify(x => x.GetInitialAppDataAsync(), Times.Once,
            "the curves are resolved once for the whole calculation; before R-D this was called " +
            "once per SFOC lookup, i.e. twice per candidate in the Level 1 loop");
    }

    [Fact]
    public async Task AFullCalculation_NeverUsesThePerLoadAsyncLookup()
    {
        // The levels must read the pre-resolved curves. If any of them fell back to the async
        // per-load lookup, the sort-per-call cost — and the swallow-everything catch — would be
        // back on the hot path without anyone noticing.
        var sfoc = new Mock<ISfocService>();
        var real = BuildRealSfocService(out var appDataMock);

        sfoc.Setup(s => s.GetCurvesAsync(It.IsAny<CalculatorInput>()))
            .Returns((CalculatorInput i) => real.GetCurvesAsync(i));

        var configRepo = new Mock<IKSailCalcConfigRepository>();
        configRepo.Setup(r => r.GetIntegrationLevelConfigsAsync())
            .ReturnsAsync(TestServiceFactory.DefaultIntegrationLevels);

        var sailRepo = new Mock<ISailContributionRepository>();
        sailRepo.Setup(r => r.GetSailContributionItemsAsync()).ReturnsAsync((List<SailContributionItem>?)null);

        var settings = Options.Create(new CalculatorSettings());
        var runner = new ModePipelineRunner(
            sfoc.Object,
            new Level1OptimizationService(Options.Create(new BatterySettings())),
            new Level2OptimizationService(),
            new Level3DrcService(settings),
            new BatteryAllocationService(Options.Create(new BatterySettings())),
            NullLogger<ModePipelineRunner>.Instance);

        var calculator = new CalculatorService(
            configRepo.Object, new SailContributionService(sailRepo.Object), runner, settings,
            NullLogger<CalculatorService>.Instance);

        await calculator.CalculateAllVariantsAsync(CalculatorInputBuilder.Default().Build());

        // "The levels never fall back to a per-load lookup" used to need two Times.Never checks here.
        // It is now structural: ISfocService offers nothing but GetCurvesAsync, so there is no
        // per-load path left to fall back to. What still needs asserting is that it happens ONCE.
        sfoc.Verify(s => s.GetCurvesAsync(It.IsAny<CalculatorInput>()), Times.Once);
        appDataMock.Verify(x => x.GetInitialAppDataAsync(), Times.Once);
    }

    // ─── AC5: missing curve still falls back, at every level ────────────────────

    [Fact]
    public void AMissingCurve_YieldsTheFallbackSfoc_NotAnException()
    {
        var curves = new EngineFuelCurves(new List<SfocDataPoint>(), new List<SfocDataPoint>());

        curves.Sfoc(0.5m, EngineCategory.Main).Should().Be(220);
        curves.Sfoc(0.5m, EngineCategory.Auxiliary).Should().Be(220);
        curves.Sfoc(0.5m, (EngineCategory)999).Should().Be(220,
            "an unrecognised category left the old lookup with null data and fell back");
    }

    [Fact]
    public async Task AnEngineWithNoSfocData_ProducesTheFallbackSfocThroughAllThreeLevels()
    {
        var (calculator, level1, level2, level3, curves) = BuildPlantWithNoSfocData();

        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(1000, 2)
            .WithPropulsionPower(3000)
            .WithSeaMargin(0)
            .WithTransitMode(5000, 1500)
            .Build();

        // Level 1: FOC is power × 220 g/kWh
        var l1 = level1.FindOptimalCombination(input, curves, OperationalMode.Transit);
        var optimal = l1.OptimalCombination;
        optimal.MeFocTonPerHour.Should().BeApproximately(optimal.MePowerKw * 220 / 1_000_000, 1e-12);
        optimal.AeFocTonPerHour.Should().BeApproximately(optimal.AePowerKw * 220 / 1_000_000, 1e-12);

        // Level 2: every setpoint reports the fallback SFOC
        var l2 = level2.OptimizeLoadSetpoints(l1, input, curves);
        l2.OptimalSetpoints.Where(s => s.PowerKw > 0).Should().OnlyContain(s => s.Sfoc == 220);

        // Level 3: a flat curve has no non-linearity for DRC to exploit ⇒ no savings
        var l3 = level3.CalculateDrcSavings(l2, input, curves, input.TransitHours);
        l3.DrcSavingsTonPerYear.Should().Be(0);

        // And the whole pipeline still answers rather than throwing
        var result = await calculator.CalculateAllVariantsAsync(input);
        result.BaselineFOC.Should().BeGreaterThan(0);
    }

    // ─── harness ────────────────────────────────────────────────────────────────

    private static SfocService BuildRealSfocService(out Mock<IAppDataAggregationService> appDataMock)
    {
        appDataMock = new Mock<IAppDataAggregationService>();
        appDataMock.Setup(x => x.GetInitialAppDataAsync()).ReturnsAsync(new AppInitialData
        {
            EngineTypes = new EngineTypesData
            {
                MainEngines = TestServiceFactory.DefaultMainEngines,
                AuxiliaryEngines = TestServiceFactory.DefaultAuxEngines
            },
            OperationalProfiles = new List<VesselOperationalProfile>(),
            Metadata = new AppDataMetadata { Version = "curves-test" }
        });

        return new SfocService(appDataMock.Object, new Mock<ILogger<SfocService>>().Object);
    }

    private static (CalculatorService Calculator, Level1OptimizationService L1,
        Level2OptimizationService L2, Level3DrcService L3, EngineFuelCurves Curves)
        BuildPlantWithNoSfocData()
    {
        var appData = new Mock<IAppDataAggregationService>();
        appData.Setup(x => x.GetInitialAppDataAsync()).ReturnsAsync(new AppInitialData
        {
            EngineTypes = new EngineTypesData
            {
                MainEngines = new List<EngineType> { new() { Id = 1, Name = "No curve ME", SfocData = new() } },
                AuxiliaryEngines = new List<AuxiliaryEngineType> { new() { Id = 1, Name = "No curve AE", SfocData = new() } }
            },
            OperationalProfiles = new List<VesselOperationalProfile>(),
            Metadata = new AppDataMetadata { Version = "no-sfoc" }
        });

        var sfoc = new SfocService(appData.Object, new Mock<ILogger<SfocService>>().Object);
        var curves = sfoc.GetCurvesAsync(CalculatorInputBuilder.Default().Build()).GetAwaiter().GetResult();

        var configRepo = new Mock<IKSailCalcConfigRepository>();
        configRepo.Setup(r => r.GetIntegrationLevelConfigsAsync())
            .ReturnsAsync(TestServiceFactory.DefaultIntegrationLevels);

        var sailRepo = new Mock<ISailContributionRepository>();
        sailRepo.Setup(r => r.GetSailContributionItemsAsync()).ReturnsAsync((List<SailContributionItem>?)null);

        var settings = Options.Create(new CalculatorSettings());
        var l1 = new Level1OptimizationService(Options.Create(new BatterySettings()));
        var l2 = new Level2OptimizationService();
        var l3 = new Level3DrcService(settings);

        var calculator = new CalculatorService(
            configRepo.Object,
            new SailContributionService(sailRepo.Object),
            new ModePipelineRunner(sfoc, l1, l2, l3,
                new BatteryAllocationService(Options.Create(new BatterySettings())),
                NullLogger<ModePipelineRunner>.Instance),
            settings,
            NullLogger<CalculatorService>.Instance);

        return (calculator, l1, l2, l3, curves);
    }
}
