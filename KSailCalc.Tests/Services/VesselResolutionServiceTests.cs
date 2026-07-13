using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Repositories.Interfaces;
using KSailCalc.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KSailCalc.Tests.Services;

/// <summary>
/// Golden-value tests for Epic 1 / Story 1.2 (parametric vessel resolution).
/// Expected values G1-G8 come from the verified Excel source
/// (docs/Speed vs Power curve_...2025.xlsx, laden block) via Story 1.1.
/// </summary>
public class VesselResolutionServiceTests
{
    // ---- Fixture: reference fleet mirroring the post-Story-1.1 database ----

    private static List<SpeedPowerPoint> Curve(params decimal[] kw)
    {
        // 20 points, 6.0 -> 15.5 kn in 0.5 steps
        return kw.Select((p, i) => new SpeedPowerPoint
        {
            SpeedKnots = 6.0m + 0.5m * i,
            CalmWaterPowerKW = p
        }).ToList();
    }

    private static VesselType Vessel(int id, string name, string category, string unit,
        List<SpeedPowerPoint> curve, decimal? refSize, decimal? minSize, decimal? maxSize) => new()
    {
        Id = id,
        VesselTypeName = name,
        Category = category,
        Unit = unit,
        SpeedPowerCurve = curve,
        ReferenceSize = refSize,
        MinSize = minSize,
        MaxSize = maxSize,
        SeaMarginPercent = 20m,
        IsActive = true
    };

    private static List<VesselType> ReferenceFleet() => new()
    {
        Vessel(1, "Bulk Carrier 10,000 dwt", "Bulk Carrier", "dwt",
            Curve(167, 209, 258, 313, 376, 447, 528, 621, 727, 850, 994, 1164, 1365, 1606, 1895, 2245, 2663, 3157, 3754, 4508),
            10000, 0, 22499),
        Vessel(2, "Bulk Carrier 35,000 dwt", "Bulk Carrier", "dwt",
            Curve(405, 509, 628, 763, 916, 1086, 1276, 1487, 1721, 1980, 2267, 2588, 2945, 3347, 3801, 4315, 4902, 5573, 6344, 7230),
            35000, 22500, 48999),
        Vessel(3, "Bulk Carrier 63,000 dwt", "Bulk Carrier", "dwt",
            Curve(534, 671, 829, 1008, 1211, 1438, 1690, 1970, 2278, 2618, 2993, 3407, 3865, 4373, 4941, 5577, 6294, 7107, 8032, 9087),
            63000, 49000, 121499),
        Vessel(5, "Bulk Carrier 180,000 dwt", "Bulk Carrier", "dwt",
            Curve(988, 1244, 1540, 1877, 2258, 2685, 3160, 3686, 4264, 4898, 5589, 6342, 7160, 8048, 9013, 10062, 11204, 12451, 13820, 15349),
            180000, 121500, null),
        Vessel(9, "Tanker 300,000 dwt", "Oil Tanker", "dwt",
            Curve(1326, 1671, 2071, 2527, 3044, 3624, 4271, 4986, 5774, 6636, 7576, 8598, 9704, 10898, 12188, 13592, 15114, 16761, 18542, 20474),
            300000, 202500, null),
        // Smaller tankers: anchors for ordering; minimal curves (not used by the golden cases)
        Vessel(6, "Tanker 10,000 dwt", "Oil Tanker", "dwt",
            new List<SpeedPowerPoint> { new() { SpeedKnots = 14.0m, CalmWaterPowerKW = 2777 } },
            10000, 0, 29999),
        Vessel(7, "Tanker 50,000 dwt", "Oil Tanker", "dwt",
            new List<SpeedPowerPoint> { new() { SpeedKnots = 14.0m, CalmWaterPowerKW = 5723 } },
            50000, 30000, 77499),
        Vessel(8, "Tanker 105,000 dwt", "Oil Tanker", "dwt",
            new List<SpeedPowerPoint> { new() { SpeedKnots = 14.0m, CalmWaterPowerKW = 8560 } },
            105000, 77500, 202499),
        // Bucket-only categories: no ReferenceSize
        Vessel(16, "Container 8000-11999 TEU", "Container", "TEU",
            new List<SpeedPowerPoint>
            {
                new() { SpeedKnots = 17.5m, CalmWaterPowerKW = 15288 },
                new() { SpeedKnots = 18.0m, CalmWaterPowerKW = 16635 },
                new() { SpeedKnots = 19.0m, CalmWaterPowerKW = 19775 }
            },
            null, 8000, 11999),
        Vessel(10, "Offshore Support Vessel", "Offshore Support", "dwt",
            new List<SpeedPowerPoint>
            {
                new() { SpeedKnots = 10.0m, CalmWaterPowerKW = 1500 },
                new() { SpeedKnots = 11.0m, CalmWaterPowerKW = 1950 },
                new() { SpeedKnots = 12.0m, CalmWaterPowerKW = 2500 },
                new() { SpeedKnots = 13.0m, CalmWaterPowerKW = 3200 }
            },
            null, 0, null)
    };

    private static VesselResolutionService CreateService(List<VesselType>? fleet = null)
    {
        var repoMock = new Mock<IKSailCalcConfigRepository>();
        repoMock.Setup(r => r.GetVesselTypesAsync()).ReturnsAsync(fleet ?? ReferenceFleet());
        return new VesselResolutionService(repoMock.Object, NullLogger<VesselResolutionService>.Instance);
    }

    // ---- G1: full 2D interpolation ----
    [Fact]
    public async Task Resolve_Bulk75000_At12_7Knots_Interpolates2D()
    {
        var result = await CreateService().ResolveAsync("Bulk Carrier", 75000, 12.7m);

        result.Should().NotBeNull();
        // 63k @12.7 = 4600.2; 180k @12.7 = 8434.0; t = 12000/117000
        result!.CalmWaterPowerKW.Should().BeApproximately(4993.4m, 0.5m);
        result.Info.LowerRefSize.Should().Be(63000);
        result.Info.UpperRefSize.Should().Be(180000);
        result.Info.T.Should().BeApproximately(0.1026m, 0.001m);
        result.Info.Clamped.Should().BeFalse();
        result.BucketRecord.VesselTypeName.Should().Be("Bulk Carrier 63,000 dwt"); // 75000 in [49000;121499]
    }

    // ---- G2: exact anchor + exact speed ----
    [Fact]
    public async Task Resolve_Bulk63000_At13Knots_ReturnsExactCurveValue()
    {
        var result = await CreateService().ResolveAsync("Bulk Carrier", 63000, 13.0m);

        result!.CalmWaterPowerKW.Should().Be(4941);
        result.Info.T.Should().BeNull(); // lower == upper, no blend
        result.Info.Clamped.Should().BeFalse();
    }

    // ---- G3: clamp below smallest reference ----
    [Fact]
    public async Task Resolve_Bulk5000_ClampsToSmallestReference()
    {
        var result = await CreateService().ResolveAsync("Bulk Carrier", 5000, 13.0m);

        result!.CalmWaterPowerKW.Should().Be(1895); // 10k curve @13
        result.Info.Clamped.Should().BeTrue();
        result.Info.LowerRefSize.Should().BeNull();
        result.Info.UpperRefSize.Should().Be(10000);
    }

    // ---- G4: clamp above largest reference ----
    [Fact]
    public async Task Resolve_Tanker400000_ClampsToLargestReference()
    {
        var result = await CreateService().ResolveAsync("Oil Tanker", 400000, 14.0m);

        result!.CalmWaterPowerKW.Should().Be(15114); // 300k curve @14
        result.Info.Clamped.Should().BeTrue();
        result.BucketRecord.VesselTypeName.Should().Be("Tanker 300,000 dwt"); // MaxSize null bucket
    }

    // ---- G5: bucket-only category (Container) ----
    [Fact]
    public async Task Resolve_Container8500_UsesBucketCurveDirectly()
    {
        var result = await CreateService().ResolveAsync("Container", 8500, 18.0m);

        result!.CalmWaterPowerKW.Should().Be(16635);
        result.Info.LowerRefSize.Should().BeNull();
        result.Info.UpperRefSize.Should().BeNull();
        result.Info.ProfileSource.Should().Be("Container 8000-11999 TEU");
        result.Info.Clamped.Should().BeFalse();
    }

    [Fact]
    public async Task Resolve_OffshoreSupport_UsesSingleRecordCurve()
    {
        var result = await CreateService().ResolveAsync("Offshore Support", 7000, 11.0m);

        result!.CalmWaterPowerKW.Should().Be(1950);
    }

    // ---- G6: power anchors and profile bucket diverge ----
    [Fact]
    public async Task Resolve_Bulk130000_InterpolatesPowerButTakesProfileFrom180k()
    {
        var result = await CreateService().ResolveAsync("Bulk Carrier", 130000, 13.0m);

        // 4941 + (67000/117000) * (9013 - 4941) = 7272.8
        result!.CalmWaterPowerKW.Should().BeApproximately(7272.8m, 0.5m);
        result.Info.LowerRefSize.Should().Be(63000);
        result.Info.UpperRefSize.Should().Be(180000);
        result.BucketRecord.VesselTypeName.Should().Be("Bulk Carrier 180,000 dwt"); // 130000 >= 121500
    }

    // ---- G8: bucket boundary is inclusive ----
    [Fact]
    public async Task Resolve_Bulk121500_BucketBoundaryBelongsTo180k()
    {
        var result = await CreateService().ResolveAsync("Bulk Carrier", 121500, 13.0m);

        result!.BucketRecord.VesselTypeName.Should().Be("Bulk Carrier 180,000 dwt");
    }

    // ---- Error handling ----
    [Fact]
    public async Task Resolve_UnknownCategory_ReturnsNull()
    {
        var result = await CreateService().ResolveAsync("Cruise Ship", 50000, 12.0m);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Resolve_CategoryMatch_IsCaseInsensitive()
    {
        var result = await CreateService().ResolveAsync("bulk carrier", 63000, 13.0m);

        result!.CalmWaterPowerKW.Should().Be(4941);
    }
}

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
