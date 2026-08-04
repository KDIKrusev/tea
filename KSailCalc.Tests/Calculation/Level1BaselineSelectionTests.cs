using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Tests.TestHelpers;

namespace KSailCalc.Tests.Calculation;

/// <summary>
/// The baseline is an INDEX into the FOC-sorted candidate list, so two things must hold together:
/// the selection policy (default / battery D1 rule / user pin), and the ordering that index
/// addresses. Story R-C gave the policy a name; these tests pin both halves.
/// </summary>
public class Level1BaselineSelectionTests
{
    private static IReadOnlyList<EngineCombination> Candidates(int count)
        => Enumerable.Range(0, count).Select(_ => new EngineCombination()).ToList();

    /// <summary>
    /// A plant that genuinely offers a choice: no SG, ME 2×5000 carrying 4000 kW of propulsion, and
    /// hotel 1200 kW coverable by either 2 or 3 of the 800 kW aux engines ⇒ 4 valid combinations.
    /// The default builder yields a single combination, which would make every ordering assertion
    /// below vacuously true.
    /// </summary>
    private static CalculatorInput PlantWithSeveralValidCombinations() => CalculatorInputBuilder.Default()
        .WithMainEngines(5000, 2)
        .WithShaftGenerators(0)
        .WithAuxiliaryEngines(800, 3)
        .WithPropulsionPower(4000)
        .WithSeaMargin(0)
        .WithTransitMode(5000, 1200)
        .Build();

    // ── The policy (AC5) ────────────────────────────────────────────────────────

    [Fact]
    public void WithoutBatteryOrPin_TheBaselineIsTheHighestFocCombination()
    {
        Level1OptimizationService.SelectBaseline(Candidates(8), null, hasBatteryAdjustment: false)
            .Should().Be(7, "the last entry of a FOC-ascending list is the theoretical worst case");
    }

    [Fact]
    public void WithABatteryAndNoPin_TheBaselineIsTheThirdHighest()
    {
        Level1OptimizationService.SelectBaseline(Candidates(8), null, hasBatteryAdjustment: true)
            .Should().Be(5, "decision D1 — a battery vessel already operates better than the worst case");
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 1)]
    public void WithABattery_TheThirdHighestRuleClampsOnShortLists(int candidateCount, int expected)
    {
        Level1OptimizationService.SelectBaseline(Candidates(candidateCount), null, hasBatteryAdjustment: true)
            .Should().Be(expected);
    }

    [Fact]
    public void APinnedIndexWinsOverBothDefaults()
    {
        Level1OptimizationService.SelectBaseline(Candidates(8), 2, hasBatteryAdjustment: false).Should().Be(2);
        Level1OptimizationService.SelectBaseline(Candidates(8), 2, hasBatteryAdjustment: true).Should().Be(2);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(8)]
    [InlineData(99)]
    public void APinThatDoesNotAddressTheListFallsBackToTheDefault(int requestedIndex)
    {
        Level1OptimizationService.SelectBaseline(Candidates(8), requestedIndex, hasBatteryAdjustment: false)
            .Should().Be(7, "an out-of-range pin must not throw and must not silently clamp to an edge");
    }

    [Fact]
    public void APinOfZeroIsHonoured_ItIsNotTreatedAsAbsent()
    {
        Level1OptimizationService.SelectBaseline(Candidates(8), 0, hasBatteryAdjustment: false)
            .Should().Be(0, "index 0 is the optimum — an unusual but legitimate baseline choice");
    }

    // ── The ordering the index addresses (AC6) ──────────────────────────────────

    [Fact]
    public async Task ValidCombinations_AreOrderedByFocThenByTotalEngineCount()
    {
        var factory = TestServiceFactory.Create();
        var input = PlantWithSeveralValidCombinations();

        var result = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit);

        result.AllValidCombinations.Count.Should().BeGreaterThan(1, "the ordering must be observable");
        result.AllValidCombinations.Should().BeInAscendingOrder(c => c.FocTonPerHour);
        result.AllValidCombinations.Should().BeEquivalentTo(
            result.AllValidCombinations
                .OrderBy(c => c.FocTonPerHour)
                .ThenBy(c => c.ActiveMeCount + c.ActiveAeCount),
            options => options.WithStrictOrdering(),
            "the baseline index addresses this exact order — reordering repoints every pinned baseline");
    }

    [Fact]
    public async Task TheOptimumIsTheFirstEntryAndTheDefaultBaselineIsTheLast()
    {
        var factory = TestServiceFactory.Create();
        var input = PlantWithSeveralValidCombinations();

        var result = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit);

        result.AllValidCombinations.Count.Should().BeGreaterThan(1, "first and last must differ");
        result.OptimalCombination.Should().BeSameAs(result.AllValidCombinations[0]);
        result.BaselineCombination.Should().BeSameAs(result.AllValidCombinations[^1]);
        result.SelectedBaselineIndex.Should().Be(result.AllValidCombinations.Count - 1);
    }

    [Fact]
    public async Task APinnedBaselineSelectsThatExactCombination()
    {
        var factory = TestServiceFactory.Create();
        var input = PlantWithSeveralValidCombinations();

        var unpinned = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit);
        unpinned.AllValidCombinations.Count.Should().BeGreaterThan(1, "the scenario must offer a choice");
        unpinned.SelectedBaselineIndex.Should().NotBe(0, "otherwise pinning 0 would prove nothing");

        var pinned = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit, baselineIndex: 0);

        pinned.SelectedBaselineIndex.Should().Be(0);
        pinned.BaselineFocTonPerHour.Should().Be(pinned.AllValidCombinations[0].FocTonPerHour);
        pinned.BaselineFocTonPerHour.Should().Be(pinned.OptimalFocTonPerHour,
            "pinning index 0 makes the baseline the optimum, so the reported savings collapse to zero");
    }
}
