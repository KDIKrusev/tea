using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Tests.TestHelpers;

namespace KSailCalc.Tests.Calculation;

/// <summary>
/// Characterization tests for the infeasibility diagnostics (QA-C-1).
///
/// `RejectionTally.ExplainFor` picks ONE sentence based on which rejection counter fired, in a fixed
/// precedence: battery PTI gate → engine capacity → aux overload → structural fallback. That
/// precedence is load-bearing — it decides what the user is told to change — and it is invisible to
/// the golden snapshots except on the few infeasible scenarios.
///
/// These tests assert the EXACT message for each branch, so a refactor of the candidate loop cannot
/// silently reorder the rejection reasons. Written against the pre-refactor code (story R-C, Task 1)
/// before the loop was touched.
/// </summary>
public class Level1RejectionDiagnosticsTests
{
    /// <summary>
    /// Plant with a deliberate ME deficit in Transit: 2×5000 ME, SG 2×500 fully used by hotel,
    /// propulsion 9200 ⇒ ME power needed = 10200 > 10000 capacity (deficit 200).
    /// </summary>
    private static CalculatorInput DeficitPlant() => CalculatorInputBuilder.Default()
        .WithMainEngines(5000, 2)
        .WithShaftGenerators(500)
        .WithAuxiliaryEngines(800, 3)
        .WithPropulsionPower(9200)
        .WithSeaMargin(0)
        .WithTransitMode(5000, 2000)
        .Build();

    // ── Branch 1: battery PTI discharge gate ────────────────────────────────────

    [Fact]
    public async Task BatteryPtiGate_ExplainsRequiredVersusAvailablePti()
    {
        var factory = TestServiceFactory.Create();
        var input = DeficitPlant();
        input.MaxPtiPerEngineKw = 500; // headroom after the 200 kW assist = 800

        var act = () => factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit,
            batteryAdjustment: new BatteryL1Adjustment(0, 0, PropulsionPeakShavingKw: 900));

        var ex = act.Should().Throw<NoValidCombinationException>().Which;

        ex.Mode.Should().Be(OperationalMode.Transit);
        ex.UserMessage.Should().Be(
            "the battery needs 900 kW of PTI capacity to shave propulsion peaks in Transit mode, " +
            "but only 800 kW is available. Increase the PTI capacity per main engine " +
            "(currently 500 kW), reduce the battery power, or clear the PTI field to model the " +
            "battery at switchboard level only.");
    }

    // ── Branch 2: engines cannot carry the demand ───────────────────────────────

    [Fact]
    public async Task InsufficientEnginePower_ExplainsEngineCapacity()
    {
        var factory = TestServiceFactory.Create();

        var act = () => factory.Level1Service.FindOptimalCombination(DeficitPlant(), factory.Curves, OperationalMode.Transit);

        var ex = act.Should().Throw<NoValidCombinationException>().Which;

        ex.UserMessage.Should().Be(
            "the installed engines cannot carry the Transit demand. Increase engine capacity or " +
            "engine count, or reduce the propulsion/hotel power for this mode.");
    }

    // ── Branch 3: aux engines above 90 % ────────────────────────────────────────

    [Fact]
    public async Task AuxOverloaded_ExplainsTheNinetyPercentCeiling()
    {
        // ME carries propulsion comfortably (4000 of 2×5000, no SG), so no capacity rejection fires.
        // Hotel 950 against AE 2×500: the only combination that covers the hotel runs the AEs at
        // 95 % ⇒ the sole non-structural rejection is the aux overload.
        var factory = TestServiceFactory.Create();
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(5000, 2)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(500, 2)
            .WithPropulsionPower(4000)
            .WithSeaMargin(0)
            .WithTransitMode(5000, 950)
            .Build();

        var act = () => factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit);

        var ex = act.Should().Throw<NoValidCombinationException>().Which;

        ex.UserMessage.Should().Be(
            "the auxiliary engines would run above 90% load in Transit mode. Increase auxiliary " +
            "engine capacity or count, or reduce the hotel/mission power.");
    }

    // ── Branch 4: structural fallback ───────────────────────────────────────────

    [Fact]
    public async Task StructurallyImpossiblePlant_FallsBackToTheGenericExplanation()
    {
        // Hotel 5000 against AE 1×100 and no SG: no combination ever reaches load distribution,
        // so every rejection is structural and no specific counter fires.
        var factory = TestServiceFactory.Create();
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(5000, 2)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(100, 1)
            .WithPropulsionPower(4000)
            .WithSeaMargin(0)
            .WithTransitMode(5000, 5000)
            .Build();

        var act = () => factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit);

        var ex = act.Should().Throw<NoValidCombinationException>().Which;

        ex.UserMessage.Should().Be(
            "no engine configuration can cover the Transit demand. Check the engine capacities, " +
            "engine counts and the power demands for this mode.");
    }

    // ── Precedence: the battery gate outranks the capacity message ──────────────

    [Fact]
    public async Task BatteryPtiGate_OutranksTheEngineCapacityMessage()
    {
        // The deficit plant would report "installed engines cannot carry…" on its own (see above);
        // with the battery gate also firing, the battery message must win.
        var factory = TestServiceFactory.Create();
        var input = DeficitPlant();
        input.MaxPtiPerEngineKw = 500;

        var act = () => factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit,
            batteryAdjustment: new BatteryL1Adjustment(0, 0, PropulsionPeakShavingKw: 900));

        var ex = act.Should().Throw<NoValidCombinationException>().Which;

        ex.UserMessage.Should().StartWith("the battery needs");
        ex.UserMessage.Should().NotContain("installed engines cannot carry");
    }

    // ── The exception itself ────────────────────────────────────────────────────

    [Fact]
    public async Task Exception_CarriesTheModeAndWrapsTheUserMessage()
    {
        var factory = TestServiceFactory.Create();

        var act = () => factory.Level1Service.FindOptimalCombination(DeficitPlant(), factory.Curves, OperationalMode.Transit);

        var ex = act.Should().Throw<NoValidCombinationException>().Which;

        ex.Mode.Should().Be(OperationalMode.Transit);
        ex.Message.Should().Be(
            $"No valid engine combinations found for Transit mode: {ex.UserMessage}");
    }
}
