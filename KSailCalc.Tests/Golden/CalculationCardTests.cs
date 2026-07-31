using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Tests.Golden;

/// <summary>
/// The headline numbers of every calculation card in
/// docs/qa/manual-test-scenarios/calculations, written out by hand.
///
/// Why this exists next to <see cref="GoldenMasterTests"/>: snapshots can be re-approved with
/// GOLDEN_UPDATE=1, so a broken refactor can be blessed by someone who does not read the diff.
/// These numbers come from the documents, not from the program — nothing regenerates them. If one
/// moves, either the card or the code is wrong, and a human has to decide which.
///
/// Deliberately NOT exhaustive: only what a card puts in its headline (tiles, benefit, baseline,
/// IL1) plus each scenario's reason for existing. Everything else is covered by the snapshots.
/// </summary>
public class CalculationCardTests
{
    // Tolerances match the precision the cards are written in.
    private const double Kw = 1e-3;      // allocation figures are quoted to 4 decimals
    private const double TonPerYear = 0.05;
    private const double Usd = 1.0;

    // ─── 01 — Excel baseline (battery 1260, Transit) ────────────────────────────

    [Fact]
    public async Task Card01_ExcelBaseline_CascadeTilesBenefitAndTiers()
    {
        var r = await GoldenScenario.CalculateAsync("01-excel-baseline.json");

        // "Propulsion 573.15 → J 200.60 / L 372.55 · Hotel 76 → J 3.8 / L 72.2 · 610.85 unused"
        var transit = SingleAllocation(r);
        var propulsion = Row(transit, BatteryLoadType.Propulsion);
        propulsion.VariationKw.Should().BeApproximately(573.15, Kw);
        propulsion.CoveredBandKw.Should().BeApproximately(200.6025, Kw);
        propulsion.UncoveredReserveKw.Should().BeApproximately(372.5475, Kw);

        var hotel = Row(transit, BatteryLoadType.Hotel);
        hotel.VariationKw.Should().BeApproximately(76, Kw);
        hotel.CoveredBandKw.Should().BeApproximately(3.8, Kw);
        hotel.UncoveredReserveKw.Should().BeApproximately(72.2, Kw);
        transit.RemainingBatteryKw.Should().BeApproximately(610.85, Kw);

        // "Tiles: PS = 204.4 · SR = 444.7"
        r.BatteryDetails!.PeakShavingKw.Should().BeApproximately(204.4025, Kw);
        r.BatteryDetails.SpinningReserveKw.Should().BeApproximately(444.7475, Kw);

        // "SG = 3 250 · AE = 622 · ME = 15 086 (header)"
        r.PowerDemands.ShaftGeneratorPowerKw.Should().BeApproximately(3250, 0.5);
        r.PowerDemands.AuxiliaryEnginePowerKw.Should().BeApproximately(622.2, 0.5);
        r.PowerDemands.MainEnginePowerKw.Should().BeApproximately(15085.5, 0.5);

        // "baseline = 3rd-from-worst (1 ME+SG+3 AE) 2.6615 → 13 307.4 t/yr; IL1 13 286.2; savings 21.2"
        r.Level1Details!.ValidCombinationsCount.Should().Be(5);
        r.Level1Details.SelectedBaselineIndex.Should().Be(2);
        r.Level1Details.BaselineAeCount.Should().Be(3);
        r.BaselineFOC.Should().BeApproximately(13307.4, TonPerYear);
        r.Advanced.OptimizedFOC.Should().BeApproximately(13286.2, TonPerYear);
        r.Advanced.FuelSavings.Should().BeApproximately(21.16, 0.01);

        // "CO2 = 52 333.6 (ME 49 494.9 + AE 2 838.7 — cards must sum to the total)"
        r.BaselineCO2.Should().BeApproximately(52333.6, 0.1);
        (r.BaselineMeCO2 + r.BaselineAeCO2).Should().BeApproximately(r.BaselineCO2, 1e-6);
        r.BaselineMeCO2.Should().BeApproximately(49494.9, 0.1);
        r.BaselineAeCO2.Should().BeApproximately(2838.7, 0.1);

        // "Benefit = 173.66 t/yr × 780 = $135 452"
        r.BatteryDetails.BenefitFocTonPerYear.Should().BeApproximately(173.66, 0.01);
        r.BatteryDetails.BenefitCostPerYear.Should().BeApproximately(135452, Usd);

        // "IL2 adds 0 … IL3 adds 0 here — all three tier chips show 21.2, correct, not a bug"
        r.Pro.Level2SavingsTonPerYear.Should().Be(0);
        r.Premium.Level3SavingsTonPerYear.Should().Be(0);
    }

    // ─── 02 — Small battery 300 kW (budget exhausts mid-cascade) ────────────────

    [Fact]
    public async Task Card02_SmallBattery_PropulsionTakesAll_HotelStarves()
    {
        var r = await GoldenScenario.CalculateAsync("02-battery-small-300.json");
        var transit = SingleAllocation(r);

        // "Propulsion: I = min(300, 573.15) = 300 → J 105 / L 468.15 · Hotel gets nothing"
        var propulsion = Row(transit, BatteryLoadType.Propulsion);
        propulsion.BatteryUsedKw.Should().BeApproximately(300, Kw);
        propulsion.CoveredBandKw.Should().BeApproximately(105, Kw);
        propulsion.UncoveredReserveKw.Should().BeApproximately(468.15, Kw);

        var hotel = Row(transit, BatteryLoadType.Hotel);
        hotel.BatteryUsedKw.Should().Be(0);
        hotel.UncoveredReserveKw.Should().BeApproximately(76, Kw);

        // "Tiles: PS = 105 · SR = 544.15. Invariant: 105 + 544.15 = 649.15 — same swing as 01"
        r.BatteryDetails!.PeakShavingKw.Should().BeApproximately(105, Kw);
        r.BatteryDetails.SpinningReserveKw.Should().BeApproximately(544.15, Kw);
        (r.BatteryDetails.PeakShavingKw + r.BatteryDetails.SpinningReserveKw)
            .Should().BeApproximately(649.15, Kw);

        r.BaselineFOC.Should().BeApproximately(13392.5, TonPerYear);
        r.Advanced.OptimizedFOC.Should().BeApproximately(13371.2, TonPerYear);
        r.BatteryDetails.BenefitFocTonPerYear.Should().BeApproximately(88.74, 0.01);
    }

    // ─── 03 — No-battery reference world (proves the dual-scenario rule R3a) ────

    [Fact]
    public async Task Card03_NoBatteryReference_ReproducesBothBenefitsBySubtraction()
    {
        var reference = await GoldenScenario.CalculateAsync("03-no-battery-reference.json");

        // "No battery ⇒ baseline default = LAST row (2.7305 → 13 652.6), not 3rd-from-worst"
        reference.BatteryDetails.Should().BeNull();
        reference.Level1Details!.SelectedBaselineIndex
            .Should().Be(reference.Level1Details.ValidCombinationsCount - 1);
        reference.BaselineFOC.Should().BeApproximately(13652.6, TonPerYear);
        reference.Advanced.OptimizedFOC.Should().BeApproximately(13459.9, TonPerYear);

        // "13 459.9 − 13 286.2 (IL1 of 01) = 173.7 = test 01's green badge"
        var scenario01 = await GoldenScenario.CalculateAsync("01-excel-baseline.json");
        var scenario02 = await GoldenScenario.CalculateAsync("02-battery-small-300.json");

        (reference.Advanced.OptimizedFOC - scenario01.Advanced.OptimizedFOC)
            .Should().BeApproximately(scenario01.BatteryDetails!.BenefitFocTonPerYear, 0.01);
        (reference.Advanced.OptimizedFOC - scenario02.Advanced.OptimizedFOC)
            .Should().BeApproximately(scenario02.BatteryDetails!.BenefitFocTonPerYear, 0.01);
    }

    // ─── 04 — DP redundancy 400 kW (the RESERVE function) ──────────────────────

    [Fact]
    public async Task Card04_DpRedundancy_ReserveIsCoveredOneForOne()
    {
        var r = await GoldenScenario.CalculateAsync("04-dp-redundancy-reserve.json");
        var dp = r.BatteryDetails!.ModeAllocations.Single(m => m.Mode == OperationalMode.DP);

        // "DpReserve: H 400 (the FULL requirement) → J 400, L 0 — covered kW-for-kW"
        var reserve = Row(dp, BatteryLoadType.DpReserve);
        reserve.Function.Should().Be(BatteryFunction.Reserve);
        reserve.VariationKw.Should().BeApproximately(400, Kw);
        reserve.CoveredBandKw.Should().BeApproximately(400, Kw);
        reserve.UncoveredReserveKw.Should().Be(0);

        // "Hotel: 1 500×2 % = 30 → 1.5 / 28.5, budget left 70"
        var hotel = Row(dp, BatteryLoadType.Hotel);
        hotel.CoveredBandKw.Should().BeApproximately(1.5, Kw);
        hotel.UncoveredReserveKw.Should().BeApproximately(28.5, Kw);
        dp.RemainingBatteryKw.Should().BeApproximately(70, Kw);

        // "Tiles: SR = 28.5 · PS = 1.5 (PS counts peak-shaving rows only)"
        r.BatteryDetails.PeakShavingKw.Should().BeApproximately(1.5, Kw);
        r.BatteryDetails.SpinningReserveKw.Should().BeApproximately(28.5, Kw);

        // "Baseline total 14 534.8 · IL1 14 300.2 · savings 234.6 = $182 996"
        r.BaselineFOC.Should().BeApproximately(14534.8, TonPerYear);
        r.Advanced.OptimizedFOC.Should().BeApproximately(14300.2, TonPerYear);
        r.Advanced.FuelSavings.Should().BeApproximately(234.6, 0.05);
        r.Advanced.AnnualCostSavings.Should().BeApproximately(182996, Usd);

        // "Benefit = ΔFOC × 2 000 h = 139.9 t/yr = $109 113"
        r.BatteryDetails.BenefitFocTonPerYear.Should().BeApproximately(139.89, 0.01);
        r.BatteryDetails.BenefitCostPerYear.Should().BeApproximately(109113, Usd);
    }

    // ─── 05 — Mission crane 500 kW (cascade continues below) ───────────────────

    [Fact]
    public async Task Card05_MissionCrane500_TakesFullValueAndLandsOnTheHotelSide()
    {
        var r = await GoldenScenario.CalculateAsync("05-mission-crane-500.json");
        var transit = SingleAllocation(r);

        // "The crane's H is its FULL kW (Excel G7 = I3), covered at 50 %: 500 → J 250 / L 250"
        var mission = Row(transit, BatteryLoadType.Mission);
        mission.VariationKw.Should().BeApproximately(500, Kw);
        mission.CoveredBandKw.Should().BeApproximately(250, Kw);
        mission.UncoveredReserveKw.Should().BeApproximately(250, Kw);

        // "Everyone got paid" — the propulsion row is unchanged from card 01
        Row(transit, BatteryLoadType.Propulsion).CoveredBandKw.Should().BeApproximately(200.6025, Kw);
        transit.RemainingBatteryKw.Should().BeApproximately(110.85, Kw);

        r.BatteryDetails!.PeakShavingKw.Should().BeApproximately(454.4025, Kw);
        r.BatteryDetails.SpinningReserveKw.Should().BeApproximately(694.7475, Kw);

        // "Hotel' = 3 800+250+72.2 → AE 872 · ME identical to test 01: the ME never felt the crane"
        r.PowerDemands.AuxiliaryEnginePowerKw.Should().BeApproximately(872.2, 0.5);
        r.PowerDemands.MainEnginePowerKw.Should().BeApproximately(15085.5, 0.5);

        r.BaselineFOC.Should().BeApproximately(13590.7, TonPerYear);
        r.Advanced.OptimizedFOC.Should().BeApproximately(13554.1, TonPerYear);
        r.BatteryDetails.BenefitFocTonPerYear.Should().BeApproximately(422.25, 0.01);
    }

    // ─── 06 — Mission crane 3000 kW (budget devoured, plant reshuffles) ────────

    [Fact]
    public async Task Card06_MissionCrane3000_DrainsBudgetAndFlipsTheOptimalPlant()
    {
        var r = await GoldenScenario.CalculateAsync("06-mission-crane-3000.json");
        var transit = SingleAllocation(r);

        // "Mission takes ALL 1 260 → J 630 / L 2 370; propulsion and hotel starve"
        var mission = Row(transit, BatteryLoadType.Mission);
        mission.BatteryUsedKw.Should().BeApproximately(1260, Kw);
        mission.CoveredBandKw.Should().BeApproximately(630, Kw);
        mission.UncoveredReserveKw.Should().BeApproximately(2370, Kw);
        Row(transit, BatteryLoadType.Propulsion).BatteryUsedKw.Should().Be(0);
        Row(transit, BatteryLoadType.Hotel).BatteryUsedKw.Should().Be(0);

        r.BatteryDetails!.PeakShavingKw.Should().BeApproximately(630, Kw);
        r.BatteryDetails.SpinningReserveKw.Should().BeApproximately(3019.15, Kw);

        // "The optimizer picks 2 ME + both SGs + 0 AE — the row that was WORST in test 01"
        r.Level1Details!.ActiveMeCount.Should().Be(2);
        r.Level1Details.ActiveAeCount.Should().Be(0);
        r.PowerDemands.MainEnginePowerKw.Should().BeApproximately(18282.15, 0.5);
        r.PowerDemands.AuxiliaryEnginePowerKw.Should().Be(0);

        r.BaselineFOC.Should().BeApproximately(15890.0, TonPerYear);
        r.Advanced.OptimizedFOC.Should().BeApproximately(15547.3, TonPerYear);
        r.Advanced.FuelSavings.Should().BeApproximately(342.7, 0.05);

        // "Benefit = the ceiling for this battery (630 = 1 260 × 50 %) → 631.3 t/yr"
        r.BatteryDetails.BenefitFocTonPerYear.Should().BeApproximately(631.29, 0.01);

        // "Anti-double-counting (Q4/D4): variation 0, batteryShaved 500, L3 component 0"
        var l3 = r.Premium.Level3Details!;
        l3.VariationPerGeneratorKw.Should().Be(0);
        l3.BatteryShavedVariationKw.Should().BeApproximately(500, Kw);
        l3.DrcSavingsTonPerYear.Should().Be(0);
    }

    // ─── 07 — Multi-mode Transit + Port ────────────────────────────────────────

    [Fact]
    public async Task Card07_MultiMode_EachModeCascadesWithTheFullBudget()
    {
        var r = await GoldenScenario.CalculateAsync("07-multimode-transit-port.json");

        // "Modes never overlap in time, so each cascade starts with the whole 1260"
        r.BatteryDetails!.ModeAllocations.Should().HaveCount(2);
        var transit = r.BatteryDetails.ModeAllocations.Single(m => m.Mode == OperationalMode.Transit);
        var port = r.BatteryDetails.ModeAllocations.Single(m => m.Mode == OperationalMode.Port);

        // "Port table: a single row — H = 500×2 % = 10 → J 0.5 / L 9.5; 1 250 left unused"
        port.Loads.Should().ContainSingle();
        var portHotel = Row(port, BatteryLoadType.Hotel);
        portHotel.VariationKw.Should().BeApproximately(10, Kw);
        portHotel.CoveredBandKw.Should().BeApproximately(0.5, Kw);
        portHotel.UncoveredReserveKw.Should().BeApproximately(9.5, Kw);
        port.RemainingBatteryKw.Should().BeApproximately(1250, Kw);
        transit.PeakShavingBandKw.Should().BeApproximately(204.4025, Kw);

        // "Tiles are SUMS across modes: SR = 444.7+9.5 · PS = 204.4+0.5"
        r.BatteryDetails.PeakShavingKw.Should().BeApproximately(204.9025, Kw);
        r.BatteryDetails.SpinningReserveKw.Should().BeApproximately(454.2475, Kw);

        // "Transit 173.66 + Port ≈ 0.09 = 173.74 t/yr"
        r.BatteryDetails.BenefitFocTonPerYear.Should().BeApproximately(173.74, 0.01);
        r.BaselineFOC.Should().BeApproximately(13396.6, TonPerYear);
        r.Advanced.OptimizedFOC.Should().BeApproximately(13375.4, TonPerYear);
    }

    // ─── 08 / 09 — the two sides of the battery PTI gate ───────────────────────

    [Fact]
    public async Task Card08_PtiWideOpen_ChangesNothing()
    {
        var withPti = await GoldenScenario.CalculateAsync("08-pti-gate-pass.json");
        var withoutPti = await GoldenScenario.CalculateAsync("01-excel-baseline.json");

        // "Identical to test 01 in every number. The gate is a guard, not a feature."
        withPti.Level1Details!.ValidCombinationsCount.Should().Be(5);
        withPti.BatteryDetails!.PeakShavingKw.Should().BeApproximately(withoutPti.BatteryDetails!.PeakShavingKw, 1e-9);
        withPti.BatteryDetails.SpinningReserveKw.Should().BeApproximately(withoutPti.BatteryDetails.SpinningReserveKw, 1e-9);
        withPti.BaselineFOC.Should().BeApproximately(withoutPti.BaselineFOC, 1e-9);
        withPti.Advanced.OptimizedFOC.Should().BeApproximately(withoutPti.Advanced.OptimizedFOC, 1e-9);
        withPti.BatteryDetails.BenefitFocTonPerYear
            .Should().BeApproximately(withoutPti.BatteryDetails.BenefitFocTonPerYear, 1e-9);
    }

    [Fact]
    public async Task Card09_PtiTooSmall_Answers400WithTheActualNumbers()
    {
        var response = await GoldenScenario.RunAsync("09-pti-gate-fail.json");

        // "100 < 200.6 → every combination rejected → HTTP 400 with the QA-C-1 message"
        response.Status.Should().Be(400);
        response.Result.Should().BeNull();
        var message = response.Errors.Should().ContainSingle().Subject;
        message.Should().Contain("200.6 kW of PTI capacity")
            .And.Contain("only 100 kW is available")
            .And.Contain("currently 50 kW");
    }

    [Fact]
    public async Task Card09_PtiThreshold_Is101PerEngine()
    {
        // "Manual boundary: 100/engine still fails, 101/engine passes (2×101 = 202 ≥ 200.6)"
        var input = GoldenScenario.LoadInput("01-excel-baseline.json");

        input.MaxPtiPerEngineKw = 100;
        (await GoldenScenarioHost.Instance.RunAsync(input)).Status.Should().Be(400);

        input.MaxPtiPerEngineKw = 101;
        (await GoldenScenarioHost.Instance.RunAsync(input)).Status.Should().Be(200);
    }

    // ─── 10 — capacity plausibility warning ────────────────────────────────────

    [Fact]
    public async Task Card10_UndersizedCapacity_WarnsButStillCalculates()
    {
        var response = await GoldenScenario.RunAsync("10-battery-capacity-warning.json");

        // "A warning advises; it never blocks"
        response.Status.Should().Be(200);
        var r = response.Result!;
        r.Warnings.Should().ContainSingle()
            .Which.Message.Should().Contain("cannot sustain the configured power for 30 minutes");

        // "Beyond saturation extra power buys nothing (INV-2): identical tiles to card 01, 350.85 left"
        r.BatteryDetails!.PeakShavingKw.Should().BeApproximately(204.4025, Kw);
        r.BatteryDetails.SpinningReserveKw.Should().BeApproximately(444.7475, Kw);
        SingleAllocation(r).RemainingBatteryKw.Should().BeApproximately(350.85, Kw);
        r.Advanced.OptimizedFOC.Should().BeApproximately(13286.2, TonPerYear);
    }

    [Fact]
    public async Task Card10_CapacityKwh_ParticipatesInNoCalculation()
    {
        // "kWh participates in NO calculation (D4/Q1). Change 400 → 5 000 and nothing moves."
        var input = GoldenScenario.LoadInput("10-battery-capacity-warning.json");
        var small = (await GoldenScenarioHost.Instance.RunAsync(input)).Result!;

        input.Battery!.CapacityKwh = 5000;
        var large = (await GoldenScenarioHost.Instance.RunAsync(input)).Result!;

        large.BaselineFOC.Should().Be(small.BaselineFOC);
        large.Advanced.OptimizedFOC.Should().Be(small.Advanced.OptimizedFOC);
        large.BatteryDetails!.BenefitFocTonPerYear.Should().Be(small.BatteryDetails!.BenefitFocTonPerYear);
        large.Warnings.Should().BeEmpty("a 5 000 kWh pack sustains 1 000 kW well past 30 minutes");
    }

    // ─── 11 — OSV, all five modes ──────────────────────────────────────────────

    [Fact]
    public async Task Card11_OsvFiveModes_ClampedBaselineAndDrcAsTheOnlyEarner()
    {
        var r = await GoldenScenario.CalculateAsync("11-osv-parametric-full.json");

        // "Propulsion H = 1 725×5 % = 86.25 → J 30.2 / L 56.1 · Hotel 4.4 → 0.22 / 4.18"
        var transit = SingleAllocation(r);
        Row(transit, BatteryLoadType.Propulsion).VariationKw.Should().BeApproximately(86.25, Kw);
        r.BatteryDetails!.PeakShavingKw.Should().BeApproximately(30.4075, Kw);
        r.BatteryDetails.SpinningReserveKw.Should().BeApproximately(60.2425, Kw);

        // "Transit has only 2 valid combos ⇒ the 3rd-from-worst rule clamps to index 0"
        r.Level1Details!.ValidCombinationsCount.Should().Be(2);
        r.Level1Details.SelectedBaselineIndex.Should().Be(0);
        r.Level1Details.OptimalFocTonPerHour.Should().BeApproximately(0.35048, 1e-5);

        // "AE = 0 for all 8 760 h (SG-forced rule — observation #1 at its clearest)"
        r.PowerDemands.AuxiliaryEnginePowerKw.Should().Be(0);
        r.PowerDemands.MainEnginePowerKw.Should().BeApproximately(2012, 1.0);
        r.PowerDemands.ModeBreakdowns.Should().HaveCount(5);

        // "Baseline 3 081.6 → savings 4.1 t/yr; IL3 = 36.3 chip, of which DRC 32.2"
        r.BaselineFOC.Should().BeApproximately(3081.6, TonPerYear);
        r.Advanced.OptimizedFOC.Should().BeApproximately(3077.5, TonPerYear);
        r.Advanced.FuelSavings.Should().BeApproximately(4.06, 0.01);
        r.Pro.Level2SavingsTonPerYear.Should().Be(0);
        r.Premium.Level3SavingsTonPerYear.Should().BeApproximately(32.2, 0.05);
        r.Premium.FuelSavings.Should().BeApproximately(36.26, 0.01);

        // "DRC: variation ±500 → −battery 0.22 → ×0.8 → ±400"
        r.Premium.Level3Details!.ReducedVariationPerGeneratorKw.Should().BeApproximately(399.824, 1e-3);

        r.BatteryDetails.BenefitFocTonPerYear.Should().BeApproximately(21.2, 0.05);
    }

    // ─── 12 — sail on ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Card12_SailOn_ShrinksTheCascade()
    {
        var r = await GoldenScenario.CalculateAsync("12-sail-transit.json");

        // "sail thrust = 539.93 kW → propulsion 11 463 − 540 = 10 923"
        r.SailContribution!.SailPowerKw.Should().BeApproximately(539.93, 0.01);
        r.SailContribution.TransitPropulsionAfterKw.Should().BeApproximately(10923.07, 0.01);

        // "The cascade uses the ADJUSTED value: H = 546.2 → J 191.2 / L 355.0"
        var propulsion = Row(SingleAllocation(r), BatteryLoadType.Propulsion);
        propulsion.VariationKw.Should().BeApproximately(546.15, 0.01);
        propulsion.CoveredBandKw.Should().BeApproximately(191.15, 0.01);

        // "Tiles: PS = 195 · SR = 427.2 — the wind literally shrinks the swing"
        r.BatteryDetails!.PeakShavingKw.Should().BeApproximately(194.95, 0.01);
        r.BatteryDetails.SpinningReserveKw.Should().BeApproximately(427.2, 0.01);

        // "Canonical backend values at $780: 165.5 t/yr = $129 105"
        r.BatteryDetails.BenefitFocTonPerYear.Should().BeApproximately(165.52, 0.01);
        r.BatteryDetails.BenefitCostPerYear.Should().BeApproximately(129105, Usd);
        r.BaselineFOC.Should().BeApproximately(12836.6, TonPerYear);
        r.Advanced.OptimizedFOC.Should().BeApproximately(12815.4, TonPerYear);
    }

    // ─── 13 — LNG main fuel, per-fuel CO2 split (D6 fix #3) ───────────────────

    [Fact]
    public async Task Card13_TwoFuels_PerEngineCo2UsesItsOwnFactorAndSumsToTheTotal()
    {
        var r = await GoldenScenario.CalculateAsync("13-lng-fuel-co2.json");

        // "The cascade knows nothing about engines or fuels" — identical tiles to card 01
        r.BatteryDetails!.PeakShavingKw.Should().BeApproximately(204.4025, Kw);
        r.BatteryDetails.SpinningReserveKw.Should().BeApproximately(444.7475, Kw);

        // "Hotel 3 872.2 = SG 2 800 (the smaller Dual-Fuel SG) + AE 1 072"
        r.PowerDemands.ShaftGeneratorPowerKw.Should().BeApproximately(2800, 0.5);
        r.PowerDemands.AuxiliaryEnginePowerKw.Should().BeApproximately(1072.2, 0.5);

        // "Baseline 13 432.4 = ME 12 219.3 + AE 1 213.2"
        r.BaselineFOC.Should().BeApproximately(13432.4, TonPerYear);
        r.BaselineME.Should().BeApproximately(12219.3, 0.1);
        r.BaselineAE.Should().BeApproximately(1213.2, 0.1);

        // "ME × 2.753 (LNG) = 33 639.6 · AE × 3.93267 (MGO) = 4 771.1 → total 38 410.7"
        r.BaselineMeCO2.Should().BeApproximately(r.BaselineME * 2.753, 1e-6);
        r.BaselineAeCO2.Should().BeApproximately(r.BaselineAE * 3.93267, 1e-6);
        r.BaselineMeCO2.Should().BeApproximately(33639.6, 0.1);
        r.BaselineAeCO2.Should().BeApproximately(4771.1, 0.1);
        r.BaselineCO2.Should().BeApproximately(38410.7, 0.1);

        // "IL1 13 389.0 (savings 43.4) · CO2 38 239.8 = ME 33 639.6 + AE 4 600.2"
        r.Advanced.OptimizedFOC.Should().BeApproximately(13389.0, TonPerYear);
        r.Advanced.FuelSavings.Should().BeApproximately(43.4, 0.05);
        r.Advanced.OptimizedMeCO2.Should().BeApproximately(33639.6, 0.1);
        r.Advanced.OptimizedAeCO2.Should().BeApproximately(4600.2, 0.1);
        (r.Advanced.OptimizedMeCO2 + r.Advanced.OptimizedAeCO2)
            .Should().BeApproximately(r.Advanced.OptimizedCO2, 1e-6);

        // "2 ME+SG is now the SECOND row (2.6831), not the last"
        r.Level1Details!.ValidCombinations[1].ActiveMeCount.Should().Be(2);
        r.Level1Details.ValidCombinations[1].FocTonPerHour.Should().BeApproximately(2.6831, 1e-4);
        r.BatteryDetails.BenefitFocTonPerYear.Should().BeApproximately(173.41, 0.01);
    }

    // ─── 14 — Bulk carrier, L3 variation from the vessel-type lookup ───────────

    [Fact]
    public async Task Card14_EmptyVariationField_FallsBackToTheVesselTypeLookup()
    {
        var input = GoldenScenario.LoadInput("14-bulk-l3-lookup.json");
        input.HotelLoadVariationKw.Should().BeNull("the scenario's point is an EMPTY variation field");

        var r = await GoldenScenario.CalculateAsync("14-bulk-l3-lookup.json");

        // "'Bulk Carrier…' → appsettings VesselVariations → ±250; DRC reduction 20 % → 200"
        var l3 = r.Premium.Level3Details!;
        l3.VariationPerGeneratorKw.Should().BeApproximately(250, Kw);
        l3.ReducedVariationPerGeneratorKw.Should().BeApproximately(200, Kw);

        // "One valid combination ⇒ baseline = optimal ⇒ savings 0 t / $0 against a $110k investment"
        r.Level1Details!.ValidCombinationsCount.Should().Be(1);
        r.BaselineFOC.Should().BeApproximately(1883.4, TonPerYear);
        r.Advanced.OptimizedFOC.Should().BeApproximately(1883.4, TonPerYear);
        r.Advanced.FuelSavings.Should().Be(0);
        r.Advanced.AnnualCostSavings.Should().Be(0);
    }

    // ─── 15 — user-picked baseline (rule D1) ──────────────────────────────────

    [Fact]
    public async Task Card15_PinnedBaseline_MovesSavingsButNeverTheBatteryBenefit()
    {
        var pinned = await GoldenScenario.CalculateAsync("15-baseline-user-pick.json");
        var defaulted = await GoldenScenario.CalculateAsync("01-excel-baseline.json");

        // "baselineIndex: 4 pins the WORST row (2 ME + SG, 2.6975)"
        pinned.Level1Details!.SelectedBaselineIndex.Should().Be(4);
        pinned.Level1Details.BaselineMeCount.Should().Be(2);
        pinned.Level1Details.BaselineAeCount.Should().Be(0);

        // "Baseline 13 487.5 · savings 201.25 t/yr = $156 976 (vs 21.2 on the default row)"
        pinned.BaselineFOC.Should().BeApproximately(13487.5, TonPerYear);
        pinned.Advanced.FuelSavings.Should().BeApproximately(201.25, 0.01);
        pinned.Advanced.AnnualCostSavings.Should().BeApproximately(156976, Usd);

        // "Battery Benefit = 173.7 — UNCHANGED. The invariant this test pins."
        pinned.BatteryDetails!.BenefitFocTonPerYear
            .Should().BeApproximately(defaulted.BatteryDetails!.BenefitFocTonPerYear, 1e-9);
        pinned.Advanced.OptimizedFOC.Should().BeApproximately(defaulted.Advanced.OptimizedFOC, 1e-9);
    }

    // ─── 16 — sea margin 15 % ─────────────────────────────────────────────────

    [Fact]
    public async Task Card16_SeaMargin_InflatesTheCascadeAndTheBatteryValue()
    {
        var r = await GoldenScenario.CalculateAsync("16-sea-margin-15.json");

        // "11 463 × 1.15 = 13 182.45 → H = 659.1 → J 230.7 / L 428.4"
        var propulsion = Row(SingleAllocation(r), BatteryLoadType.Propulsion);
        propulsion.AverageLoadKw.Should().BeApproximately(13182.45, 0.01);
        propulsion.VariationKw.Should().BeApproximately(659.12, 0.01);
        propulsion.CoveredBandKw.Should().BeApproximately(230.69, 0.01);

        // "Tiles: PS = 234.5 · SR = 500.6 · ME header 16 861"
        r.BatteryDetails!.PeakShavingKw.Should().BeApproximately(234.49, 0.01);
        r.BatteryDetails.SpinningReserveKw.Should().BeApproximately(500.63, 0.01);
        r.PowerDemands.MainEnginePowerKw.Should().BeApproximately(16861, 1.0);

        // "Rougher sea makes the battery MORE valuable: 199.7 t/yr = $155 742"
        r.BatteryDetails.BenefitFocTonPerYear.Should().BeApproximately(199.67, 0.01);
        r.BatteryDetails.BenefitCostPerYear.Should().BeApproximately(155742, Usd);
        r.BaselineFOC.Should().BeApproximately(14809.7, TonPerYear);
        r.Advanced.OptimizedFOC.Should().BeApproximately(14788.5, TonPerYear);
    }

    // ─── 17 — infeasible plant (fails earlier than 09) ────────────────────────

    [Fact]
    public async Task Card17_UndersizedMainEngines_AreCaughtByInputValidation()
    {
        var response = await GoldenScenario.RunAsync("17-infeasible-plant.json");

        // "INPUT VALIDATION promotes the capacity warning to an error, before Level 1 even starts"
        response.Status.Should().Be(400);
        response.Result.Should().BeNull();
        response.Errors.Should().Contain(e => e.StartsWith("Main engine utilization > 100%"));
        response.Errors.Should().NotContain(e => e.Contains("PTI"),
            "this plant fails on capacity, not on the battery gate (that is card 09)");
    }

    // ─── 18 — battery configured but its mode has no hours ────────────────────

    [Fact]
    public async Task Card18_BatteryWithoutHours_StepsAsideWithoutATrace()
    {
        var r = await GoldenScenario.CalculateAsync("18-battery-zero-hours.json");

        // "The Battery Contribution panel is entirely absent — not an empty box: no box"
        r.BatteryDetails.Should().BeNull();

        // "Every number equals the PURE no-battery calculation of this vessel"
        r.PowerDemands.MainEnginePowerKw.Should().BeApproximately(14713, 0.5);
        r.PowerDemands.AuxiliaryEnginePowerKw.Should().BeApproximately(550, 0.5);

        // "Baseline rule flip: battery inactive ⇒ default = LAST row (2.6255) = 13 127.3"
        r.Level1Details!.SelectedBaselineIndex
            .Should().Be(r.Level1Details.ValidCombinationsCount - 1);
        r.BaselineFOC.Should().BeApproximately(13127.3, TonPerYear);
        r.Advanced.OptimizedFOC.Should().BeApproximately(12892.7, TonPerYear);
        r.Advanced.FuelSavings.Should().BeApproximately(234.6, 0.05);

        // "IL3: variation ±500 → ±400 (nothing shaved by a battery that never runs)"
        var l3 = r.Premium.Level3Details!;
        l3.VariationPerGeneratorKw.Should().BeApproximately(500, Kw);
        l3.ReducedVariationPerGeneratorKw.Should().BeApproximately(400, Kw);
        l3.BatteryShavedVariationKw.Should().Be(0);
    }

    // ─── the recipe's invariant, on every scenario that has a battery ─────────

    [Theory]
    [MemberData(nameof(GoldenMasterTests.Scenarios), MemberType = typeof(GoldenMasterTests))]
    public async Task Recipe_PeakShavingPlusSpinningReserve_EqualsTheTotalSwing(string scenarioFile)
    {
        // calculations/README.md, step 1: "Invariant: ΣJ + ΣL = ΣH — the sea sets the total swing;
        // the battery only moves the split."
        var response = await GoldenScenario.RunAsync(scenarioFile);
        if (response.Status != 200 || response.Result?.BatteryDetails is null)
            return; // error scenarios and inactive batteries have no cascade to check

        foreach (var mode in response.Result.BatteryDetails.ModeAllocations)
        {
            var shaving = mode.Loads.Where(l => l.Function == BatteryFunction.PeakShaving).ToList();
            var swing = shaving.Sum(l => l.VariationKw);

            (shaving.Sum(l => l.CoveredBandKw) + shaving.Sum(l => l.UncoveredReserveKw))
                .Should().BeApproximately(swing, 1e-9, $"{scenarioFile} [{mode.Mode}] ΣJ + ΣL must equal ΣH");

            mode.PeakShavingBandKw.Should().BeApproximately(shaving.Sum(l => l.CoveredBandKw), 1e-9,
                "the Peak Shaving tile counts peak-shaving rows only");
            mode.AdditionalSpinningReserveKw.Should().BeApproximately(mode.Loads.Sum(l => l.UncoveredReserveKw), 1e-9,
                "the Spinning Reserve tile carries every row's uncovered part, reserve rows included");
        }
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static BatteryModeAllocation SingleAllocation(AllVariantsCalculationResult result)
        => result.BatteryDetails!.ModeAllocations.Should().ContainSingle().Subject;

    private static BatteryLoadAllocation Row(BatteryModeAllocation allocation, BatteryLoadType load)
        => allocation.Loads.Should().ContainSingle(l => l.Load == load).Subject;
}
