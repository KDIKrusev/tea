using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Tests.TestHelpers;

namespace KSailCalc.Tests.Calculation;

/// <summary>
/// Story: Battery Increment C — PTI propulsion assist and battery discharge gate.
/// PTI is opt-in: MaxPtiPerEngineKw = 0 (default) must reproduce pre-PTI behaviour exactly.
/// </summary>
public class Level1PtiTests
{
    private const double Precision = 1e-6;

    /// <summary>
    /// Plant with a deliberate ME deficit in Transit: 2×5000 ME, SG 2×500 fully used by hotel,
    /// propulsion 9200 ⇒ ME power needed = 9200 + 1000 = 10200 > 10000 capacity (deficit 200).
    /// AE 3×800 carries hotel remainder 1000 and has headroom for the PTI load.
    /// </summary>
    private static CalculatorInputBuilder DeficitPlant() => CalculatorInputBuilder.Default()
        .WithMainEngines(5000, 2)
        .WithShaftGenerators(500)
        .WithAuxiliaryEngines(800, 3)
        .WithPropulsionPower(9200)
        .WithSeaMargin(0)
        .WithTransitMode(5000, 2000);

    // ── AC1: zero regression with MaxPti = 0 ─────────────────────────────────

    [Fact]
    public async Task Level1_MaxPtiZero_IsIdenticalToPrePtiBehaviour()
    {
        var factory = TestServiceFactory.Create();
        var input = CalculatorInputBuilder.Default().Build();
        input.MaxPtiPerEngineKw = 0;

        var result = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit);

        // NOTE: the default plant yields exactly ONE valid combination, so OnlyContain below is a
        // single-item check. The count pins the set; the optimum assertion below carries the rest.
        result.AllValidCombinations.Should().HaveCount(1);
        result.AllValidCombinations.Should().OnlyContain(c => c.PtiPowerKw == 0);
        // Same optimum as the untouched suite's expectations (2 ME + SG, no AE for this plant)
        result.OptimalCombination.MePowerKw.Should().BeApproximately(
            input.EffectivePropulsionPower + result.OptimalCombination.SgPowerKw, Precision);
    }

    [Fact]
    public async Task Level1_DeficitPlant_WithoutPti_HasNoFullMeCombination()
    {
        var factory = TestServiceFactory.Create();
        var input = DeficitPlant().Build(); // MaxPti defaults to 0

        // 2-ME combos are infeasible (deficit 200); plant survives only if some other combo works —
        // here no combination can carry the load, so Level 1 must throw.
        var act = () => factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit);

        act.Should().Throw<NoValidCombinationException>()
            .WithMessage("*No valid engine combinations*");
    }

    // ── AC2: PTI enables the combo, aux side carries the loss-grossed load ───

    [Fact]
    public async Task Level1_DeficitPlant_WithPti_EnablesCombination_AndChargesAuxWithLosses()
    {
        var factory = TestServiceFactory.Create();
        var input = DeficitPlant().Build();
        input.MaxPtiPerEngineKw = 500; // capacity 2×500 = 1000 ≥ deficit 200

        var result = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit);

        var ptiCombo = result.AllValidCombinations.Single(
            c => c.ActiveMeCount == 2 && c.PtiPowerKw > 0 && c.ActiveAeCount == 3);
        ptiCombo.PtiPowerKw.Should().BeApproximately(200, Precision);           // the deficit
        ptiCombo.MePowerKw.Should().BeApproximately(10000, Precision);          // pinned to capacity
        ptiCombo.AvailablePtiKw.Should().BeApproximately(800, Precision);       // 1000 − 200
        // AE: hotel remainder (2000 − 1000 SG) + PTI 200 × 1.05 = 1000 + 210 = 1210
        ptiCombo.AePowerKw.Should().BeApproximately(1210, Precision);
        ptiCombo.AeLoadPercent.Should().BeApproximately(1210.0 / 2400.0, Precision);
    }

    // ── AC3: deficit beyond PTI capacity stays invalid ───────────────────────

    [Fact]
    public async Task Level1_DeficitBeyondPtiCapacity_StaysInvalid()
    {
        var factory = TestServiceFactory.Create();
        var input = DeficitPlant()
            .WithPropulsionPower(11500) // deficit = 11500 + 1000 − 10000 = 2500 > PTI 1000
            .Build();
        input.MaxPtiPerEngineKw = 500;

        var act = () => factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit);

        act.Should().Throw<NoValidCombinationException>()
            .WithMessage("*No valid engine combinations*");
    }

    // ── AC4: battery discharge gate ("Insufficient PTI") ─────────────────────

    [Fact]
    public async Task Level1_BatteryPropulsionBand_ExceedingPtiHeadroom_ExcludesCombination()
    {
        var factory = TestServiceFactory.Create();
        var input = DeficitPlant().Build();
        input.MaxPtiPerEngineKw = 500; // headroom after assist = 800

        // Band 900 > headroom 800 ⇒ the PTI combo must be gated out ⇒ nothing remains
        var adjustment = new BatteryL1Adjustment(0, 0, PropulsionPeakShavingKw: 900);
        var act = () => factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit, batteryAdjustment: adjustment);

        act.Should().Throw<NoValidCombinationException>()
            .WithMessage("*No valid engine combinations*");
    }

    [Fact]
    public async Task Level1_BatteryPropulsionBand_WithinPtiHeadroom_KeepsCombination()
    {
        var factory = TestServiceFactory.Create();
        var input = DeficitPlant().Build();
        input.MaxPtiPerEngineKw = 500;

        var adjustment = new BatteryL1Adjustment(0, 0, PropulsionPeakShavingKw: 700); // ≤ 800
        var result = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit, batteryAdjustment: adjustment);

        result.AllValidCombinations.Should().Contain(c => c.PtiPowerKw > 0);
    }

    [Fact]
    public async Task Level1_BatteryBand_WithoutPtiConfigured_IsNotGated_BusLevelSimplification()
    {
        var factory = TestServiceFactory.Create();
        var input = CalculatorInputBuilder.Default().Build(); // MaxPti = 0

        // Increment B behaviour: band present but PTI not modelled → no gate (ADR-5 opt-in)
        var adjustment = new BatteryL1Adjustment(0, 0, PropulsionPeakShavingKw: 5000);
        var result = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit, batteryAdjustment: adjustment);

        result.AllValidCombinations.Should().NotBeEmpty();
    }

    // ── AC5: aux headroom limits the assist ──────────────────────────────────

    [Fact]
    public async Task Level1_PtiAuxLoad_BeyondAeCapacity_StaysInvalid()
    {
        var factory = TestServiceFactory.Create();
        // AE 2×800: hotel remainder 1000 leaves 600 headroom; PTI needs 200×1.05 = 210... make it tighter:
        // deficit 700 → aux PTI load 735 > headroom 600 ⇒ invalid
        var input = DeficitPlant()
            .WithAuxiliaryEngines(800, 2)
            .WithPropulsionPower(9700) // deficit = 9700 + 1000 − 10000 = 700 ≤ PTI 1000
            .Build();
        input.MaxPtiPerEngineKw = 500;

        var act = () => factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit);

        act.Should().Throw<NoValidCombinationException>()
            .WithMessage("*No valid engine combinations*");
    }

    // ── End-to-end wiring: propulsion band from the allocation reaches the gate ──
    // (QA addition: ToAdjustment's PropulsionPeakShavingKw computation was otherwise untested)

    [Fact]
    public async Task Calculate_ExcelBatteryWithAmplePti_Succeeds_GateHeadroomSufficient()
    {
        var factory = TestServiceFactory.Create();
        // Excel scenario: propulsion-side covered band = 200.6025 kW; PTI 2×500 = 1000 ≥ band
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(24000, 2)
            .WithShaftGenerators(1000)
            .WithPropulsionPower(11463)
            .WithSeaMargin(0)
            .WithTransitMode(5000, 3800)
            .WithBattery(1260, 2000, OperationalMode.Transit)
            .Build();
        input.MaxPtiPerEngineKw = 500;

        var result = await factory.CalculatorService.CalculateAllVariantsAsync(input);

        result.BatteryDetails.Should().NotBeNull();
        result.BatteryDetails!.PeakShavingKw.Should().BeApproximately(204.4025, Precision);
    }

    [Fact]
    public async Task Calculate_ExcelBatteryWithTinyPti_AllCombinationsGated_Throws()
    {
        var factory = TestServiceFactory.Create();
        // PTI 2×50 = 100 kW < propulsion band 200.6 kW ⇒ every combination fails the
        // discharge gate ("Insufficient PTI") and Level 1 finds nothing
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(24000, 2)
            .WithShaftGenerators(1000)
            .WithPropulsionPower(11463)
            .WithSeaMargin(0)
            .WithTransitMode(5000, 3800)
            .WithBattery(1260, 2000, OperationalMode.Transit)
            .Build();
        input.MaxPtiPerEngineKw = 50;

        var act = () => factory.CalculatorService.CalculateAllVariantsAsync(input);

        await act.Should().ThrowAsync<NoValidCombinationException>()
            .WithMessage("*No valid engine combinations*");
    }

    // ── Increment G: PTI donors = installed machines (Excel row-59 analog) ───

    [Fact]
    public async Task G_IdleMachinePti_EnablesExcelRow59StyleCombination()
    {
        var factory = TestServiceFactory.Create();
        // 1-of-2 engines running: deficit 700 needs BOTH machines' PTI (2×500);
        // the active-only cap (500) used to reject this — the Excel's idle machine donates
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(5000, 2)
            .WithShaftGenerators(500)
            .WithAuxiliaryEngines(1000, 3)
            .WithPropulsionPower(5200).WithSeaMargin(0)
            .WithTransitMode(5000, 2000)
            .Build();
        input.MaxPtiPerEngineKw = 500;

        var result = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit);

        var oneMe = result.AllValidCombinations.Single(
            c => c.ActiveMeCount == 1 && c.PtiPowerKw > 0 && c.ActiveAeCount == 3);
        oneMe.PtiPowerKw.Should().BeApproximately(700, Precision);       // deficit 5700 − 5000
        oneMe.AvailablePtiKw.Should().BeApproximately(300, Precision);   // 1000 − 700
        oneMe.MePowerKw.Should().BeApproximately(5000, Precision);       // pinned to capacity
        oneMe.AePowerKw.Should().BeApproximately(2235, Precision);       // 1500 + 700×1.05
    }

    // ── QA-C-1: the infeasibility reason must be actionable ──────────────────

    [Fact]
    public async Task NoValidCombination_BatteryPtiGate_ExplainsRequiredVsAvailablePti()
    {
        var factory = TestServiceFactory.Create();
        var input = DeficitPlant().Build();
        input.MaxPtiPerEngineKw = 500; // headroom after the 200 kW assist = 800

        var act = () => factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit,
            batteryAdjustment: new BatteryL1Adjustment(0, 0, PropulsionPeakShavingKw: 900));

        var ex = act.Should().Throw<NoValidCombinationException>().Which;
        ex.Mode.Should().Be(OperationalMode.Transit);
        ex.UserMessage.Should().Contain("900");            // required band
        ex.UserMessage.Should().Contain("800");            // best available PTI headroom
        ex.UserMessage.Should().Contain("PTI capacity");   // which field to change
        ex.UserMessage.Should().Contain("battery power");  // the alternative fix
    }

    [Fact]
    public async Task NoValidCombination_WithoutBattery_ExplainsEngineCapacity()
    {
        var factory = TestServiceFactory.Create();
        var input = DeficitPlant().Build(); // deficit 200, no PTI, no battery

        var act = () => factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit);

        var ex = act.Should().Throw<NoValidCombinationException>().Which;
        ex.UserMessage.Should().NotContain("PTI capacity to shave"); // not a battery problem
        ex.UserMessage.Should().Contain("Transit");
    }

    // ── Validation ───────────────────────────────────────────────────────────

    [Fact]
    public void Validate_NegativeMaxPti_ProducesError()
    {
        var service = new ValidationService();
        var input = CalculatorInputBuilder.Default().Build();
        input.MaxPtiPerEngineKw = -1;

        var result = service.ValidateInput(input);

        result.Valid.Should().BeFalse();
        result.Errors.Should().Contain("PTI capacity per engine cannot be negative");
    }
}
