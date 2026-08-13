using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Battery;
using KSailCalc.Api.Services.Calculation;
using KSailCalc.Tests.TestHelpers;
using Microsoft.Extensions.Options;

namespace KSailCalc.Tests.Calculation;

/// <summary>
/// Epic E1 story DE-B: at MeCount == 0 the auxiliary engines carry the whole demand
/// (propulsion × (1 + ElectricPropulsionLossFactor) + hotel); everything upstream of the
/// distribution — cascade, adjustments, baseline rules — runs unchanged.
///
/// Hand-derived numbers from the story file (AE 4×4000, SM 0 throughout):
/// demand 11 000 → ae=3 is 91.7% > 90% (rejected), ae=4 sole survivor at 68.75%.
/// </summary>
public class DieselElectricLevel1Tests
{
    private static readonly TestServiceFactory Factory = TestServiceFactory.Create();

    private static Level1OptimizationService NewService(double lossFactor = 0)
        => new(Options.Create(new BatterySettings()),
            Options.Create(new CalculatorSettings { ElectricPropulsionLossFactor = lossFactor }));

    /// <summary>Engine type ids stay 1/1 (builder default) so the test SFOC curves resolve.</summary>
    private static CalculatorInput DieselElectric(double propulsion, double hotel)
        => CalculatorInputBuilder.Default()
            .WithMainEngines(0, 0)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(4000, 4)
            .WithPropulsionPower(propulsion)
            .WithSeaMargin(0)
            .WithTransitMode(5000, hotel)
            .Build();

    private static Level1Result Run(
        CalculatorInput input, OperationalMode mode = OperationalMode.Transit,
        double lossFactor = 0, BatteryL1Adjustment? battery = null)
        => NewService(lossFactor).FindOptimalCombination(
            input, Factory.CurvesFor(input), mode, batteryAdjustment: battery);

    // ── AC1: survivor space and the 90 % cap ────────────────────────────────────

    [Fact]
    public void WholeDemandLandsOnTheAuxiliaries_And90PercentCapPolicesIt()
    {
        var result = Run(DieselElectric(8000, 3000)); // demand 11 000

        // ae=1,2 cannot carry it; ae=3 runs at 91.7% > 90%; ae=4 is the sole survivor.
        var only = result.AllValidCombinations.Should().ContainSingle().Subject;
        only.ActiveMeCount.Should().Be(0);
        only.SgEnabled.Should().BeFalse();
        only.ActiveAeCount.Should().Be(4);
        only.MePowerKw.Should().Be(0);
        only.SgPowerKw.Should().Be(0);
        only.AePowerKw.Should().Be(11000);
        only.AeLoadPercent.Should().BeApproximately(11000.0 / 16000, 1e-9);

        // Single survivor ⇒ baseline = optimal ⇒ zero Level 1 savings (the known clamp behaviour).
        result.BaselineFocTonPerHour.Should().Be(result.OptimalFocTonPerHour);
    }

    // ── AC2: ranking and the no-battery baseline ────────────────────────────────

    [Fact]
    public void TwoSurvivors_RankByFuel_BaselineIsTheWorse()
    {
        var result = Run(DieselElectric(5000, 2600)); // demand 7 600

        result.AllValidCombinations.Should().HaveCount(2); // ae=3 (63.3%) and ae=4 (47.5%)
        result.AllValidCombinations.Should().OnlyContain(c => c.MePowerKw == 0 && !c.SgEnabled);

        // Fewer engines at higher load sit lower on the AE SFOC curve → ae=3 wins.
        result.OptimalCombination.ActiveAeCount.Should().Be(3);
        result.BaselineCombination.ActiveAeCount.Should().Be(4);   // count − 1, no battery
        result.BaselineFocTonPerHour.Should().BeGreaterThan(result.OptimalFocTonPerHour);
    }

    // ── AC3: the electric loss factor ───────────────────────────────────────────

    [Fact]
    public void LossFactorGrossesUpThePropulsionSideOnly()
    {
        var result = Run(DieselElectric(5000, 2600), lossFactor: 0.05);

        // demand = 2 600 + 5 000 × 1.05 = 7 850
        result.OptimalCombination.AePowerKw.Should().BeApproximately(7850, 1e-9);
    }

    [Fact]
    public void LossFactorDefaultsToZero()
    {
        new CalculatorSettings().ElectricPropulsionLossFactor.Should().Be(0,
            "D-DE2: the user enters demand at the switchboard unless configuration says otherwise");

        // The optional constructor argument (kept for the existing test call sites) means factor 0.
        var withoutSettings = new Level1OptimizationService(Options.Create(new BatterySettings()));
        var result = withoutSettings.FindOptimalCombination(
            DieselElectric(5000, 2600), Factory.CurvesFor(DieselElectric(5000, 2600)), OperationalMode.Transit);
        result.OptimalCombination.AePowerKw.Should().Be(7600);
    }

    // ── AC4/AC5: battery — cascade values land on the AE side, two worlds differ by J ──

    /// <summary>
    /// Battery 800 kW in Transit on propulsion 10 000 / hotel 3 000 (cascade arithmetic, unchanged
    /// code, verified against BatteryAllocationService below): Propulsion H=500, I=500, J=175,
    /// L=325; Hotel H=60, I=60, J=3, L=57. World A carries L; world B (budget 0) carries H.
    /// </summary>
    private static CalculatorInput BatteryBoat() => DieselElectric(10000, 3000);

    [Fact]
    public void CascadeNumbersMatchTheHandDerivation()
    {
        var allocation = new BatteryAllocationService(Options.Create(new BatterySettings()))
            .Allocate(OperationalMode.Transit,
                CalculatorInputBuilder.Default()
                    .WithMainEngines(0, 0).WithShaftGenerators(0)
                    .WithAuxiliaryEngines(4000, 4)
                    .WithPropulsionPower(10000).WithSeaMargin(0)
                    .WithTransitMode(5000, 3000)
                    .WithBattery(800, 800, OperationalMode.Transit)
                    .Build());

        var propulsion = allocation.Loads.Should().ContainSingle(l => l.Load == BatteryLoadType.Propulsion).Subject;
        propulsion.VariationKw.Should().Be(500);
        propulsion.CoveredBandKw.Should().Be(175);
        propulsion.UncoveredReserveKw.Should().Be(325);

        var hotel = allocation.Loads.Should().ContainSingle(l => l.Load == BatteryLoadType.Hotel).Subject;
        hotel.CoveredBandKw.Should().Be(3);
        hotel.UncoveredReserveKw.Should().Be(57);

        allocation.PeakShavingBandKw.Should().Be(178);
        allocation.AdditionalSpinningReserveKw.Should().Be(382);
    }

    [Fact]
    public void BatteryWorldA_UncoveredReserveRidesOnTheAuxiliaries()
    {
        var worldA = Run(BatteryBoat(), battery: new BatteryL1Adjustment(325, 57, 175));

        // (10 000 + 325) + (3 000 + 57) = 13 382 on ae=4 → 83.6 %
        var only = worldA.AllValidCombinations.Should().ContainSingle().Subject;
        only.AePowerKw.Should().BeApproximately(13382, 1e-9);

        // With a battery the baseline rule is max(0, count − 3) → clamps to the optimum here.
        worldA.SelectedBaselineIndex.Should().Be(0);
    }

    [Fact]
    public void BatteryBenefit_WorldsDifferByTheCoveredBand()
    {
        var worldA = Run(BatteryBoat(), battery: new BatteryL1Adjustment(325, 57, 175));
        var worldB = Run(BatteryBoat(), battery: new BatteryL1Adjustment(500, 60, 0)); // budget 0: L = H

        var demandGap = worldB.OptimalCombination.AePowerKw - worldA.OptimalCombination.AePowerKw;
        demandGap.Should().BeApproximately(178, 1e-9, "world B adds back exactly the covered J per side");

        worldB.OptimalFocTonPerHour.Should().BeGreaterThan(worldA.OptimalFocTonPerHour,
            "carrying the full swing burns more fuel — that difference is the Battery Benefit");
    }

    // ── AC6: DP at 0 ME — thrust is an electric load like everything else ───────

    [Fact]
    public void DpThrustLandsOnTheAuxiliaries()
    {
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(0, 0).WithShaftGenerators(0)
            .WithAuxiliaryEngines(4000, 4)
            .WithPropulsionPower(8000).WithSeaMargin(0)
            .WithTransitMode(2500, 3000)
            .WithDPMode(1000, 3500, 1000)
            .Build();

        var result = Run(input, OperationalMode.DP);

        // thrust 1 000 + hotel 3 500 = 4 500; ae=2..4 survive, every one pure-AE.
        result.AllValidCombinations.Should().HaveCount(3);
        result.AllValidCombinations.Should().OnlyContain(c => c.MePowerKw == 0 && c.AePowerKw == 4500);
    }

    [Fact]
    public void DpUncoveredRedundancyRaisesTheAuxDemand()
    {
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(0, 0).WithShaftGenerators(0)
            .WithAuxiliaryEngines(4000, 4)
            .WithPropulsionPower(8000).WithSeaMargin(0)
            .WithTransitMode(2500, 3000)
            .WithDPMode(1000, 3500, 1000)
            .Build();
        input.DpRedundancyRequirementKw = 800;

        // Battery 500 DP-only: DpReserve H=800, I=500, J=500 (1:1), L=300 → thrust side.
        // Hotel H=70 gets nothing → L=70. Adjustment = (300, 70, 0) — reserve band is not PS.
        var result = Run(input, OperationalMode.DP, battery: new BatteryL1Adjustment(300, 70, 0));

        // (1 000 + 300) + (3 500 + 70) = 4 870 — carried entirely by the AEs, no PTI gate.
        result.OptimalCombination.AePowerKw.Should().BeApproximately(4870, 1e-9);
        result.OptimalCombination.MePowerKw.Should().Be(0);
    }

    // ── AC7: Level 2 at zero SG — characterization, not belief ──────────────────

    [Fact]
    public void Level2AtZeroSg_OptimizesTheAeSplit()
    {
        // ARCHITECTURE CORRECTION (design §8.3 said "expected empty" — the first run of this
        // test proved otherwise): Level 2 sweeps UNEQUAL splits across the active AEs, so with
        // the whole diesel-electric demand on the aux side it has real room to work. Level 2 is
        // LIVE at 0 ME — a product feature, not a bug. The exact figure is a characterization
        // pin of today's 2% grid on the test curves.
        var input = DieselElectric(5000, 2600);
        var level1 = Run(input);

        var level2 = new Level2OptimizationService()
            .OptimizeLoadSetpoints(level1, input, Factory.CurvesFor(input));

        level2.Should().NotBeNull("a 0-ME plant must not crash Level 2");
        level2.Level2SavingsTonPerHour.Should().BeGreaterThanOrEqualTo(0);
        level2.Level2SavingsTonPerHour.Should().BeApproximately(0.0009, 1e-5,
            "characterization: the unequal AE split beats Level 1's equal split by this much here");
    }

    // ── Rejection wording: the diesel-electric no-survivor sentence ─────────────

    [Fact]
    public void AllRejectedByTheCap_ExplainsWithTheDieselElectricSentence()
    {
        // AE 2×4000: demand 7 600 needs > 90 % of the full fleet — every combination dies.
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(0, 0).WithShaftGenerators(0)
            .WithAuxiliaryEngines(4000, 2)
            .WithPropulsionPower(5000).WithSeaMargin(0)
            .WithTransitMode(5000, 2600)
            .Build();

        var act = () => Run(input);

        act.Should().Throw<NoValidCombinationException>()
            .Which.Message.Should().Contain("whole diesel-electric demand")
            .And.Contain("propulsion or hotel/mission power");
    }
}
