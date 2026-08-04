using KSailCalc.Api.Services.Results;
using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Battery;
using KSailCalc.Api.Services.Interfaces;
using KSailCalc.Tests.TestHelpers;
using Moq;

namespace KSailCalc.Tests.Battery;

/// <summary>
/// The battery translation moved out of CalculatorService in story R-B: which plant side carries
/// which reserve, and which covered band offsets the Level 3 DRC variation.
/// </summary>
public class BatteryModeAdapterTests
{
    private static BatteryLoadAllocation Row(
        BatteryLoadType load, BatteryFunction function, double uncovered, double covered)
        => new()
        {
            Load = load,
            Function = function,
            UncoveredReserveKw = uncovered,
            CoveredBandKw = covered
        };

    private static BatteryModeAllocation Allocation() => new()
    {
        Mode = OperationalMode.DP,
        Loads =
        {
            Row(BatteryLoadType.Propulsion, BatteryFunction.PeakShaving, uncovered: 10, covered: 4),
            Row(BatteryLoadType.DpReserve, BatteryFunction.Reserve, uncovered: 20, covered: 5),
            Row(BatteryLoadType.DpDemand, BatteryFunction.PeakShaving, uncovered: 3, covered: 6),
            Row(BatteryLoadType.Hotel, BatteryFunction.PeakShaving, uncovered: 7, covered: 3),
            Row(BatteryLoadType.Mission, BatteryFunction.PeakShaving, uncovered: 1, covered: 2)
        }
    };

    [Fact]
    public void ToAdjustment_SendsThrustSideReserveToPropulsionAndTheRestToHotel()
    {
        var adjustment = BatteryModeAdapter.ToAdjustment(Allocation());

        adjustment.PropulsionReserveKw.Should().Be(33, "Propulsion 10 + DpReserve 20 + DpDemand 3");
        adjustment.HotelReserveKw.Should().Be(8, "Hotel 7 + Mission 1");
        adjustment.PropulsionPeakShavingKw.Should().Be(15, "covered band on thrust-side loads: 4 + 5 + 6");
    }

    [Fact]
    public void HotelPeakShavingKw_CountsOnlyElectricalPeakShavingRows()
    {
        BatteryModeAdapter.HotelPeakShavingKw(Allocation())
            .Should().Be(5, "Hotel 3 + Mission 2; thrust-side bands flow through PTI instead");
    }

    [Fact]
    public void HotelPeakShavingKw_IgnoresReserveRowsEvenOnTheHotelSide()
    {
        var allocation = new BatteryModeAllocation
        {
            Loads =
            {
                Row(BatteryLoadType.Hotel, BatteryFunction.Reserve, uncovered: 0, covered: 100),
                Row(BatteryLoadType.Mission, BatteryFunction.PeakShaving, uncovered: 0, covered: 2)
            }
        };

        BatteryModeAdapter.HotelPeakShavingKw(allocation)
            .Should().Be(2, "a reserve row is held ready, it does not shave the variation band");
    }

    [Fact]
    public void BuildBatteryDetails_ReturnsNull_WhenNoModeContributedAnOutcome()
    {
        var input = CalculatorInputBuilder.Default()
            .WithBattery(500, 1000, OperationalMode.Port).Build();

        var modes = new List<Api.Services.Results.ModePipelineResult>
        {
            new(OperationalMode.Transit, new Level1Result(), new Level2Result(), new Level3Result(), 5000, null)
        };

        BatteryDetailsBuilder.Build(input, modes)
            .Should().BeNull("no panel at all, rather than an empty one (G2/B10)");
    }

    [Fact]
    public void BuildBatteryDetails_SumsAcrossContributingModes()
    {
        var input = CalculatorInputBuilder.Default()
            .WithBattery(500, 1000, OperationalMode.Transit).WithFuelPrice(600).Build();

        var outcome = new Api.Services.Results.BatteryModeOutcome(
            new BatteryModeAllocation
            {
                Mode = OperationalMode.Transit,
                PeakShavingBandKw = 40,
                AdditionalSpinningReserveKw = 12
            },
            BenefitTonPerYear: 25);

        var modes = new List<Api.Services.Results.ModePipelineResult>
        {
            new(OperationalMode.Transit, new Level1Result(), new Level2Result(), new Level3Result(), 5000, outcome)
        };

        var details = BatteryDetailsBuilder.Build(input, modes);

        details.Should().NotBeNull();
        details!.PeakShavingKw.Should().Be(40);
        details.SpinningReserveKw.Should().Be(12);
        details.BenefitFocTonPerYear.Should().Be(25);
        details.BenefitCostPerYear.Should().Be(25 * 600);
        details.ModeAllocations.Should().ContainSingle();
    }
}
