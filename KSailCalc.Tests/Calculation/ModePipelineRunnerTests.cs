using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Battery;
using KSailCalc.Api.Services.Interfaces;
using KSailCalc.Tests.TestHelpers;
using Moq;

namespace KSailCalc.Tests.Calculation;

/// <summary>
/// Direct unit tests for the pipeline runner and the battery adapter extracted from
/// CalculatorService (Refactor story R-B). These pin the two asymmetries that are easy to break
/// and that the golden snapshots only reach indirectly: Levels 2/3 run for Transit only, and the
/// user-pinned baseline applies to Transit only.
/// </summary>
public class ModePipelineRunnerTests
{
    // ─── harness ────────────────────────────────────────────────────────────────

    private readonly Mock<ISfocService> _sfoc = new();
    private readonly Mock<ILevel1OptimizationService> _level1 = new();
    private readonly Mock<ILevel2OptimizationService> _level2 = new();
    private readonly Mock<ILevel3DrcService> _level3 = new();
    private readonly Mock<IBatteryAllocationService> _battery = new();

    private static readonly EngineFuelCurves NoCurves =
        new(new List<SfocDataPoint>(), new List<SfocDataPoint>());

    public ModePipelineRunnerTests()
    {
        _sfoc.Setup(s => s.GetCurvesAsync(It.IsAny<CalculatorInput>())).ReturnsAsync(NoCurves);

        _level1.Setup(s => s.FindOptimalCombination(
                It.IsAny<CalculatorInput>(), It.IsAny<EngineFuelCurves>(), It.IsAny<OperationalMode>(),
                It.IsAny<double?>(), It.IsAny<int?>(), It.IsAny<BatteryL1Adjustment?>()))
            .Returns(() => new Level1Result
            {
                OptimalCombination = new EngineCombination { ActiveMeCount = 1, ActiveAeCount = 1 },
                BaselineCombination = new EngineCombination { ActiveMeCount = 2, ActiveAeCount = 2 },
                OptimalFocTonPerHour = 0.9,
                BaselineFocTonPerHour = 1.2
            });

        _level2.Setup(s => s.OptimizeLoadSetpoints(
                It.IsAny<Level1Result>(), It.IsAny<CalculatorInput>(), It.IsAny<EngineFuelCurves>()))
            .Returns(new Level2Result());

        _level3.Setup(s => s.CalculateDrcSavings(
                It.IsAny<Level2Result>(), It.IsAny<CalculatorInput>(), It.IsAny<EngineFuelCurves>(),
                It.IsAny<double>(), It.IsAny<double>()))
            .Returns(new Level3Result());

        _battery.Setup(s => s.Allocate(
                It.IsAny<OperationalMode>(), It.IsAny<CalculatorInput>(),
                It.IsAny<double?>(), It.IsAny<double?>()))
            .Returns((OperationalMode m, CalculatorInput _, double? __, double? ___)
                => new BatteryModeAllocation { Mode = m });
    }

    private ModePipelineRunner Sut()
        => new(_sfoc.Object, _level1.Object, _level2.Object, _level3.Object, _battery.Object,
            NullLogger<ModePipelineRunner>.Instance);

    private static CalculatorInput TransitAndPort()
    {
        var input = CalculatorInputBuilder.Default().Build();
        input.PortHours = 1000;
        input.PortHotelPowerKW = 400;
        return input;
    }

    // ─── mode asymmetries ───────────────────────────────────────────────────────

    [Fact]
    public async Task RunAllModes_RunsLevels2And3ForTransitOnly()
    {
        var result = await Sut().RunAllModesAsync(TransitAndPort(), null);

        result.Should().HaveCount(2);
        result[0].Mode.Should().Be(OperationalMode.Transit, "Transit is always reported first");
        result[1].Mode.Should().Be(OperationalMode.Port);

        _level2.Verify(s => s.OptimizeLoadSetpoints(
                It.IsAny<Level1Result>(), It.IsAny<CalculatorInput>(), It.IsAny<EngineFuelCurves>()),
            Times.Once, "Level 2 has no Excel counterpart outside Transit (D4/Q5)");
        _level3.Verify(s => s.CalculateDrcSavings(
                It.IsAny<Level2Result>(), It.IsAny<CalculatorInput>(), It.IsAny<EngineFuelCurves>(),
                It.IsAny<double>(), It.IsAny<double>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAllModes_AppliesThePinnedBaselineToTransitOnly()
    {
        var input = TransitAndPort();
        input.BaselineIndex = 2;

        await Sut().RunAllModesAsync(input, null);

        _level1.Verify(s => s.FindOptimalCombination(
                input, It.IsAny<EngineFuelCurves>(), OperationalMode.Transit, It.IsAny<double?>(), 2, It.IsAny<BatteryL1Adjustment?>()), Times.Once);
        _level1.Verify(s => s.FindOptimalCombination(
                input, It.IsAny<EngineFuelCurves>(), OperationalMode.Port, It.IsAny<double?>(), null, It.IsAny<BatteryL1Adjustment?>()), Times.Once);
    }

    [Fact]
    public async Task RunAllModes_PassesTheSailAdjustedPropulsionToTransit()
    {
        var input = TransitAndPort();

        await Sut().RunAllModesAsync(input, transitPropulsionOverrideKw: 3200);

        _level1.Verify(s => s.FindOptimalCombination(
                input, It.IsAny<EngineFuelCurves>(), OperationalMode.Transit, 3200, It.IsAny<int?>(), It.IsAny<BatteryL1Adjustment?>()), Times.Once);
        _level1.Verify(s => s.FindOptimalCombination(
                input, It.IsAny<EngineFuelCurves>(), OperationalMode.Port, null, It.IsAny<int?>(), It.IsAny<BatteryL1Adjustment?>()), Times.Once);
    }

    [Fact]
    public async Task RunAllModes_SkipsInactiveModes()
    {
        var input = CalculatorInputBuilder.Default().Build(); // Transit only

        var result = await Sut().RunAllModesAsync(input, null);

        result.Should().ContainSingle().Which.Mode.Should().Be(OperationalMode.Transit);
    }

    // ─── battery gating (AC6) ───────────────────────────────────────────────────

    [Fact]
    public async Task RunAllModes_NeverTouchesTheBattery_WhenNoBatteryIsConfigured()
    {
        await Sut().RunAllModesAsync(TransitAndPort(), null);

        _battery.Verify(s => s.Allocate(
                It.IsAny<OperationalMode>(), It.IsAny<CalculatorInput>(),
                It.IsAny<double?>(), It.IsAny<double?>()),
            Times.Never, "an inactive battery must leave the pre-battery code path untouched");

        _level1.Verify(s => s.FindOptimalCombination(
                It.IsAny<CalculatorInput>(), It.IsAny<EngineFuelCurves>(), It.IsAny<OperationalMode>(),
                It.IsAny<double?>(), It.IsAny<int?>(), null),
            Times.Exactly(2), "both modes run Level 1 with no battery adjustment");
    }

    [Fact]
    public async Task RunAllModes_NeverTouchesTheBattery_ForModesTheBatteryDoesNotApplyTo()
    {
        var input = TransitAndPort();
        input.Battery = new BatteryConfigurationInput
        {
            PowerKw = 500,
            CapacityKwh = 1000,
            RelevantModes = new List<OperationalMode> { OperationalMode.Transit }
        };

        await Sut().RunAllModesAsync(input, null);

        _battery.Verify(s => s.Allocate(
                OperationalMode.Transit, It.IsAny<CalculatorInput>(), It.IsAny<double?>(), It.IsAny<double?>()),
            Times.Exactly(2), "the real allocation plus the R3a zero-budget reference run");
        _battery.Verify(s => s.Allocate(
                OperationalMode.Port, It.IsAny<CalculatorInput>(), It.IsAny<double?>(), It.IsAny<double?>()),
            Times.Never, "the battery is not relevant to Port in this configuration");
    }

    [Fact]
    public async Task RunAllModes_RunsTheR3aReferenceScenarioWithAZeroBudgetAndNoPinnedBaseline()
    {
        var input = TransitAndPort();
        input.BaselineIndex = 1;
        input.Battery = new BatteryConfigurationInput
        {
            PowerKw = 500,
            CapacityKwh = 1000,
            RelevantModes = new List<OperationalMode> { OperationalMode.Transit }
        };

        await Sut().RunAllModesAsync(input, null);

        _battery.Verify(s => s.Allocate(OperationalMode.Transit, input, 0, It.IsAny<double?>()),
            Times.Once, "the reference scenario carries the full variation on the gensets");
        _level1.Verify(s => s.FindOptimalCombination(
                input, It.IsAny<EngineFuelCurves>(), OperationalMode.Transit, It.IsAny<double?>(), null, It.IsAny<BatteryL1Adjustment>()),
            Times.Once, "the reference run must not inherit the user-pinned baseline");
    }
}

