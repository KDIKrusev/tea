using KSailCalc.Api.Services.Results;
using System.Text.Json;
using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Tests.TestHelpers;
using Microsoft.Extensions.Options;

namespace KSailCalc.Tests.Battery;

/// <summary>
/// Implements the NEW scenarios from docs/qa/assessments/battery-test-design-20260713.md.
/// Test names carry the scenario IDs (A2, B1, …) for traceability; every expected number is
/// hand-derived from the Excel reference workbook formulas (see the test-design doc §0).
/// </summary>
public class BatteryTestDesignScenarioTests
{
    private const double Precision = 1e-6;

    /// <summary>Mirrors how the API deserializes a request body.</summary>
    private static readonly JsonSerializerOptions WireOptions = new() { PropertyNameCaseInsensitive = true };

    private static BatteryAllocationService Allocator(BatterySettings? settings = null)
        => new(Options.Create(settings ?? new BatterySettings()));

    /// <summary>Excel base loads: Transit propulsion 11463 (SM 0), hotel 3800 ⇒ ΣH = 649.15.</summary>
    private static CalculatorInput ExcelLoads(double batteryPowerKw = 1260)
        => CalculatorInputBuilder.Default()
            .WithPropulsionPower(11463).WithSeaMargin(0)
            .WithTransitMode(5000, 3800)
            .WithBattery(batteryPowerKw, 2000, OperationalMode.Transit)
            .Build();

    /// <summary>Excel plant "EP": sized so the pipeline has a valid combination.</summary>
    private static CalculatorInputBuilder ExcelPlant() => CalculatorInputBuilder.Default()
        .WithMainEngines(24000, 2)
        .WithShaftGenerators(1000)
        .WithAuxiliaryEngines(800, 3)
        .WithPropulsionPower(11463).WithSeaMargin(0)
        .WithTransitMode(5000, 3800);

    // ═══ Family A — allocation engine ════════════════════════════════════════

    [Fact]
    public void A2_BudgetExhaustsMidRow_PartialCoverageOnPropulsion()
    {
        var result = Allocator().Allocate(OperationalMode.Transit, ExcelLoads(batteryPowerKw: 300));

        var prop = result.Loads.Single(l => l.Load == BatteryLoadType.Propulsion);
        prop.BatteryUsedKw.Should().BeApproximately(300, Precision);
        prop.CoveredBandKw.Should().BeApproximately(105, Precision);          // 300 × 0.35
        prop.UncoveredReserveKw.Should().BeApproximately(468.15, Precision);  // 573.15 − 105

        var hotel = result.Loads.Single(l => l.Load == BatteryLoadType.Hotel);
        hotel.BatteryUsedKw.Should().Be(0);
        hotel.UncoveredReserveKw.Should().BeApproximately(76, Precision);

        result.PeakShavingBandKw.Should().BeApproximately(105, Precision);
        result.AdditionalSpinningReserveKw.Should().BeApproximately(544.15, Precision);
        result.RemainingBatteryKw.Should().Be(0);
    }

    [Fact]
    public void A3_BudgetExactlyFirstRowVariation_SecondRowUncovered()
    {
        var result = Allocator().Allocate(OperationalMode.Transit, ExcelLoads(batteryPowerKw: 573.15));

        var prop = result.Loads.Single(l => l.Load == BatteryLoadType.Propulsion);
        prop.BatteryUsedKw.Should().BeApproximately(573.15, Precision);
        prop.CoveredBandKw.Should().BeApproximately(200.6025, Precision);
        prop.UncoveredReserveKw.Should().BeApproximately(372.5475, Precision);

        result.Loads.Single(l => l.Load == BatteryLoadType.Hotel).BatteryUsedKw.Should().Be(0);
        result.AdditionalSpinningReserveKw.Should().BeApproximately(448.5475, Precision); // 372.5475 + 76
        result.RemainingBatteryKw.Should().BeApproximately(0, Precision);
    }

    [Fact]
    public void A5_SaturatedBudget_ReserveIndependentOfBudget_INV2()
    {
        var saturated = Allocator().Allocate(OperationalMode.Transit, ExcelLoads(batteryPowerKw: 10_000));
        var reference = Allocator().Allocate(OperationalMode.Transit, ExcelLoads(batteryPowerKw: 1260));

        saturated.CommittedBatteryKw.Should().BeApproximately(649.15, Precision);
        saturated.RemainingBatteryKw.Should().BeApproximately(9350.85, Precision);
        // INV-2: beyond saturation, ΣJ/ΣL are budget-independent (coverage < 100% per PS row)
        saturated.PeakShavingBandKw.Should().BeApproximately(reference.PeakShavingBandKw, Precision);
        saturated.AdditionalSpinningReserveKw.Should().BeApproximately(reference.AdditionalSpinningReserveKw, Precision);
        saturated.AdditionalSpinningReserveKw.Should().BeApproximately(444.7475, Precision);
    }

    [Fact]
    public void A6_PortModeSmallHotel_MatchesLiveUserScenario()
    {
        var input = CalculatorInputBuilder.Default()
            .WithBattery(70, 60, OperationalMode.Port).Build();
        input.PortHotelPowerKW = 155;

        var result = Allocator().Allocate(OperationalMode.Port, input);

        var hotel = result.Loads.Single(l => l.Load == BatteryLoadType.Hotel);
        hotel.VariationKw.Should().BeApproximately(3.1, Precision);           // 155 × 0.02
        hotel.BatteryUsedKw.Should().BeApproximately(3.1, Precision);
        hotel.CoveredBandKw.Should().BeApproximately(0.155, Precision);      // × 0.05
        hotel.UncoveredReserveKw.Should().BeApproximately(2.945, Precision);
        result.RemainingBatteryKw.Should().BeApproximately(66.9, Precision);
    }

    [Fact]
    public void A7_DpMode_HotelRowNumbers()
    {
        var input = CalculatorInputBuilder.Default()
            .WithDPMode(2000, 1500, 4000)
            .WithBattery(500, 1000, OperationalMode.DP).Build();

        var result = Allocator().Allocate(OperationalMode.DP, input);

        result.Loads.Should().HaveCount(4); // DpReserve, DpDemand, Mission, Hotel
        result.Loads.Where(l => l.Load != BatteryLoadType.Hotel)
            .Should().OnlyContain(l => l.VariationKw == 0); // no variation inputs yet
        var hotel = result.Loads.Single(l => l.Load == BatteryLoadType.Hotel);
        hotel.VariationKw.Should().BeApproximately(30, Precision);           // 1500 × 0.02
        hotel.CoveredBandKw.Should().BeApproximately(1.5, Precision);
        hotel.UncoveredReserveKw.Should().BeApproximately(28.5, Precision);
        result.CommittedBatteryKw.Should().BeApproximately(30, Precision);
        result.RemainingBatteryKw.Should().BeApproximately(470, Precision);
    }

    [Fact]
    public void A11_SailAdjustedPropulsionOverride_FullNumbers()
    {
        var result = Allocator().Allocate(OperationalMode.Transit, ExcelLoads(), propulsionOverrideKw: 10_000);

        var prop = result.Loads.Single(l => l.Load == BatteryLoadType.Propulsion);
        prop.AverageLoadKw.Should().Be(10_000);
        prop.VariationKw.Should().BeApproximately(500, Precision);           // 10000 × 0.05
        prop.BatteryUsedKw.Should().BeApproximately(500, Precision);
        prop.CoveredBandKw.Should().BeApproximately(175, Precision);         // × 0.35
        prop.UncoveredReserveKw.Should().BeApproximately(325, Precision);

        result.PeakShavingBandKw.Should().BeApproximately(178.8, Precision); // 175 + 3.8
        result.AdditionalSpinningReserveKw.Should().BeApproximately(397.2, Precision); // 325 + 72.2
    }

    [Fact]
    public void A12_CustomPriorityOrder_FullRowValues()
    {
        var settings = new BatterySettings { LoadPriorities = BatterySettings.CreateDefaultLoadPriorities() };
        var hotel = settings.LoadPriorities.Single(p => p.Load == BatteryLoadType.Hotel);
        settings.LoadPriorities.Remove(hotel);
        settings.LoadPriorities.Insert(0, hotel);

        var result = Allocator(settings).Allocate(OperationalMode.Transit, ExcelLoads(batteryPowerKw: 100));

        var hotelRow = result.Loads.Single(l => l.Load == BatteryLoadType.Hotel);
        hotelRow.BatteryUsedKw.Should().BeApproximately(76, Precision);
        hotelRow.CoveredBandKw.Should().BeApproximately(3.8, Precision);
        hotelRow.UncoveredReserveKw.Should().BeApproximately(72.2, Precision);

        var propRow = result.Loads.Single(l => l.Load == BatteryLoadType.Propulsion);
        propRow.BatteryUsedKw.Should().BeApproximately(24, Precision);       // 100 − 76
        propRow.CoveredBandKw.Should().BeApproximately(8.4, Precision);      // 24 × 0.35
        propRow.UncoveredReserveKw.Should().BeApproximately(564.75, Precision); // 573.15 − 8.4
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(300)]
    [InlineData(573.15)]
    [InlineData(649.15)]
    [InlineData(1260)]
    [InlineData(10_000)]
    public void A13_Invariant_CoveredPlusUncoveredEqualsTotalVariation_ForAnyBudget(double budgetKw)
    {
        var result = Allocator().Allocate(OperationalMode.Transit, ExcelLoads(batteryPowerKw: budgetKw));

        var totalVariation = result.Loads.Sum(l => l.VariationKw);
        var coveredPlusUncovered = result.Loads.Sum(l => l.CoveredBandKw + l.UncoveredReserveKw);

        totalVariation.Should().BeApproximately(649.15, Precision);
        coveredPlusUncovered.Should().BeApproximately(totalVariation, Precision); // INV-1
        result.Loads.Should().OnlyContain(l => l.BatteryUsedKw <= l.VariationKw + 1e-9);
    }

    // ═══ Family B — pipeline wiring ══════════════════════════════════════════

    [Fact]
    public async Task B1_ExcelPlantEndToEnd_CombinationPowersAndLoadsPinned()
    {
        var factory = TestServiceFactory.Create();
        var input = ExcelPlant().WithBattery(1260, 2000, OperationalMode.Transit).Build();

        var result = await factory.CalculatorService.CalculateAllVariantsAsync(input);

        var l1 = result.Level1Details!;
        l1.ValidCombinationsCount.Should().Be(1); // only {2 ME, SG, 3 AE} carries the adjusted loads
        l1.SelectedBaselineIndex.Should().Be(0);  // max(0, 1 − 3)

        // Adjusted demand: propulsion 11463 + 372.5475, hotel 3800 + 72.2; ME drives prop + SG(2000)
        l1.BaselineMePowerKw.Should().BeApproximately(13_835.5475, Precision);
        l1.BaselineSgPowerKw.Should().BeApproximately(2000, Precision);
        l1.BaselineAePowerKw.Should().BeApproximately(1872.2, Precision);           // 3872.2 − 2000
        l1.BaselineMeLoadPercent.Should().BeApproximately(28.824057291666668, 1e-9); // /48000 ×100
        l1.BaselineAeLoadPercent.Should().BeApproximately(78.00833333333334, 1e-9);  // /2400 ×100

        result.BatteryDetails!.SpinningReserveKw.Should().BeApproximately(444.7475, Precision);
        result.BatteryDetails.PeakShavingKw.Should().BeApproximately(204.4025, Precision);
    }

    [Fact]
    public async Task B5_PortOnlyBattery_WithPortHours_AllocatesPortAndLeavesTransitUntouched()
    {
        var factory = TestServiceFactory.Create();

        var noBattery = CalculatorInputBuilder.Default().Build();
        noBattery.PortHours = 1000;
        noBattery.PortHotelPowerKW = 155;

        var withBattery = CalculatorInputBuilder.Default()
            .WithBattery(70, 60, OperationalMode.Port).Build();
        withBattery.PortHours = 1000;
        withBattery.PortHotelPowerKW = 155;

        var baseline = await factory.CalculatorService.CalculateAllVariantsAsync(noBattery);
        var result = await factory.CalculatorService.CalculateAllVariantsAsync(withBattery);

        result.BatteryDetails.Should().NotBeNull();
        result.BatteryDetails!.ModeAllocations.Should().ContainSingle(a => a.Mode == OperationalMode.Port);
        result.BatteryDetails.SpinningReserveKw.Should().BeApproximately(2.945, Precision);
        result.BatteryDetails.PeakShavingKw.Should().BeApproximately(0.155, Precision);

        // Transit (battery-irrelevant mode) is bit-identical
        result.Level1Details!.OptimalFocTonPerHour
            .Should().Be(baseline.Level1Details!.OptimalFocTonPerHour);
    }

    [Fact]
    public async Task B6_MultiModeBattery_SumsAcrossModes()
    {
        var factory = TestServiceFactory.Create();
        var input = ExcelPlant()
            .WithBattery(1260, 2000, OperationalMode.Transit, OperationalMode.Port).Build();
        input.PortHours = 1000;
        input.PortHotelPowerKW = 155;

        var result = await factory.CalculatorService.CalculateAllVariantsAsync(input);

        var details = result.BatteryDetails!;
        details.ModeAllocations.Should().HaveCount(2);
        // Documented semantics (gate battery.b): headline figures are summed across relevant modes
        details.SpinningReserveKw.Should().BeApproximately(444.7475 + 2.945, Precision);
        details.PeakShavingKw.Should().BeApproximately(204.4025 + 0.155, Precision);
        details.BenefitFocTonPerYear.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task B7_DpBattery_AllocatesDpMode()
    {
        var factory = TestServiceFactory.Create();
        var input = ExcelPlant()
            .WithDPMode(2000, 1500, 4000)
            .WithBattery(500, 1000, OperationalMode.DP).Build();

        var result = await factory.CalculatorService.CalculateAllVariantsAsync(input);

        var details = result.BatteryDetails!;
        details.ModeAllocations.Should().ContainSingle(a => a.Mode == OperationalMode.DP);
        details.SpinningReserveKw.Should().BeApproximately(28.5, Precision); // A7 numbers
        details.PeakShavingKw.Should().BeApproximately(1.5, Precision);
        details.BenefitFocTonPerYear.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task B8_BatteryWithSail_AllocationUsesSailAdjustedPropulsion()
    {
        var factory = TestServiceFactory.Create(enableSailData: true);
        var input = ExcelPlant()
            .WithSailEnabled(10, 90)
            .WithBattery(1260, 2000, OperationalMode.Transit).Build();

        var result = await factory.CalculatorService.CalculateAllVariantsAsync(input);

        result.SailContribution.Should().NotBeNull();
        var transitAlloc = result.BatteryDetails!.ModeAllocations.Single(a => a.Mode == OperationalMode.Transit);
        var propRow = transitAlloc.Loads.Single(l => l.Load == BatteryLoadType.Propulsion);
        // QA carry-over #3: the allocation must see the sail-adjusted propulsion, not the raw value
        propRow.AverageLoadKw.Should().BeApproximately(
            result.SailContribution!.TransitPropulsionAfterKw, Precision);
        propRow.AverageLoadKw.Should().NotBe(input.EffectivePropulsionPower);
    }

    // ═══ Family C — PTI boundaries & transparency ════════════════════════════

    /// <summary>Deficit plant DP1: ME 2×5000, SG 500/e, AE 3×800, prop 9200, hotel 2000 ⇒ deficit 200.</summary>
    private static CalculatorInputBuilder DeficitPlant() => CalculatorInputBuilder.Default()
        .WithMainEngines(5000, 2)
        .WithShaftGenerators(500)
        .WithAuxiliaryEngines(800, 3)
        .WithPropulsionPower(9200).WithSeaMargin(0)
        .WithTransitMode(5000, 2000);

    [Fact]
    public async Task C4_DischargeGate_BandExactlyEqualToHeadroom_IsKept()
    {
        var factory = TestServiceFactory.Create();
        var input = DeficitPlant().Build();
        input.MaxPtiPerEngineKw = 500; // headroom after assist = 1000 − 200 = 800

        var adjustment = new BatteryL1Adjustment(0, 0, PropulsionPeakShavingKw: 800); // == headroom
        var result = factory.Level1Service.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit, batteryAdjustment: adjustment);

        result.AllValidCombinations.Should().Contain(c => c.PtiPowerKw > 0);
    }

    [Fact]
    public async Task C7_CustomPtiLossFactor_IsConfigDriven()
    {
        var factory = TestServiceFactory.Create();
        var level1 = new Level1OptimizationService(Options.Create(new BatterySettings { PtiLossFactor = 0.10 }));
        var input = DeficitPlant().Build();
        input.MaxPtiPerEngineKw = 500;

        var result = level1.FindOptimalCombination(input, factory.Curves, OperationalMode.Transit);

        var combo = result.AllValidCombinations.Single(c => c.PtiPowerKw > 0 && c.ActiveAeCount == 3);
        combo.AePowerKw.Should().BeApproximately(1000 + 200 * 1.10, Precision); // 1220, not 1210
    }

    [Fact]
    public async Task C8_PtiKw_ExposedInValidCombinationDto()
    {
        var factory = TestServiceFactory.Create();

        var deficit = DeficitPlant().Build();
        deficit.MaxPtiPerEngineKw = 500;
        var withPti = Level1DetailsBuilder.Build(
            factory.Level1Service.FindOptimalCombination(deficit, factory.Curves, OperationalMode.Transit));
        withPti.ValidCombinations.Should().NotBeEmpty();
        withPti.ValidCombinations.Should().OnlyContain(c => c.PtiKw != null && Math.Abs(c.PtiKw.Value - 200) < 1e-6);

        // ExcelPlant has no ME deficit in any valid combo (24 000 kW engines) ⇒ ptiKw stays null
        var noDeficit = ExcelPlant().Build();
        noDeficit.MaxPtiPerEngineKw = 500;
        var withoutPti = Level1DetailsBuilder.Build(
            factory.Level1Service.FindOptimalCombination(noDeficit, factory.Curves, OperationalMode.Transit));
        withoutPti.ValidCombinations.Should().OnlyContain(c => c.PtiKw == null);
    }

    // ═══ Family D — L3 residual variation ════════════════════════════════════

    [Fact]
    public async Task D5_OnlyHotelSideBand_OffsetsDrcVariation_NotPropulsionBand()
    {
        var factory = TestServiceFactory.Create();
        var input = ExcelPlant().WithAuxiliaryEngines(2000, 3)
            .WithBattery(1260, 2000, OperationalMode.Transit).Build();
        input.HotelLoadVariationKw = 500;

        var result = await factory.CalculatorService.CalculateAllVariantsAsync(input);

        var l3 = result.Premium.Level3Details!;
        // Hotel band = 76 × 0.05 = 3.8; the propulsion band (200.6025) must NOT offset DRC
        l3.BatteryShavedVariationKw.Should().BeApproximately(3.8, Precision);
        l3.VariationPerGeneratorKw.Should().BeApproximately(496.2, Precision);
        l3.BatteryShavedVariationKw.Should().NotBeApproximately(204.4025, 1);
    }

    [Fact]
    public async Task D6_VesselTypeVariationLookup_CombinedWithBatteryBand()
    {
        var factory = TestServiceFactory.Create();
        var input = ExcelPlant().WithAuxiliaryEngines(2000, 3)
            .WithBattery(1260, 2000, OperationalMode.Transit).Build();
        input.HotelLoadVariationKw = null;          // force the vessel-type lookup
        input.VesselTypeName = "Container 5000 TEU"; // substring-matches "Container" ⇒ ±1500

        var result = await factory.CalculatorService.CalculateAllVariantsAsync(input);

        var l3 = result.Premium.Level3Details!;
        l3.BatteryShavedVariationKw.Should().BeApproximately(3.8, Precision);
        l3.VariationPerGeneratorKw.Should().BeApproximately(1496.2, Precision); // 1500 − 3.8
    }

    // ═══ Family E — validation boundaries ════════════════════════════════════

    [Fact]
    public void E5_CapacityExactlyHalfPower_NoThirtyMinuteWarning()
    {
        var service = new ValidationService();
        var input = CalculatorInputBuilder.Default()
            .WithBattery(1000, 500, OperationalMode.Transit).Build(); // 500 == 1000 × 0.5

        var result = service.ValidateInput(input);

        result.Valid.Should().BeTrue();
        result.Warnings.Should().NotContain(w => w.Type == "battery");
    }

    [Fact]
    public void E6_LiveUserConfiguration_70kW_60kWh_NoBatteryWarnings()
    {
        var service = new ValidationService();
        var input = CalculatorInputBuilder.Default()
            .WithBattery(70, 60, OperationalMode.Port).Build(); // 60 ≥ 35

        var result = service.ValidateInput(input);

        result.Valid.Should().BeTrue();
        result.Warnings.Should().NotContain(w => w.Type == "battery");
    }

    // ═══ Family G — legacy stub guard ════════════════════════════════════════

    /// <summary>
    /// G4 used to assert that the legacy <c>batteryCapacity</c> field changed nothing whatever its
    /// value. The field has since been removed from <see cref="CalculatorInput"/> entirely, so the
    /// guarantee is now structural rather than tested: the client may still send the property and
    /// JSON deserialization ignores it.
    ///
    /// What remains worth asserting is the other half — that an unknown property on the wire does
    /// not break the request.
    /// </summary>
    [Fact]
    public async Task G4_AnUnknownWireProperty_IsIgnoredRatherThanRejected()
    {
        var factory = TestServiceFactory.Create();

        var json = JsonSerializer.Serialize(CalculatorInputBuilder.Default().Build());
        var withLegacyField = json.TrimEnd('}') + ",\"batteryCapacity\":999999,\"hotelLoad\":123,\"sailInstalled\":true}";

        var revived = JsonSerializer.Deserialize<CalculatorInput>(withLegacyField, WireOptions)!;

        var baseline = await factory.CalculatorService.CalculateAllVariantsAsync(
            CalculatorInputBuilder.Default().Build());
        var withExtras = await factory.CalculatorService.CalculateAllVariantsAsync(revived);

        withExtras.BaselineFOC.Should().Be(baseline.BaselineFOC,
            "removed fields are ignored on the wire — saved profiles keep loading");
        withExtras.Premium.FuelSavings.Should().Be(baseline.Premium.FuelSavings);
        withExtras.BatteryDetails.Should().BeNull();
    }
}
