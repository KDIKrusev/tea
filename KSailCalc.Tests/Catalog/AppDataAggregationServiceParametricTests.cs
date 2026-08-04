using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KSailCalc.Tests.Catalog;

/// <summary>
/// AppDataAggregationService parametric flow: resolution trace and Categories payload.
/// </summary>
public class AppDataAggregationServiceParametricTests
{
    private static IOptions<CalculatorSettings> LoadCalculatorSettingsFromConfiguration()
    {
        // Load from appsettings.json in the KSailCalc.Backend project root
        // Tests run from: C:\1906\KSailCalc.Backend\KSailCalc.Tests\bin\Release\net10.0\
        // We need to go up 4 levels to reach the project root
        var projectRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..");
        var appSettingsPath = Path.Combine(projectRoot, "appsettings.json");
        
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(appSettingsPath) ?? projectRoot)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var settings = new CalculatorSettings();
        config.GetSection("CalculatorSettings").Bind(settings);
        return Options.Create(settings);
    }

    private static AppDataAggregationService CreateService(List<VesselType> fleet)
    {
        var repoMock = new Mock<IKSailCalcConfigRepository>();
        repoMock.Setup(r => r.GetVesselTypesAsync()).ReturnsAsync(fleet);
        repoMock.Setup(r => r.GetMainEnginesAsync()).ReturnsAsync(new List<EngineType>());
        repoMock.Setup(r => r.GetAuxiliaryEnginesAsync()).ReturnsAsync(new List<AuxiliaryEngineType>());
        repoMock.Setup(r => r.GetOperationalProfilesAsync()).ReturnsAsync(
            fleet.Select(v => new VesselOperationalProfile { VesselTypeName = v.VesselTypeName }).ToList());

        var resolutionService = new VesselResolutionService(
            repoMock.Object, NullLogger<VesselResolutionService>.Instance);

        var settingsMock = LoadCalculatorSettingsFromConfiguration();

        return new AppDataAggregationService(
            repoMock.Object,
            resolutionService,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<AppDataAggregationService>.Instance,
            settingsMock);
    }

    private static List<VesselType> Bulk63kOnly() => new()
    {
        new VesselType
        {
            Id = 3,
            VesselTypeName = "Bulk Carrier 63,000 dwt",
            Category = "Bulk Carrier",
            Unit = "dwt",
            ReferenceSize = 63000,
            MinSize = 49000,
            MaxSize = 121499,
            SeaMarginPercent = 20m,
            SpeedPowerCurve = new List<SpeedPowerPoint>
            {
                new() { SpeedKnots = 12.5m, CalmWaterPowerKW = 4373 },
                new() { SpeedKnots = 13.0m, CalmWaterPowerKW = 4941 }
            }
        }
    };

    [Fact]
    public async Task ParametricPath_PopulatesResolutionTrace()
    {
        var service = CreateService(Bulk63kOnly());

        var result = await service.GetFullVesselDataByCategoryAsync("Bulk Carrier", 63000, 12.7m);

        result.Should().NotBeNull();
        result!.VesselConfig.CalmWaterPowerKW.Should().BeApproximately(4600.2m, 0.05m);
        result.Resolution.Should().NotBeNull();
        result.Resolution!.ProfileSource.Should().Be("Bulk Carrier 63,000 dwt");
    }

    [Fact]
    public async Task InitialData_ExposesCategoriesWithBounds()
    {
        var service = CreateService(Bulk63kOnly());

        var data = await service.GetInitialAppDataAsync();

        data.Categories.Should().ContainSingle(c => c.Name == "Bulk Carrier");
        var cat = data.Categories.Single();
        cat.Unit.Should().Be("dwt");
        cat.SizeMin.Should().Be(63000);
        cat.SizeMax.Should().Be(63000);
        cat.SpeedMin.Should().Be(12.5m);
        cat.SpeedMax.Should().Be(13.0m);
    }
}
