using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Tests.TestHelpers;

namespace KSailCalc.Tests.Calculation;

/// <summary>
/// What Level 2 GUARANTEES, rather than how its sweep works.
///
/// The sweep is the densest code left in the backend — a recursive search that fills engines left to
/// right and lets the last one absorb the remainder. Reading that loop is not the fastest way to
/// understand it, and splitting it apart would scatter a search that only makes sense whole.
///
/// So this file is the reading material instead: run several genuinely different plants through it
/// and assert the properties that must hold for every one of them. If you are new to Level 2, read
/// these assertions first — they are the contract; the loop is just how it is met.
/// </summary>
public class Level2InvariantTests
{
    private const double Tolerance = 1e-6;

    /// <summary>
    /// Plants chosen to exercise different shapes of the search: two engines with a comfortable
    /// demand, three engines where one can be switched off, a plant with a shaft generator in the
    /// mix, and a tightly loaded one near the 90% ceiling.
    /// </summary>
    public static TheoryData<string, CalculatorInput> Plants => new()
    {
        {
            "2 AE, mid demand",
            CalculatorInputBuilder.Default()
                .WithMainEngines(8500, 1).WithShaftGenerators(0)
                .WithAuxiliaryEngines(4000, 2)
                .WithPropulsionPower(8000).WithSeaMargin(0)
                .WithTransitMode(5717, 5000).Build()
        },
        {
            "3 AE, one can be switched off",
            CalculatorInputBuilder.Default()
                .WithMainEngines(12000, 1).WithShaftGenerators(0)
                .WithAuxiliaryEngines(1000, 3)
                .WithPropulsionPower(3000).WithSeaMargin(0)
                .WithTransitMode(5717, 1400).Build()
        },
        {
            "SG carries part of the hotel",
            CalculatorInputBuilder.Default()
                .WithMainEngines(12000, 1).WithShaftGenerators(2000)
                .WithAuxiliaryEngines(2000, 2)
                .WithPropulsionPower(3000).WithSeaMargin(0)
                .WithTransitMode(5717, 3000).Build()
        },
        {
            "loaded near the ceiling",
            CalculatorInputBuilder.Default()
                .WithMainEngines(12000, 2).WithShaftGenerators(0)
                .WithAuxiliaryEngines(1000, 3)
                .WithPropulsionPower(4000).WithSeaMargin(0)
                .WithTransitMode(5717, 2500).Build()
        }
    };

    private static (Level1Result L1, Level2Result L2) Run(CalculatorInput input)
    {
        var factory = TestServiceFactory.Create();
        var l1 = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit);
        var l2 = factory.Level2Service.OptimizeLoadSetpoints(l1, input, factory.Curves);
        return (l1, l2);
    }

    private static List<GeneratorSetpoint> RunningAuxiliaries(Level2Result l2) =>
        l2.OptimalSetpoints.Where(s => s.GeneratorType == GeneratorType.AE && s.PowerKw > Tolerance).ToList();

    // ── The load window ─────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(Plants))]
    public void EveryRunningAuxiliaryStaysInsideTheTenToNinetyPercentWindow(string _, CalculatorInput input)
    {
        var (_, l2) = Run(input);

        RunningAuxiliaries(l2).Should().OnlyContain(
            s => s.LoadPercent >= 0.10 - Tolerance && s.LoadPercent <= 0.90 + Tolerance,
            "below 10% a generator wastes fuel and fouls; above 90% there is no headroom left");
    }

    [Theory]
    [MemberData(nameof(Plants))]
    public void AnEngineIsEitherRunningOrFullyOff_NeverIdling(string _, CalculatorInput input)
    {
        var (_, l2) = Run(input);

        l2.OptimalSetpoints
            .Where(s => s.GeneratorType == GeneratorType.AE)
            .Should().OnlyContain(s => s.PowerKw <= Tolerance || s.LoadPercent >= 0.10 - Tolerance,
                "the sweep switches an engine off rather than trickling it below the minimum");
    }

    // ── The demand is met ───────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(Plants))]
    public void TheRunningAuxiliariesCoverExactlyTheDemandLevel1AssignedThem(string _, CalculatorInput input)
    {
        var (l1, l2) = Run(input);

        RunningAuxiliaries(l2).Sum(s => s.PowerKw)
            .Should().BeApproximately(l1.OptimalCombination.AePowerKw, 1e-3,
                "Level 2 redistributes the aux demand, it never changes how much of it there is");
    }

    // ── It never makes things worse ─────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(Plants))]
    public void ReportedSavingsAreNeverNegative(string _, CalculatorInput input)
    {
        var (_, l2) = Run(input);

        l2.Level2SavingsTonPerHour.Should().BeGreaterThanOrEqualTo(0,
            "Level2SavingsTonPerHour is clamped with Math.Max(0, …), so the client can never be " +
            "shown a Pro tier that costs more than Advanced");
    }

    /// <summary>
    /// A quirk worth knowing before it is rediscovered as a bug.
    ///
    /// The sweep searches load distributions on a 2% grid. Level 1's own split — whatever the equal
    /// distribution happened to be — is not necessarily ON that grid, so the best grid point can be
    /// a hair MORE expensive than what Level 1 assumed. <c>Level2FocTonPerHour</c> is not clamped,
    /// so it can sit fractionally above <c>Level1FocTonPerHour</c>.
    ///
    /// This is invisible to the client: <c>Level2Details</c> exposes only <c>OptimalTotalSfoc</c>
    /// and the clamped <c>SavingsTonPerHour</c>, never the raw FOC. Measured overshoot on the
    /// tightest plant here is under 1e-6 t/h — under a gram per hour.
    /// </summary>
    [Theory]
    [MemberData(nameof(Plants))]
    public void Level2FocMatchesLevel1_ToWithinTheSweepsGridResolution(string _, CalculatorInput input)
    {
        var (_, l2) = Run(input);

        // One grid step is 2% of an aux engine; at a realistic SFOC that is ~1e-3 t/h. Anything
        // beyond that would mean the sweep genuinely picked a worse distribution, not a grid artefact.
        const double gridArtefactAllowance = 1e-3;

        l2.Level2FocTonPerHour.Should().BeLessThanOrEqualTo(
            l2.Level1FocTonPerHour + gridArtefactAllowance,
            "Level 2 may miss Level 1's exact split by one grid step, but must never be materially worse");
    }

    // ── What Level 2 does NOT touch ─────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(Plants))]
    public void TheShaftGeneratorIsPassedThroughUnchanged(string _, CalculatorInput input)
    {
        var (l1, l2) = Run(input);

        var sg = l2.OptimalSetpoints.SingleOrDefault(s => s.GeneratorType == GeneratorType.SG);
        if (l1.OptimalCombination.SgEnabled && l1.OptimalCombination.SgPowerKw > Tolerance)
        {
            sg.Should().NotBeNull();
            sg!.PowerKw.Should().BeApproximately(l1.OptimalCombination.SgPowerKw, Tolerance,
                "SG load is fixed by the main engine shaft — Level 2 optimizes the aux side only");
        }
        else
        {
            sg.Should().BeNull();
        }
    }

    [Theory]
    [MemberData(nameof(Plants))]
    public void ASetpointIsReportedForEveryInstalledAuxiliary_OffOnesIncluded(string _, CalculatorInput input)
    {
        var (l1, l2) = Run(input);

        if (l1.OptimalCombination.ActiveAeCount == 0) return; // pass-through result, covered below

        l2.OptimalSetpoints.Count(s => s.GeneratorType == GeneratorType.AE)
            .Should().Be(l1.OptimalCombination.ActiveAeCount,
                "the client draws one row per engine Level 1 had running, including any Level 2 shut down");
    }

    // ── The degenerate case ─────────────────────────────────────────────────────

    [Fact]
    public void WithNoAuxiliaryDemand_Level2PassesLevel1Through()
    {
        // SG covers the whole hotel load, so Level 1 runs no auxiliaries at all.
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 1).WithShaftGenerators(2000)
            .WithAuxiliaryEngines(800, 3)
            .WithPropulsionPower(3000).WithSeaMargin(0)
            .WithTransitMode(5717, 500).Build();

        var (l1, l2) = Run(input);

        l1.OptimalCombination.ActiveAeCount.Should().Be(0, "the SG alone covers this hotel load");
        l2.Level2SavingsTonPerHour.Should().Be(0, "there is nothing to redistribute");
        l2.Level2FocTonPerHour.Should().Be(l2.Level1FocTonPerHour);
    }
}
