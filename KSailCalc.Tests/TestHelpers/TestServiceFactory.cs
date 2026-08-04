using KSailCalc.Api.Models;
using KSailCalc.Api.Repositories.Interfaces;
using KSailCalc.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace KSailCalc.Tests.TestHelpers;

/// <summary>
/// Wires up the real service chain with mocked repositories for testing.
/// 
/// CalculatorService → Level1/2/3 Services → SfocService + SailContributionService
///                                                ↓              ↓
///                                              AppData (mock)  SailContribRepo (mock)
/// </summary>
public class TestServiceFactory
{
    public static readonly List<IntegrationLevelConfig> DefaultIntegrationLevels = new()
    {
        new() { IntegrationLevelId = 1, IemsPriceNOK = 1_000_000, CommissioningNOK = 100_000 },
        new() { IntegrationLevelId = 2, IemsPriceNOK = 1_205_000, CommissioningNOK = 200_000 },
        new() { IntegrationLevelId = 3, IemsPriceNOK = 1_800_000, CommissioningNOK = 300_000 }
    };

    // REAL DATA: Level1_Optimal Power System Setup.xlsx → Main Engine SFOC Diesel (g/kWH)
    public static readonly List<EngineType> DefaultMainEngines = new()
    {
        new()
        {
            Id = 1, Name = "Test ME",
            SfocData = new()
            {
                new() { Load = 0.00, Sfoc = 0.0 },
                new() { Load = 0.10, Sfoc = 284.533 },
                new() { Load = 0.20, Sfoc = 255.791 },
                new() { Load = 0.30, Sfoc = 235.422 },
                new() { Load = 0.40, Sfoc = 218.615 },
                new() { Load = 0.50, Sfoc = 214.097 },
                new() { Load = 0.60, Sfoc = 208.458 },
                new() { Load = 0.70, Sfoc = 203.592 },
                new() { Load = 0.80, Sfoc = 197.544 },
                new() { Load = 0.90, Sfoc = 201.096 },
                new() { Load = 1.00, Sfoc = 190.662 }
            }
        }
    };

    // REAL DATA: Level1_Optimal Power System Setup.xlsx → DG1/DG3 SFOC Diesel (g/kWH)
    public static readonly List<AuxiliaryEngineType> DefaultAuxEngines = new()
    {
        new()
        {
            Id = 1, Name = "Test AE",
            SfocData = new()
            {
                new() { Load = 0.00, Sfoc = 0.0 },
                new() { Load = 0.10, Sfoc = 345.288 },
                new() { Load = 0.20, Sfoc = 292.729 },
                new() { Load = 0.30, Sfoc = 257.287 },
                new() { Load = 0.40, Sfoc = 240.613 },
                new() { Load = 0.50, Sfoc = 228.148 },
                new() { Load = 0.60, Sfoc = 219.784 },
                new() { Load = 0.70, Sfoc = 216.409 },
                new() { Load = 0.80, Sfoc = 214.753 },
                new() { Load = 0.90, Sfoc = 215.417 },
                new() { Load = 1.00, Sfoc = 217.341 }
            }
        }
    };

    public static readonly List<SailContributionItem> DefaultSailItems = new()
    {
        new() { ApparentWindAngle = 0, ApparentWindSpeed = 10, SailContributionForce = -0.5 },
        new() { ApparentWindAngle = 45, ApparentWindSpeed = 10, SailContributionForce = 50.0 },
        new() { ApparentWindAngle = 90, ApparentWindSpeed = 10, SailContributionForce = 80.0 },
        new() { ApparentWindAngle = 135, ApparentWindSpeed = 10, SailContributionForce = 30.0 },
        new() { ApparentWindAngle = 180, ApparentWindSpeed = 10, SailContributionForce = -5.0 },
        new() { ApparentWindAngle = 270, ApparentWindSpeed = 10, SailContributionForce = 75.0 },
        new() { ApparentWindAngle = 90, ApparentWindSpeed = 5, SailContributionForce = 40.0 },
        new() { ApparentWindAngle = 90, ApparentWindSpeed = 15, SailContributionForce = 120.0 },
        new() { ApparentWindAngle = 90, ApparentWindSpeed = 20, SailContributionForce = 160.0 },
    };

    // Mocks
    public Mock<IKSailCalcConfigRepository> ConfigRepoMock { get; private set; } = null!;
    public Mock<ISailContributionRepository> SailRepoMock { get; private set; } = null!;
    public Mock<IAppDataAggregationService> AppDataMock { get; private set; } = null!;

    // Services
    public CalculatorService CalculatorService { get; private set; } = null!;
    public SfocService SfocService { get; private set; } = null!;
    public SailContributionService SailContributionService { get; private set; } = null!;
    public Level1OptimizationService Level1Service { get; private set; } = null!;
    public Level2OptimizationService Level2Service { get; private set; } = null!;
    public Level3DrcService Level3Service { get; private set; } = null!;
    public BatteryAllocationService BatteryAllocationService { get; private set; } = null!;
    public ModePipelineRunner PipelineRunner { get; private set; } = null!;

    /// <summary>SFOC curves for the default engine ids (1, 1) — what most tests need.</summary>
    public EngineFuelCurves Curves { get; private set; } = null!;

    /// <summary>
    /// SFOC curves for a specific input. Blocking is safe here: the app-data source is a mock that
    /// completes synchronously.
    /// </summary>
    public EngineFuelCurves CurvesFor(CalculatorInput input)
        => SfocService.GetCurvesAsync(input).GetAwaiter().GetResult();

    public static TestServiceFactory Create(
        bool enableSailData = false,
        ILogger<ModePipelineRunner>? runnerLogger = null,
        ILogger<CalculatorService>? calculatorLogger = null)
    {
        var factory = new TestServiceFactory();

        // Mock config repo
        factory.ConfigRepoMock = new Mock<IKSailCalcConfigRepository>();
        factory.ConfigRepoMock.Setup(x => x.GetIntegrationLevelConfigsAsync())
            .ReturnsAsync(DefaultIntegrationLevels);

        // Mock app data (for SfocService)
        factory.AppDataMock = new Mock<IAppDataAggregationService>();
        factory.AppDataMock.Setup(x => x.GetInitialAppDataAsync())
            .ReturnsAsync(new AppInitialData
            {
                EngineTypes = new EngineTypesData
                {
                    MainEngines = DefaultMainEngines,
                    AuxiliaryEngines = DefaultAuxEngines
                },
                OperationalProfiles = new List<VesselOperationalProfile>(),
                Metadata = new AppDataMetadata { Version = "test" }
            });

        // Mock sail repo
        factory.SailRepoMock = new Mock<ISailContributionRepository>();
        factory.SailRepoMock.Setup(x => x.GetSailContributionItemsAsync())
            .ReturnsAsync(enableSailData ? DefaultSailItems : null);

        // Wire real services
        factory.SfocService = new SfocService(factory.AppDataMock.Object, new Mock<ILogger<SfocService>>().Object);
        factory.SailContributionService = new SailContributionService(factory.SailRepoMock.Object);
        factory.Level1Service = new Level1OptimizationService(Options.Create(new BatterySettings()));
        factory.Level2Service = new Level2OptimizationService();
        var settings = Options.Create(new CalculatorSettings());
        factory.Level3Service = new Level3DrcService(settings);
        factory.BatteryAllocationService = new BatteryAllocationService(Options.Create(new BatterySettings()));
        factory.Curves = factory.CurvesFor(CalculatorInputBuilder.Default().Build());
        factory.PipelineRunner = new ModePipelineRunner(
            factory.SfocService,
            factory.Level1Service,
            factory.Level2Service,
            factory.Level3Service,
            factory.BatteryAllocationService,
            runnerLogger ?? NullLogger<ModePipelineRunner>.Instance);
        factory.CalculatorService = new CalculatorService(
            factory.ConfigRepoMock.Object,
            factory.SailContributionService,
            factory.PipelineRunner,
            settings,
            calculatorLogger ?? NullLogger<CalculatorService>.Instance);

        return factory;
    }
}