using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Tests.TestHelpers;
using Microsoft.Extensions.Options;

namespace KSailCalc.Tests.Calculation;

/// <summary>
/// Epic 3 / Story 3.2 — per-fuel CO2.
/// Verifies the fuel-factor resolver, NFR1 (no-fuel requests are legacy-identical),
/// and per-engine CO2 when ME and AE burn different fuels.
/// </summary>
public class EngineFuelCo2Tests
{
    private const double Mgo = 3.93267;
    private const double Lng = 2.753; // IMO MEPC.364(79): WtT 0.00266 + TTW 2.75 = total lifecycle
    private const double Fallback = 3.206; // CalculatorSettings.Co2Factor default

    // === Co2FactorFor resolution ===

    [Fact]
    public void Co2FactorFor_KnownFuel_ReturnsConfiguredFactor()
    {
        var settings = new CalculatorSettings
        {
            FuelCo2Factors = new(StringComparer.OrdinalIgnoreCase) { ["MGO"] = Mgo, ["LNG"] = Lng }
        };

        settings.Co2FactorFor("LNG").Should().Be(Lng);
        settings.Co2FactorFor("mgo").Should().Be(Mgo); // case-insensitive
    }

    [Fact]
    public void Co2FactorFor_NullOrUnknown_FallsBackToCo2Factor()
    {
        var settings = new CalculatorSettings
        {
            FuelCo2Factors = new(StringComparer.OrdinalIgnoreCase) { ["MGO"] = Mgo }
        };

        settings.Co2FactorFor(null).Should().Be(Fallback);
        settings.Co2FactorFor("").Should().Be(Fallback);
        settings.Co2FactorFor("Unobtainium").Should().Be(Fallback);
    }

    // === NFR1: no fuel set => identical to legacy single-constant behaviour ===

    [Fact]
    public async Task NoFuel_BaselineCo2_EqualsBaselineFocTimesFallback()
    {
        var sut = TestServiceFactory.Create().CalculatorService; // default settings: empty fuel dict
        var input = CalculatorInputBuilder.Default().WithVesselTypeName("Bulk Carrier").Build();

        var result = await sut.CalculateAllVariantsAsync(input);

        result.BaselineCO2.Should().BeApproximately(result.BaselineFOC * Fallback, 1e-6);
        result.Advanced.Co2Reduction.Should().BeApproximately(result.Advanced.FuelSavings * Fallback, 1e-6);
    }

    // === Per-engine CO2 is reported by the API (client must not recompute) ===

    [Fact]
    public async Task PerEngineCo2_IsReported_AndSumsToTheTotals()
    {
        var factory = TestServiceFactory.Create();
        var fuelSettings = Options.Create(new CalculatorSettings
        {
            FuelCo2Factors = new(StringComparer.OrdinalIgnoreCase) { ["MGO"] = Mgo, ["LNG"] = Lng }
        });
        var sut = new CalculatorService(
            factory.ConfigRepoMock.Object, factory.SailContributionService,
            factory.PipelineRunner, fuelSettings, NullLogger<CalculatorService>.Instance);

        var input = CalculatorInputBuilder.Default().Build();
        input.MainFuelType = "LNG";   // deliberately different factors so a wrong
        input.AuxFuelType = "MGO";    // single-constant recomputation cannot pass

        var result = await sut.CalculateAllVariantsAsync(input);

        // Baseline: per-engine CO2 uses each engine's own fuel factor and sums to the total
        result.BaselineMeCO2.Should().BeApproximately(result.BaselineME * Lng, 1e-6);
        result.BaselineAeCO2.Should().BeApproximately(result.BaselineAE * Mgo, 1e-6);
        (result.BaselineMeCO2 + result.BaselineAeCO2).Should().BeApproximately(result.BaselineCO2, 1e-6);

        // Every variant: same invariant against its own optimized total
        foreach (var variant in new[] { result.Advanced, result.Pro, result.Premium })
        {
            variant.OptimizedMeCO2.Should().BeApproximately(variant.OptimizedME * Lng, 1e-6);
            variant.OptimizedAeCO2.Should().BeApproximately(variant.OptimizedAE * Mgo, 1e-6);
            (variant.OptimizedMeCO2 + variant.OptimizedAeCO2)
                .Should().BeApproximately(variant.OptimizedCO2, 1e-6);
        }
    }

    // === Per-engine CO2 with different fuels for ME and AE ===

    [Fact]
    public async Task PerFuel_BaselineCo2_UsesPerEngineFactors_AndTonnageUnchanged()
    {
        var factory = TestServiceFactory.Create();

        // No-fuel baseline (uses fallback 3.206)
        var noFuelInput = CalculatorInputBuilder.Default().WithVesselTypeName("Bulk Carrier").Build();
        var noFuel = await factory.CalculatorService.CalculateAllVariantsAsync(noFuelInput);

        // Build a CalculatorService with populated fuel factors, reusing the factory's wired services
        var fuelSettings = Options.Create(new CalculatorSettings
        {
            FuelCo2Factors = new(StringComparer.OrdinalIgnoreCase) { ["MGO"] = Mgo, ["LNG"] = Lng }
        });
        var sutFuel = new CalculatorService(
            factory.ConfigRepoMock.Object,
            factory.SailContributionService,
            factory.PipelineRunner,
            fuelSettings,
            NullLogger<CalculatorService>.Instance);

        var fuelInput = CalculatorInputBuilder.Default()
            .WithVesselTypeName("Bulk Carrier")
            .Build();
        fuelInput.MainFuelType = "LNG";
        fuelInput.AuxFuelType = "MGO";

        var withFuel = await sutFuel.CalculateAllVariantsAsync(fuelInput);

        // CO2 is per-engine: ME on LNG, AE on MGO
        var expectedBaselineCo2 = withFuel.BaselineME * Lng + withFuel.BaselineAE * Mgo;
        withFuel.BaselineCO2.Should().BeApproximately(expectedBaselineCo2, 1e-6);

        // It must differ from the single-constant result (sanity: fuel actually changed CO2)
        withFuel.BaselineCO2.Should().NotBeApproximately(noFuel.BaselineCO2, 1.0);

        // NFR1: tonnage and optimizer outputs are unaffected by fuel choice
        withFuel.BaselineFOC.Should().BeApproximately(noFuel.BaselineFOC, 1e-6);
        withFuel.Advanced.FuelSavings.Should().BeApproximately(noFuel.Advanced.FuelSavings, 1e-6);
        withFuel.Premium.FuelSavings.Should().BeApproximately(noFuel.Premium.FuelSavings, 1e-6);
    }
}
