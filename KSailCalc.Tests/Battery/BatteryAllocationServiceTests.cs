using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Tests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace KSailCalc.Tests.Battery;

/// <summary>
/// Story: Battery Increment A — Battery Allocation Engine.
/// Reference numbers come from the workbook
/// docs/PowerPlantSetupAdvisesIncludingPTIOAndbatteries_test.xlsx, sheet "Load Demands" rows 5-10.
/// </summary>
public class BatteryAllocationServiceTests
{
    private const double Precision = 1e-6;

    private static BatteryAllocationService CreateService(BatterySettings? settings = null)
        => new(Options.Create(settings ?? new BatterySettings()));

    /// <summary>
    /// Excel saved state: budget 1260 kW, Propulsion avg 11463 @ ±5%, Hotel avg 3800 @ ±2%,
    /// DP/Mission rows zero.
    /// </summary>
    private static CalculatorInput ExcelReferenceInput(double batteryPowerKw = 1260)
        => CalculatorInputBuilder.Default()
            .WithPropulsionPower(11463)
            .WithSeaMargin(0)
            .WithTransitMode(5000, 3800)
            .WithBattery(batteryPowerKw, 2000, OperationalMode.Transit)
            .Build();

    // ── AC1: Excel reference reproduction ────────────────────────────────────

    [Fact]
    public void Allocate_ExcelReferenceScenario_ReproducesWorkbookNumbers()
    {
        var service = CreateService();

        var result = service.Allocate(OperationalMode.Transit, ExcelReferenceInput());

        var propulsion = result.Loads.Single(l => l.Load == BatteryLoadType.Propulsion);
        propulsion.VariationKw.Should().BeApproximately(573.15, Precision);          // H
        propulsion.BatteryUsedKw.Should().BeApproximately(573.15, Precision);        // I
        propulsion.CoveredBandKw.Should().BeApproximately(200.6025, Precision);      // J
        propulsion.UncoveredReserveKw.Should().BeApproximately(372.5475, Precision); // L

        var hotel = result.Loads.Single(l => l.Load == BatteryLoadType.Hotel);
        hotel.VariationKw.Should().BeApproximately(76, Precision);
        hotel.BatteryUsedKw.Should().BeApproximately(76, Precision);
        hotel.CoveredBandKw.Should().BeApproximately(3.8, Precision);
        hotel.UncoveredReserveKw.Should().BeApproximately(72.2, Precision);

        result.CommittedBatteryKw.Should().BeApproximately(649.15, Precision);            // ΣI
        result.PeakShavingBandKw.Should().BeApproximately(204.4025, Precision);           // ΣJ
        result.AdditionalSpinningReserveKw.Should().BeApproximately(444.7475, Precision); // ΣL
        result.RemainingBatteryKw.Should().BeApproximately(610.85, Precision);            // final K
    }

    [Fact]
    public void Allocate_ExcelReferenceScenario_RowsFollowConfiguredPriorityOrder()
    {
        var service = CreateService();

        var result = service.Allocate(OperationalMode.Transit, ExcelReferenceInput());

        // Transit rows in priority order: Mission → Propulsion → Hotel
        result.Loads.Select(l => l.Load).Should().ContainInOrder(
            BatteryLoadType.Mission, BatteryLoadType.Propulsion, BatteryLoadType.Hotel);
        result.Loads.Should().NotContain(l =>
            l.Load == BatteryLoadType.DpReserve || l.Load == BatteryLoadType.DpDemand);
    }

    // ── AC2: budget exhaustion ───────────────────────────────────────────────

    [Fact]
    public void Allocate_SmallBudget_FirstPriorityLoadConsumesIt_LaterLoadsUncovered()
    {
        var service = CreateService();

        var result = service.Allocate(OperationalMode.Transit, ExcelReferenceInput(batteryPowerKw: 100));

        var propulsion = result.Loads.Single(l => l.Load == BatteryLoadType.Propulsion);
        propulsion.BatteryUsedKw.Should().BeApproximately(100, Precision);
        propulsion.CoveredBandKw.Should().BeApproximately(35, Precision);             // 100 × 0.35
        propulsion.UncoveredReserveKw.Should().BeApproximately(538.15, Precision);    // 573.15 − 35

        var hotel = result.Loads.Single(l => l.Load == BatteryLoadType.Hotel);
        hotel.BatteryUsedKw.Should().Be(0);
        hotel.UncoveredReserveKw.Should().BeApproximately(76, Precision);             // L = H

        result.RemainingBatteryKw.Should().Be(0);
    }

    // ── AC3: zero / inactive battery ─────────────────────────────────────────

    [Theory]
    [InlineData(0)]     // PowerKw = 0
    [InlineData(-50)]   // defensive clamp
    public void Allocate_ZeroOrNegativePower_NoCoverage_FullVariationUncovered(double powerKw)
    {
        var service = CreateService();
        var input = ExcelReferenceInput(batteryPowerKw: powerKw);

        var result = service.Allocate(OperationalMode.Transit, input);

        result.PeakShavingBandKw.Should().Be(0);
        result.CommittedBatteryKw.Should().Be(0);
        result.AdditionalSpinningReserveKw.Should().BeApproximately(649.15, Precision); // ΣH
    }

    [Fact]
    public void Allocate_NullBattery_NoCoverage_FullVariationUncovered()
    {
        var service = CreateService();
        var input = ExcelReferenceInput();
        input.Battery = null;

        var result = service.Allocate(OperationalMode.Transit, input);

        result.PeakShavingBandKw.Should().Be(0);
        result.AdditionalSpinningReserveKw.Should().BeApproximately(649.15, Precision);
    }

    [Fact]
    public void Allocate_ModeNotInRelevantModes_NoCoverage()
    {
        var service = CreateService();
        var input = CalculatorInputBuilder.Default()
            .WithPropulsionPower(11463)
            .WithSeaMargin(0)
            .WithTransitMode(5000, 3800)
            .WithBattery(1260, 2000, OperationalMode.Port) // battery relevant for Port only
            .Build();

        var result = service.Allocate(OperationalMode.Transit, input);

        result.PeakShavingBandKw.Should().Be(0);
        result.AdditionalSpinningReserveKw.Should().BeApproximately(649.15, Precision);
    }

    // ── AC4: config-driven behaviour ─────────────────────────────────────────

    [Fact]
    public void Allocate_CustomCoverageFactor_ChangesCoveredBand()
    {
        var settings = new BatterySettings { LoadPriorities = BatterySettings.CreateDefaultLoadPriorities() };
        settings.LoadPriorities.Single(p => p.Load == BatteryLoadType.Propulsion).CoverageFactor = 1.0;
        var service = CreateService(settings);

        var result = service.Allocate(OperationalMode.Transit, ExcelReferenceInput());

        var propulsion = result.Loads.Single(l => l.Load == BatteryLoadType.Propulsion);
        propulsion.CoveredBandKw.Should().BeApproximately(573.15, Precision); // J = I × 1.0
        propulsion.UncoveredReserveKw.Should().Be(0);
    }

    [Fact]
    public void Allocate_CustomPriorityOrder_ChangesWhichLoadGetsTheBudget()
    {
        // Hotel first: a 100 kW budget goes to Hotel (H = 76), remainder 24 to Propulsion
        var settings = new BatterySettings { LoadPriorities = BatterySettings.CreateDefaultLoadPriorities() };
        var hotel = settings.LoadPriorities.Single(p => p.Load == BatteryLoadType.Hotel);
        settings.LoadPriorities.Remove(hotel);
        settings.LoadPriorities.Insert(0, hotel);
        var service = CreateService(settings);

        var result = service.Allocate(OperationalMode.Transit, ExcelReferenceInput(batteryPowerKw: 100));

        result.Loads.Single(l => l.Load == BatteryLoadType.Hotel)
            .BatteryUsedKw.Should().BeApproximately(76, Precision);
        result.Loads.Single(l => l.Load == BatteryLoadType.Propulsion)
            .BatteryUsedKw.Should().BeApproximately(24, Precision);
    }

    // ── Reserve semantics + DP mode mapping ──────────────────────────────────

    [Fact]
    public void Allocate_DpMode_MapsExpectedLoadRows_InPriorityOrder()
    {
        var service = CreateService();
        var input = CalculatorInputBuilder.Default()
            .WithDPMode(2000, 1500, 4000)
            .WithBattery(500, 1000, OperationalMode.DP)
            .Build();

        var result = service.Allocate(OperationalMode.DP, input);

        result.Loads.Select(l => l.Load).Should().ContainInOrder(
            BatteryLoadType.DpReserve, BatteryLoadType.DpDemand,
            BatteryLoadType.Mission, BatteryLoadType.Hotel);
        result.Loads.Should().NotContain(l => l.Load == BatteryLoadType.Propulsion);

        // Default DpDemand/Hotel variation factors: DpDemand 0 → H = 0; Hotel 0.02 → H = 30
        result.Loads.Single(l => l.Load == BatteryLoadType.Hotel)
            .VariationKw.Should().BeApproximately(30, Precision); // 1500 × 0.02
    }

    [Fact]
    public void Allocate_ReserveFunction_UsesFullRequirementAsVariation()
    {
        // Custom settings: make the DP demand row a RESERVE row (Excel row 5 semantics:
        // C5 = "RESERVE" → coverage 100%, H = full requirement)
        var settings = new BatterySettings { LoadPriorities = BatterySettings.CreateDefaultLoadPriorities() };
        var dpDemand = settings.LoadPriorities.Single(p => p.Load == BatteryLoadType.DpDemand);
        dpDemand.Function = BatteryFunction.Reserve;
        dpDemand.CoverageFactor = 1.0;
        dpDemand.VariationFactor = 0.10;
        var service = CreateService(settings);

        var input = CalculatorInputBuilder.Default()
            .WithDPMode(2000, 1500, 4000)
            .WithBattery(10_000, 20_000, OperationalMode.DP)
            .Build();

        var result = service.Allocate(OperationalMode.DP, input);

        var row = result.Loads.Single(l => l.Load == BatteryLoadType.DpDemand);
        row.VariationKw.Should().BeApproximately(4400, Precision);       // H = 4000 × 1.10 (full req.)
        row.BatteryUsedKw.Should().BeApproximately(4400, Precision);     // I
        row.CoveredBandKw.Should().BeApproximately(4400, Precision);     // J = I × 1.0
        row.UncoveredReserveKw.Should().Be(0);                           // L
    }
}

