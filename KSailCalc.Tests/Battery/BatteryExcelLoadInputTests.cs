using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Tests.TestHelpers;
using Microsoft.Extensions.Options;

namespace KSailCalc.Tests.Battery;

/// <summary>
/// Story: Battery Increment F — the two remaining Excel load inputs (decision D4):
/// DP redundancy requirement (Load Demands R5 / input O2) and Mission heavy-consumer max
/// (R7 / input I3, variation = the FULL value when mission ops exist, not avg×factor).
/// </summary>
public class BatteryExcelLoadInputTests
{
    private const double Precision = 1e-6;

    private static BatteryAllocationService Allocator()
        => new(Options.Create(new BatterySettings()));

    // ── AC2: DP redundancy — RESERVE semantics, first in priority ────────────

    [Fact]
    public void DpRedundancy_CoveredOneToOne_BeforeAllOtherLoads()
    {
        var input = CalculatorInputBuilder.Default()
            .WithDPMode(2000, 1500, 4000)
            .WithBattery(500, 1000, OperationalMode.DP).Build();
        input.DpRedundancyRequirementKw = 400;

        var result = Allocator().Allocate(OperationalMode.DP, input);

        var reserve = result.Loads.Single(l => l.Load == BatteryLoadType.DpReserve);
        reserve.VariationKw.Should().BeApproximately(400, Precision);   // H = full requirement
        reserve.BatteryUsedKw.Should().BeApproximately(400, Precision); // I
        reserve.CoveredBandKw.Should().BeApproximately(400, Precision); // J = I × 100%
        reserve.UncoveredReserveKw.Should().Be(0);                      // L

        var hotel = result.Loads.Single(l => l.Load == BatteryLoadType.Hotel);
        hotel.BatteryUsedKw.Should().BeApproximately(30, Precision);    // remaining 100 covers H=30
        hotel.CoveredBandKw.Should().BeApproximately(1.5, Precision);
        hotel.UncoveredReserveKw.Should().BeApproximately(28.5, Precision);

        result.CommittedBatteryKw.Should().BeApproximately(430, Precision);
        // RESERVE coverage is not "± peak shaving" — PS band comes from PS rows only
        result.PeakShavingBandKw.Should().BeApproximately(1.5, Precision);
        result.AdditionalSpinningReserveKw.Should().BeApproximately(28.5, Precision);
    }

    // ── AC3: priority order bites when the budget is short ───────────────────

    [Fact]
    public void DpRedundancy_ShortBudget_ConsumesItBeforeHotel()
    {
        var input = CalculatorInputBuilder.Default()
            .WithDPMode(2000, 1500, 4000)
            .WithBattery(300, 1000, OperationalMode.DP).Build();
        input.DpRedundancyRequirementKw = 400;

        var result = Allocator().Allocate(OperationalMode.DP, input);

        var reserve = result.Loads.Single(l => l.Load == BatteryLoadType.DpReserve);
        reserve.BatteryUsedKw.Should().BeApproximately(300, Precision);
        reserve.CoveredBandKw.Should().BeApproximately(300, Precision);
        reserve.UncoveredReserveKw.Should().BeApproximately(100, Precision); // 400 − 300

        var hotel = result.Loads.Single(l => l.Load == BatteryLoadType.Hotel);
        hotel.BatteryUsedKw.Should().Be(0);
        hotel.UncoveredReserveKw.Should().BeApproximately(30, Precision); // L = H

        result.AdditionalSpinningReserveKw.Should().BeApproximately(130, Precision);
    }

    // ── AC4: Mission heavy-consumer max — variation is the FULL value ────────

    [Fact]
    public void OthersMax_VariationIsFullValue_AndOutranksPropulsion()
    {
        // Epic E2: this used to be the Mission-in-Transit test; the Others row carries the
        // identical arithmetic (full kW, coverage 0.50, hotel side) — deliberately, so the
        // Excel-verified numbers survive as a regression pin under the new row name (D-BI4).
        var input = CalculatorInputBuilder.Default()
            .WithPropulsionPower(11463).WithSeaMargin(0)
            .WithTransitMode(5000, 3800)
            .WithBattery(1260, 2000, OperationalMode.Transit).Build();
        input.OthersConsumerMaxKw = 3000;

        var result = Allocator().Allocate(OperationalMode.Transit, input);

        var others = result.Loads.Single(l => l.Load == BatteryLoadType.Others);
        others.AverageLoadKw.Should().Be(0);                              // avg lives in Hotel/Mission input
        others.VariationKw.Should().BeApproximately(3000, Precision);     // H = entered kW as-is
        others.BatteryUsedKw.Should().BeApproximately(1260, Precision);   // whole budget
        others.CoveredBandKw.Should().BeApproximately(630, Precision);    // × 50%
        others.UncoveredReserveKw.Should().BeApproximately(2370, Precision);

        // Nothing left for the lower-priority rows
        result.Loads.Single(l => l.Load == BatteryLoadType.Propulsion)
            .UncoveredReserveKw.Should().BeApproximately(573.15, Precision);
        result.Loads.Single(l => l.Load == BatteryLoadType.Hotel)
            .UncoveredReserveKw.Should().BeApproximately(76, Precision);

        result.PeakShavingBandKw.Should().BeApproximately(630, Precision);
        result.AdditionalSpinningReserveKw.Should().BeApproximately(3019.15, Precision);
    }

    [Fact]
    public void MissionInTransit_IsInert_TheRowDoesNotExist()
    {
        // D-BI1: the client's correction — mission operations are a DP affair. A mission value
        // entered alongside a Transit battery must change nothing.
        var input = CalculatorInputBuilder.Default()
            .WithPropulsionPower(11463).WithSeaMargin(0)
            .WithTransitMode(5000, 3800)
            .WithBattery(1260, 2000, OperationalMode.Transit).Build();
        input.MissionHeavyConsumerMaxKw = 3000;

        var result = Allocator().Allocate(OperationalMode.Transit, input);

        result.Loads.Should().NotContain(l => l.Load == BatteryLoadType.Mission);
        // With the budget free of the crane, Transit allocates exactly like scenario 01:
        result.PeakShavingBandKw.Should().BeApproximately(204.4025, Precision);
        result.AdditionalSpinningReserveKw.Should().BeApproximately(444.7475, Precision);
    }

    [Fact]
    public void MissionInDp_KeepsTheFullValueBehaviour()
    {
        // Coverage note: pre-E2 the full-value mission tests ran in Transit; they became Others
        // tests. This is now the only value-carrying Mission pin, in its one remaining home.
        var input = CalculatorInputBuilder.Default()
            .WithDPMode(2000, 1500, 4000)
            .WithBattery(1260, 2000, OperationalMode.DP).Build();
        input.MissionHeavyConsumerMaxKw = 500;

        var result = Allocator().Allocate(OperationalMode.DP, input);

        var mission = result.Loads.Single(l => l.Load == BatteryLoadType.Mission);
        mission.VariationKw.Should().BeApproximately(500, Precision);   // H = full value, as-is
        mission.BatteryUsedKw.Should().BeApproximately(500, Precision); // budget 1260 suffices
        mission.CoveredBandKw.Should().BeApproximately(250, Precision); // × 50%
    }

    [Fact]
    public void OthersRow_ExistsInPort_ButNotInDp()
    {
        // D-BI5: Others covers the remaining relevant modes (Transit, Port); DP keeps Mission.
        var input = CalculatorInputBuilder.Default()
            .WithDPMode(2000, 1500, 4000)
            .WithBattery(500, 1000, OperationalMode.DP, OperationalMode.Port).Build();
        input.PortHours = 500;
        input.PortHotelPowerKW = 2000;
        input.OthersConsumerMaxKw = 300;

        var port = Allocator().Allocate(OperationalMode.Port, input);
        var others = port.Loads.Single(l => l.Load == BatteryLoadType.Others);
        others.VariationKw.Should().BeApproximately(300, Precision);
        others.CoveredBandKw.Should().BeApproximately(150, Precision);    // × 50%

        var dp = Allocator().Allocate(OperationalMode.DP, input);
        dp.Loads.Should().NotContain(l => l.Load == BatteryLoadType.Others);
        dp.Loads.Should().Contain(l => l.Load == BatteryLoadType.Mission);
    }

    // ── AC5 + AC1: absent inputs ⇒ rows stay 0 (zero regression) ─────────────

    [Fact]
    public void AbsentInputs_MissionAndDpReserveRowsStayZero()
    {
        var input = CalculatorInputBuilder.Default()
            .WithDPMode(2000, 1500, 4000)
            .WithBattery(500, 1000, OperationalMode.DP).Build();
        // DpRedundancyRequirementKw / MissionHeavyConsumerMaxKw left null

        var result = Allocator().Allocate(OperationalMode.DP, input);

        result.Loads.Single(l => l.Load == BatteryLoadType.DpReserve).VariationKw.Should().Be(0);
        result.Loads.Single(l => l.Load == BatteryLoadType.Mission).VariationKw.Should().Be(0);
        // Same totals as scenario A7 (pre-Increment-F behaviour)
        result.PeakShavingBandKw.Should().BeApproximately(1.5, Precision);
        result.AdditionalSpinningReserveKw.Should().BeApproximately(28.5, Precision);
    }

    // ═══ Family H — end-to-end pipeline effects of the new inputs ════════════

    /// <summary>Excel plant sized for the pipeline (see test-design doc).</summary>
    private static CalculatorInputBuilder ExcelPlant() => CalculatorInputBuilder.Default()
        .WithMainEngines(24000, 2)
        .WithShaftGenerators(1000)
        .WithAuxiliaryEngines(800, 3)
        .WithPropulsionPower(11463).WithSeaMargin(0)
        .WithTransitMode(5000, 3800);

    [Fact]
    public async Task H1_DpRedundancy_EndToEnd_CoveredReserveYieldsBenefit()
    {
        var factory = TestServiceFactory.Create();
        var input = ExcelPlant()
            .WithDPMode(2000, 1500, 4000)
            .WithBattery(500, 1000, OperationalMode.DP).Build();
        input.DpRedundancyRequirementKw = 400;

        var result = await factory.CalculatorService.CalculateAllVariantsAsync(input);

        var details = result.BatteryDetails!;
        details.SpinningReserveKw.Should().BeApproximately(28.5, Precision);  // redundancy fully covered (L=0)
        details.PeakShavingKw.Should().BeApproximately(1.5, Precision);       // RESERVE J is not a ± band
        // Reference scenario carries the UNCOVERED redundancy (400 kW on the thrust side)
        // as genset reserve ⇒ strictly higher FOC ⇒ measurable battery benefit
        details.BenefitFocTonPerYear.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task H2_DpRedundancyBand_FlowsThroughPtiDischargeGate()
    {
        var factory = TestServiceFactory.Create();

        // The covered redundancy (J = 400) is a thrust-side band → must fit PTI headroom.
        // MaxPti 250/engine: 2-ME DP combos have 500 kW headroom ≥ 400 ⇒ calculation succeeds
        var enough = ExcelPlant()
            .WithDPMode(2000, 1500, 4000)
            .WithBattery(500, 1000, OperationalMode.DP).Build();
        enough.DpRedundancyRequirementKw = 400;
        enough.MaxPtiPerEngineKw = 250;
        var ok = await factory.CalculatorService.CalculateAllVariantsAsync(enough);
        ok.BatteryDetails.Should().NotBeNull();

        // MaxPti 100/engine: best headroom 200 < 400 ⇒ every DP combo gated ("Insufficient PTI")
        var tooSmall = ExcelPlant()
            .WithDPMode(2000, 1500, 4000)
            .WithBattery(500, 1000, OperationalMode.DP).Build();
        tooSmall.DpRedundancyRequirementKw = 400;
        tooSmall.MaxPtiPerEngineKw = 100;
        var act = () => factory.CalculatorService.CalculateAllVariantsAsync(tooSmall);
        await act.Should().ThrowAsync<NoValidCombinationException>()
            .WithMessage("*No valid engine combinations*");
    }

    [Fact]
    public async Task H3_OthersMax_EndToEnd_RaisesHotelDemand_AndAbsorbsDrcVariation()
    {
        // Epic E2: was the Mission end-to-end test; Others carries the identical arithmetic in
        // Transit (D-BI4), so every expected number below is unchanged.
        var factory = TestServiceFactory.Create();
        // Bigger aux plant: uncovered Others reserve (2370 kW) lands on the HOTEL side
        var input = ExcelPlant().WithAuxiliaryEngines(2000, 3)
            .WithBattery(1260, 2000, OperationalMode.Transit).Build();
        input.OthersConsumerMaxKw = 3000;
        input.HotelLoadVariationKw = 500;

        var result = await factory.CalculatorService.CalculateAllVariantsAsync(input);

        var details = result.BatteryDetails!;
        details.PeakShavingKw.Should().BeApproximately(630, Precision);
        details.SpinningReserveKw.Should().BeApproximately(3019.15, Precision);

        // Adjusted hotel = 3800 + 2370 (mission L) + 76 (hotel L) = 6246 ⇒ only AE-3 combos survive
        var l1 = result.Level1Details!;
        l1.ValidCombinationsCount.Should().Be(2);        // {1 ME, SG, 3 AE} and {2 ME, SG, 3 AE}
        l1.SelectedBaselineIndex.Should().Be(0);         // max(0, 2 − 3)

        // Others' covered band (630, hotel side) fully absorbs the ±500 DRC variation (clamped)
        var l3 = result.Premium.Level3Details!;
        l3.BatteryShavedVariationKw.Should().BeApproximately(500, Precision);
        l3.VariationPerGeneratorKw.Should().Be(0);
        l3.DrcSavingsTonPerYear.Should().Be(0);
    }

    // ── AC6: validation ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(-1, 0, 0, "DP redundancy requirement cannot be negative")]
    [InlineData(0, -1, 0, "Mission heavy-consumer maximum cannot be negative")]
    [InlineData(0, 0, -1, "Others battery demand cannot be negative")]
    public void Validate_NegativeExcelLoadInputs_ProduceErrors(
        double dpRedundancy, double missionMax, double othersMax, string expectedError)
    {
        var service = new ValidationService();
        var input = CalculatorInputBuilder.Default().Build();
        input.DpRedundancyRequirementKw = dpRedundancy;
        input.MissionHeavyConsumerMaxKw = missionMax;
        input.OthersConsumerMaxKw = othersMax;

        var result = service.ValidateInput(input);

        result.Valid.Should().BeFalse();
        result.Errors.Should().Contain(expectedError);
    }

    [Fact]
    public void Validate_DpRedundancyWithoutDpMode_ProducesWarningNotError()
    {
        var service = new ValidationService();
        var input = CalculatorInputBuilder.Default().WithoutDPMode().Build();
        input.DpRedundancyRequirementKw = 400;

        var result = service.ValidateInput(input);

        result.Valid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Type == "battery" && w.Message.Contains("DP redundancy"));
    }
}
