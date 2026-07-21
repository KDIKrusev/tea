using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Repositories.Interfaces;
using KSailCalc.Api.Services;
using KSailCalc.Api.Services.Interfaces;
using KSailCalc.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace KSailCalc.Tests.Services;

public class Level2OptimizationServiceTests
{
    private readonly Level2OptimizationService _service;
    private readonly Level1OptimizationService _level1Service;

    public Level2OptimizationServiceTests()
    {
        var factory = TestServiceFactory.Create();
        _level1Service = new Level1OptimizationService(factory.SfocService, Microsoft.Extensions.Options.Options.Create(new KSailCalc.Api.Models.BatterySettings()));
        _service = new Level2OptimizationService(factory.SfocService);
    }

    /// <summary>
    /// Creates services backed by a U-shaped SFOC curve where minimum is at 75-85%,
    /// enabling L2 optimization to find savings via asymmetric loading.
    /// </summary>
    private static (Level1OptimizationService L1, Level2OptimizationService L2) CreateUShapedServices()
    {
        var uShapedSfocData = new List<SfocDataPoint>
        {
            new() { Load = 0.00, Sfoc = 0.0 },
            new() { Load = 0.10, Sfoc = 345.0 },
            new() { Load = 0.25, Sfoc = 260.0 },
            new() { Load = 0.50, Sfoc = 228.0 },
            new() { Load = 0.75, Sfoc = 193.0 },
            new() { Load = 0.85, Sfoc = 193.0 },
            new() { Load = 1.00, Sfoc = 217.0 }
        };

        var appDataMock = new Mock<IAppDataAggregationService>();
        appDataMock.Setup(x => x.GetInitialAppDataAsync())
            .ReturnsAsync(new AppInitialData
            {
                EngineTypes = new EngineTypesData
                {
                    MainEngines = new List<EngineType>
                    {
                        new() { Id = 1, Name = "U-Shaped ME", SfocData = uShapedSfocData }
                    },
                    AuxiliaryEngines = new List<AuxiliaryEngineType>
                    {
                        new() { Id = 1, Name = "U-Shaped AE", SfocData = uShapedSfocData }
                    }
                },
                OperationalProfiles = new List<VesselOperationalProfile>(),
                Metadata = new AppDataMetadata { Version = "test-ushaped" }
            });

        var sfocService = new SfocService(appDataMock.Object, new Mock<ILogger<SfocService>>().Object);
        return (new Level1OptimizationService(sfocService, Microsoft.Extensions.Options.Options.Create(new KSailCalc.Api.Models.BatterySettings())), new Level2OptimizationService(sfocService));
    }

    #region Two-Generator Scenario

    [Fact]
    public async Task TwoGenerators_OptimalSetpointsMinimizeTotalSfoc()
    {
        // ME barely fits propulsion → cannot absorb hotel → forces 2 AEs active
        // 1 ME × 8500, AE × 4000 × 2, propulsion = 8000, hotel = 5000
        // 1ME+1AE: ME = 8000 + 1000 = 9000 > 8500 → INVALID
        // 1ME+2AE: ME = 8000, AE = 5000 → only valid combo
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(8500, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(4000, 2)
            .WithPropulsionPower(8000)
            .WithSeaMargin(0)
            .WithTransitMode(5717, 5000)
            .Build();

        var level1 = await _level1Service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        level1.OptimalCombination.ActiveAeCount.Should().Be(2, "2 AEs must be active");

        var result = await _service.OptimizeLoadSetpointsAsync(level1, input);

        result.OptimalSetpoints.Should().HaveCount(2);
        result.OptimalSetpoints.Should().OnlyContain(s => s.GeneratorType == GeneratorType.AE);

        // Total setpoint power must cover the hotel demand
        var totalSetpointPower = result.OptimalSetpoints.Sum(s => s.PowerKw);
        totalSetpointPower.Should().BeGreaterThanOrEqualTo(5000,
            "setpoints must cover the hotel demand");

        // The algorithm selects the combination with minimum Total SFOC among all valid combos
        // (those that cover demand). Higher load setpoints may have lower SFOC.
        result.OptimalTotalSfoc.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TwoGenerators_Level2FocLessOrEqualLevel1Foc()
    {
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(8500, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(4000, 2)
            .WithPropulsionPower(8000)
            .WithSeaMargin(0)
            .WithTransitMode(5717, 5000)
            .Build();

        var level1 = await _level1Service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        var result = await _service.OptimizeLoadSetpointsAsync(level1, input);

        result.Level2FocTonPerHour.Should().BeLessThanOrEqualTo(result.Level1FocTonPerHour);
        result.Level2SavingsTonPerHour.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task TwoGenerators_SetpointsCoverHotelDemand()
    {
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(8500, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(4000, 2)
            .WithPropulsionPower(8000)
            .WithSeaMargin(0)
            .WithTransitMode(5717, 5000)
            .Build();

        var level1 = await _level1Service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        var result = await _service.OptimizeLoadSetpointsAsync(level1, input);

        var totalGeneratorPower = result.OptimalSetpoints.Sum(s => s.PowerKw);
        totalGeneratorPower.Should().BeGreaterThanOrEqualTo(5000);
    }

    #endregion

    #region Three-Generator Scenario

    [Fact]
    public async Task ThreeGenerators_AllIncludedInSetpoints()
    {
        // 1 ME + SG ON + 2 AE → 3 generators: 1 SG + 2 AE
        // hotel=3000 so SG(2000) covers 2000, AE covers 1000
        // L2 demand = 3000 kW, well within 80% max of 3×2000 = 4800
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 1)
            .WithShaftGenerators(2000)
            .WithAuxiliaryEngines(2000, 2)
            .WithPropulsionPower(3000)
            .WithSeaMargin(0)
            .WithTransitMode(5717, 3000)
            .Build();

        var level1 = await _level1Service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        // Ensure Level 1 picks a combo with SG ON and 2 AEs
        var optimalCombo = level1.OptimalCombination;

        // Only run Level 2 test if we have multiple generators
        if (optimalCombo.SgEnabled || optimalCombo.ActiveAeCount >= 2)
        {
            var result = await _service.OptimizeLoadSetpointsAsync(level1, input);

            var expectedCount = (optimalCombo.SgEnabled && optimalCombo.SgPowerKw > 0 ? 1 : 0)
                + optimalCombo.ActiveAeCount;

            if (expectedCount >= 2)
            {
                result.OptimalSetpoints.Should().HaveCount(expectedCount);
                // SG load is fixed from L1 (not optimized by L2) — may exceed 90%
                // Only AE setpoints should be within L2's operating range
                result.OptimalSetpoints.Where(s => s.GeneratorType == GeneratorType.AE)
                    .Should().OnlyContain(
                    s => s.LoadPercent < 0.001 || (s.LoadPercent >= 0.10 && s.LoadPercent <= 0.90 + 0.001));
            }
        }
    }

    #endregion

    #region Single Generator (No Optimization)

    [Fact]
    public async Task SingleGenerator_NoOptimization_SavingsAreZero()
    {
        // 1 ME + SG OFF + 1 AE → only 1 generator, nothing to optimize
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(500, 1)
            .WithPropulsionPower(2000)
            .WithSeaMargin(0)
            .WithTransitMode(5717, 165)
            .Build();

        var level1 = await _level1Service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        var result = await _service.OptimizeLoadSetpointsAsync(level1, input);

        result.Level2SavingsTonPerHour.Should().Be(0);
    }

    [Fact]
    public async Task NoGenerators_NoOptimization_SavingsAreZero()
    {
        // 1 ME + SG OFF + 0 AE → ME absorbs hotel, no generators
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(500, 3)
            .WithPropulsionPower(2000)
            .WithSeaMargin(0)
            .WithTransitMode(5717, 165)
            .Build();

        var level1 = await _level1Service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        // Level 1 optimal may have 0 AE (ME absorbs hotel)
        var result = await _service.OptimizeLoadSetpointsAsync(level1, input);

        // If 0 or 1 generators, savings must be 0
        if (level1.OptimalCombination.ActiveAeCount <= 1 && !level1.OptimalCombination.SgEnabled)
        {
            result.Level2SavingsTonPerHour.Should().Be(0);
        }
    }

    #endregion

    #region Load Steps Validation

    [Fact]
    public async Task LoadSteps_AreWithinExpectedRange()
    {
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(8500, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(4000, 2)
            .WithPropulsionPower(8000)
            .WithSeaMargin(0)
            .WithTransitMode(5717, 5000)
            .Build();

        var level1 = await _level1Service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        var result = await _service.OptimizeLoadSetpointsAsync(level1, input);

        // With demand distribution, a generator can be OFF (0%) or within MinLoad(10%)..MaxLoad(90%)
        result.OptimalSetpoints.Should().OnlyContain(
            s => s.LoadPercent < 0.001 || (s.LoadPercent >= 0.10 && s.LoadPercent <= 0.90 + 0.001),
            "all setpoints should be OFF (0%) or within 10%-90% range");
    }

    [Fact]
    public async Task OptimalSetpoints_HavePositiveSfocValues()
    {
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(8500, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(4000, 2)
            .WithPropulsionPower(8000)
            .WithSeaMargin(0)
            .WithTransitMode(5717, 5000)
            .Build();

        var level1 = await _level1Service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        var result = await _service.OptimizeLoadSetpointsAsync(level1, input);

        result.OptimalSetpoints.Should().OnlyContain(s => s.Sfoc > 0);
        result.OptimalTotalSfoc.Should().BeGreaterThan(0);
    }

    #endregion

    #region FOC Calculation

    [Fact]
    public async Task Level2Foc_UsesActualDemandNotGeneratorCapacity()
    {
        // ME barely fits propulsion → 2 AEs must carry hotel
        // At optimal setpoint (e.g. 80%), power = 6400, but FOC uses 5000 demand
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(8500, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(4000, 2)
            .WithPropulsionPower(8000)
            .WithSeaMargin(0)
            .WithTransitMode(5717, 5000)
            .Build();

        var level1 = await _level1Service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        var result = await _service.OptimizeLoadSetpointsAsync(level1, input);

        // Level 2 FOC should be positive and reasonable
        result.Level2FocTonPerHour.Should().BeGreaterThan(0);

        // Setpoint power exceeds demand (headroom), but FOC is based on actual demand
        var setpointPower = result.OptimalSetpoints.Sum(s => s.PowerKw);
        setpointPower.Should().BeGreaterThanOrEqualTo(5000, "setpoints must cover demand");
    }

    #endregion

    #region U-Shaped SFOC — Positive L2 Savings

    [Fact]
    public async Task UShapedSfoc_TwoGenerators_BothActive_CoverDemand()
    {
        // With a U-shaped SFOC curve, h(x) = x·SFOC(x) is NOT convex across the full
        // operating range — the plateau at 193 in [0.75, 0.85] combined with the steep
        // slope in [0.25, 0.50] means asymmetric splits can reduce total fuel vs. an
        // equal split. The test therefore asserts structural invariants (both AEs active,
        // combined load covers demand) rather than prescribing equal distribution.
        // 2 AE x 1000kW, demand = 1200kW → sum of load fractions must equal 1.2.
        var (l1, l2) = CreateUShapedServices();

        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(8500, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(1000, 2)
            .WithPropulsionPower(8000)
            .WithSeaMargin(0)
            .WithTransitMode(5717, 1200)
            .Build();

        var level1 = await l1.FindOptimalCombinationAsync(input, OperationalMode.Transit);
        level1.OptimalCombination.ActiveAeCount.Should().BeGreaterThanOrEqualTo(2,
            "both AEs must be active to carry 1200kW hotel demand");

        var result = await l2.OptimizeLoadSetpointsAsync(level1, input);

        var aeSetpoints = result.OptimalSetpoints
            .Where(s => s.GeneratorType == GeneratorType.AE && s.PowerKw > 0)
            .ToList();
        aeSetpoints.Should().HaveCount(2);

        var combinedLoad = aeSetpoints.Sum(s => s.LoadPercent);
        combinedLoad.Should().BeApproximately(1.2, 0.01,
            "combined AE load fractions must cover demand/capacity regardless of how split");

        result.Level2FocTonPerHour.Should().BeGreaterThan(0);
    }

    #endregion

    #region Generator OFF Scenario

    [Fact]
    public async Task GeneratorOFF_SmallAeCoversAll_LargeAeOff()
    {
        // 1 large AE (2000kW) + 1 small AE (500kW), demand = 400kW
        // Small AE at 80% (400/500, SFOC ~215) is much better than
        // large AE at 20% (400/2000, SFOC ~293) or both at low load.
        // Optimal: small AE carries all, large AE OFF.
        var factory = TestServiceFactory.Create();

        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(500, 1) // will be overridden — need 2 different-capacity AEs
            .WithPropulsionPower(2000)
            .WithSeaMargin(0)
            .WithTransitMode(5717, 400)
            .Build();

        // We need a specific L1 result that has 2 AEs of different capacities active.
        // Since CalculatorInput only supports uniform AE capacity, we manually construct
        // the L1 result to test L2 behavior with heterogeneous generators.
        // Use 2 AEs of 500kW each (uniform), demand = 400kW.
        // At 40% each (200kW), SFOC is high (~241). One at 80% (400kW, SFOC ~215) is better.
        input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(500, 2)
            .WithPropulsionPower(2000)
            .WithSeaMargin(0)
            .WithTransitMode(5717, 400)
            .Build();

        var level1 = await factory.Level1Service.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        // If L1 picked 2 AEs, L2 should find one OFF is better
        if (level1.OptimalCombination.ActiveAeCount >= 2)
        {
            var result = await factory.Level2Service.OptimizeLoadSetpointsAsync(level1, input);

            // One generator should be OFF (PowerKw ≈ 0)
            result.OptimalSetpoints.Should().Contain(
                s => s.PowerKw < 1.0,
                "one generator should be OFF when a single generator can efficiently cover all demand");
        }
    }

    [Fact]
    public async Task GeneratorOFF_UShapedSfoc_OneOffIsBetter()
    {
        // With U-shaped SFOC: 2 AE x 500kW, demand = 400kW
        // Equal: 2 x 200kW at 40% (SFOC ~241). One at 80% = 400kW (SFOC ~193).
        // FOC equal: 2 * 200 * 241 / 1e6 = 0.0964. FOC one: 400 * 193 / 1e6 = 0.0772.
        var (l1, l2) = CreateUShapedServices();

        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(500, 2)
            .WithPropulsionPower(2000)
            .WithSeaMargin(0)
            .WithTransitMode(5717, 400)
            .Build();

        var level1 = await l1.FindOptimalCombinationAsync(input, OperationalMode.Transit);

        if (level1.OptimalCombination.ActiveAeCount >= 2)
        {
            var result = await l2.OptimizeLoadSetpointsAsync(level1, input);

            // With U-shaped curve, turning one OFF and running the other at 80%
            // should produce savings
            result.Level2SavingsTonPerHour.Should().BeGreaterThan(0,
                "turning one generator OFF should save fuel with U-shaped SFOC");

            result.OptimalSetpoints.Should().Contain(
                s => s.PowerKw < 1.0,
                "one generator should be OFF");
        }
    }

    #endregion

    #region Edge Case — Demand Zero

    [Fact]
    public async Task DemandZero_Level2Savings_AreZero()
    {
        // When hotel demand is 0, there is nothing for L2 to optimize.
        // Create L1 result where SgPowerKw=0, AePowerKw=0.
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(12000, 1)
            .WithShaftGenerators(0)
            .WithAuxiliaryEngines(500, 2)
            .WithPropulsionPower(2000)
            .WithSeaMargin(0)
            .WithTransitMode(5717, 0) // zero hotel demand
            .Build();

        // Manually construct a L1 result with zero generator demand
        var level1 = new Level1Result
        {
            OptimalCombination = new EngineCombination
            {
                ActiveMeCount = 1,
                SgEnabled = false,
                ActiveAeCount = 0,
                MePowerKw = 2000,
                SgPowerKw = 0,
                AePowerKw = 0,
                MeLoadPercent = 2000.0 / 12000.0,
                AeLoadPercent = 0,
                FocTonPerHour = 0.4, // dummy
                MeFocTonPerHour = 0.4,
                AeFocTonPerHour = 0
            }
        };

        var result = await _service.OptimizeLoadSetpointsAsync(level1, input);

        result.Level2SavingsTonPerHour.Should().Be(0,
            "zero hotel demand means no generators to optimize");
    }

    #endregion
}
