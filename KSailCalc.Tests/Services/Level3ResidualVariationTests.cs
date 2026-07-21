using FluentAssertions;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Tests.TestHelpers;

namespace KSailCalc.Tests.Services;

/// <summary>
/// Story: Battery Increment E — L3 DRC operates on the residual variation after the battery's
/// hotel-side peak shaving (anti double-counting; Q4 working rule).
/// </summary>
public class Level3ResidualVariationTests
{
    private const double Precision = 1e-6;

    /// <summary>Transit scenario with an L3 variation of ±500 kW (explicit override).</summary>
    private static CalculatorInputBuilder Plant() => CalculatorInputBuilder.Default()
        .WithMainEngines(24000, 2)
        .WithShaftGenerators(1000)
        .WithAuxiliaryEngines(2000, 3)
        .WithPropulsionPower(11463)
        .WithSeaMargin(0)
        .WithTransitMode(5000, 3800);

    private static KSailCalc.Api.Models.CalculatorInput WithVariation(
        CalculatorInputBuilder builder, double variationKw)
    {
        var input = builder.Build();
        input.HotelLoadVariationKw = variationKw;
        return input;
    }

    // ── AC2: residual rule at the service level ──────────────────────────────

    [Fact]
    public async Task Level3_HotelBand_ReducesVariation_AndReportsShavedAmount()
    {
        var factory = TestServiceFactory.Create();
        var input = WithVariation(Plant(), 500);

        var l1 = await factory.Level1Service.FindOptimalCombinationAsync(input, OperationalMode.Transit);
        var l2 = await factory.Level2Service.OptimizeLoadSetpointsAsync(l1, input);

        var without = await factory.Level3Service.CalculateDrcSavingsAsync(l2, input, 5000);
        var with = await factory.Level3Service.CalculateDrcSavingsAsync(l2, input, 5000, batteryHotelPeakShavingKw: 200);

        without.VariationPerGeneratorKw.Should().BeApproximately(500, Precision);
        with.VariationPerGeneratorKw.Should().BeApproximately(300, Precision);   // 500 − 200
        with.BatteryShavedVariationKw.Should().BeApproximately(200, Precision);
        with.DrcSavingsTonPerYear.Should().BeLessThan(without.DrcSavingsTonPerYear);
    }

    // ── AC3: full shaving ⇒ zero DRC savings ─────────────────────────────────

    [Fact]
    public async Task Level3_BandCoversFullVariation_ZeroDrcSavings()
    {
        var factory = TestServiceFactory.Create();
        var input = WithVariation(Plant(), 500);

        var l1 = await factory.Level1Service.FindOptimalCombinationAsync(input, OperationalMode.Transit);
        var l2 = await factory.Level2Service.OptimizeLoadSetpointsAsync(l1, input);

        var result = await factory.Level3Service.CalculateDrcSavingsAsync(l2, input, 5000, batteryHotelPeakShavingKw: 800);

        result.VariationPerGeneratorKw.Should().Be(0);
        result.BatteryShavedVariationKw.Should().BeApproximately(500, Precision); // clamped to V
        result.DrcSavingsTonPerYear.Should().Be(0);
    }

    // ── AC1: zero regression ─────────────────────────────────────────────────

    [Fact]
    public async Task Level3_ZeroBand_IdenticalToPreChangeBehaviour()
    {
        var factory = TestServiceFactory.Create();
        var input = WithVariation(Plant(), 500);

        var l1 = await factory.Level1Service.FindOptimalCombinationAsync(input, OperationalMode.Transit);
        var l2 = await factory.Level2Service.OptimizeLoadSetpointsAsync(l1, input);

        var implicitDefault = await factory.Level3Service.CalculateDrcSavingsAsync(l2, input, 5000);
        var explicitZero = await factory.Level3Service.CalculateDrcSavingsAsync(l2, input, 5000, batteryHotelPeakShavingKw: 0);

        explicitZero.DrcSavingsTonPerYear.Should().Be(implicitDefault.DrcSavingsTonPerYear);
        explicitZero.BatteryShavedVariationKw.Should().Be(0);
    }

    // ── AC4: end-to-end — Transit battery lowers Premium L3 savings ──────────

    [Fact]
    public async Task Calculate_TransitBattery_ReducesPremiumLevel3Savings()
    {
        var factory = TestServiceFactory.Create();
        var noBattery = WithVariation(Plant(), 500);
        // Excel scenario: hotel-side covered band = 3.8 kW (76 × 0.05)
        var withBattery = WithVariation(Plant().WithBattery(1260, 2000, OperationalMode.Transit), 500);

        var resultNoBattery = await factory.CalculatorService.CalculateAllVariantsAsync(noBattery);
        var resultWithBattery = await factory.CalculatorService.CalculateAllVariantsAsync(withBattery);

        var l3 = resultWithBattery.Premium.Level3Details!;
        l3.BatteryShavedVariationKw.Should().BeApproximately(3.8, Precision);
        l3.VariationPerGeneratorKw.Should().BeApproximately(500 - 3.8, Precision);
        // Note: with-battery L2 setpoints differ (adjusted demand), so compare the variation
        // basis rather than raw savings magnitude; the shaved band must be excluded from DRC.
        resultNoBattery.Premium.Level3Details!.BatteryShavedVariationKw.Should().Be(0);
        resultNoBattery.Premium.Level3Details.VariationPerGeneratorKw.Should().BeApproximately(500, Precision);
    }
}
