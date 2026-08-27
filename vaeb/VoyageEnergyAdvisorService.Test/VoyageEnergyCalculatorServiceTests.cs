
using Moq;
using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Models.VoyageEnergyAdvisor;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Exceptions;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyCalculatorService.Models;
using Xunit;
using Microsoft.Extensions.Logging;
using VoyageEnergyAdvisor.Core.Services.AisService;
using VoyageEnergyAdvisor.Core.Services.FuelConsumptionService;

public class VoyageEnergyAdvisorServiceTests
{
    private readonly Mock<IVoyageEnergyAdvisorVoyageOptionsBuilder> _builderMock;
    private readonly Mock<IAisService> _aisServiceMock;
    private readonly Mock<ILogger<VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.VoyageEnergyAdvisorService>> _loggerMock;
    private readonly VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.VoyageEnergyAdvisorService _service;

    public VoyageEnergyAdvisorServiceTests()
    {
        _builderMock = new Mock<IVoyageEnergyAdvisorVoyageOptionsBuilder>();
        _aisServiceMock = new Mock<IAisService>();
        _loggerMock = new Mock<ILogger<VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.VoyageEnergyAdvisorService>>();

        _service = new VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.VoyageEnergyAdvisorService(
            _builderMock.Object,
            _aisServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetVoyageOptions_ReturnsExpectedResponse_WhenRequestIsValid()
    {
        // Arrange a valid request
        var request = new VoyageEnergyAdvisorRequest
        {
            ReturnArrayDimension = 1,    // Passes first check
            SpeedMin = 0.5,              // > 0 and <= SpeedMax
            SpeedMax = 1.0,              // >= SpeedMin
            Route = new Route
            {
                RouteName = "Minimal Route",
                Waypoints = new List<GeoCoordinate>
                {
                    new GeoCoordinate(60.0, 5.0),
                    new GeoCoordinate(61.0, 6.0)
                }
            }
        };

        var voyageOptions = new List<VoyageEnergyAdvisorVoyageOption>
        {
            new VoyageEnergyAdvisorVoyageOption(), new VoyageEnergyAdvisorVoyageOption()
        };

        _builderMock.Setup(b => b.PrepareVoyageOptions(It.IsAny<VoyageEnergyAdvisorRequest>())).ReturnsAsync(voyageOptions);
        _builderMock
            .Setup(b => b.ToValidRequest(It.IsAny<VoyageEnergyAdvisorRequest>()))
            .Returns((VoyageEnergyAdvisorRequest req) => (VoyageEnergyAdvisorRequest?)req);

        // Act
        var result = await _service.GetVoyageOptions(request);

        // Assert
        Assert.Equal(voyageOptions.Count, result.VoyageOptions.Count);
    }

    private static VoyageEnergyAdvisorOptimalVoyageRequest GetValidOptimalVoyageRequest()
    {
        return new VoyageEnergyAdvisorOptimalVoyageRequest
        {
            Etd = DateTime.UtcNow.AddHours(1),
            Eta = DateTime.UtcNow.AddHours(11),
            SpeedMin = 1.0,
            SpeedMax = 10.0,
            Route = new Route
            {
                RouteName = "Optimal Route",
                Waypoints = new List<GeoCoordinate>
                {
                    new GeoCoordinate(60.0, 5.0),
                    new GeoCoordinate(61.0, 6.0)
                }
            }
        };
    }

    [Fact]
    public async Task GetOptimalVoyageOption_ThrowsOptimalVoyageRequestException_WhenEtaIsBeforeEtd()
    {
        var request = GetValidOptimalVoyageRequest();
        request.Eta = request.Etd.AddHours(-1);

        await Assert.ThrowsAsync<OptimalVoyageRequestException>(() => _service.GetOptimalVoyageOption(request));
    }

    [Fact]
    public async Task GetOptimalVoyageOption_ThrowsOptimalVoyageRequestException_WhenRouteIsEmpty()
    {
        var request = GetValidOptimalVoyageRequest();
        request.Route = new Route { RouteName = "Empty", Waypoints = new List<GeoCoordinate>() };

        await Assert.ThrowsAsync<OptimalVoyageRequestException>(() => _service.GetOptimalVoyageOption(request));
    }

    [Fact]
    public async Task GetOptimalVoyageOption_ThrowsOptimalVoyageRequestException_WhenSpeedMinIsNotPositive()
    {
        var request = GetValidOptimalVoyageRequest();
        request.SpeedMin = 0;

        await Assert.ThrowsAsync<OptimalVoyageRequestException>(() => _service.GetOptimalVoyageOption(request));
    }

    [Fact]
    public async Task GetOptimalVoyageOption_ThrowsOptimalVoyageRequestException_WhenSpeedMaxIsNotGreaterThanSpeedMin()
    {
        var request = GetValidOptimalVoyageRequest();
        request.SpeedMin = 5.0;
        request.SpeedMax = 5.0;

        await Assert.ThrowsAsync<OptimalVoyageRequestException>(() => _service.GetOptimalVoyageOption(request));
    }

    [Fact]
    public async Task GetOptimalVoyageOption_ThrowsOptimalVoyageRequestException_WhenRequiredSpeedIsBelowSpeedMin()
    {
        // SpeedMin/SpeedMax are 1.0/10.0; a required speed below SpeedMin must be rejected.
        var request = GetValidOptimalVoyageRequest();

        _builderMock
            .Setup(b => b.CalculateRequiredAverageSpeed(It.IsAny<double>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(0.5);

        await Assert.ThrowsAsync<OptimalVoyageRequestException>(() => _service.GetOptimalVoyageOption(request));
    }

    [Fact]
    public async Task GetOptimalVoyageOption_ThrowsOptimalVoyageRequestException_WhenRequiredSpeedIsAboveSpeedMax()
    {
        // SpeedMin/SpeedMax are 1.0/10.0; a required speed above SpeedMax must be rejected.
        var request = GetValidOptimalVoyageRequest();

        _builderMock
            .Setup(b => b.CalculateRequiredAverageSpeed(It.IsAny<double>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(20.0);

        await Assert.ThrowsAsync<OptimalVoyageRequestException>(() => _service.GetOptimalVoyageOption(request));
    }

    [Fact]
    public async Task GetOptimalVoyageOption_DelegatesToBuilder_WhenRequestIsValid()
    {
        var request = GetValidOptimalVoyageRequest();
        var expectedOption = new VoyageEnergyAdvisorVoyageOption { IsValid = true, AverageSpeed = 5.5 };

        _builderMock
            .Setup(b => b.CalculateRequiredAverageSpeed(It.IsAny<double>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(5.5);

        _builderMock
            .Setup(b => b.BuildOptimalVoyageOption(request, 5.5))
            .ReturnsAsync(expectedOption);

        var result = await _service.GetOptimalVoyageOption(request);

        Assert.Same(expectedOption, result);
        _builderMock.Verify(b => b.BuildOptimalVoyageOption(request, 5.5), Times.Once);
    }

}
