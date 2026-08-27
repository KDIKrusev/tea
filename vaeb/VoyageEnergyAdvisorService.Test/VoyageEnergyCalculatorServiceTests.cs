
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

        var voyageOptionSets = new List<VoyageEnergyAdvisorVoyageOptionSet>
        {
            new()
            {
                IsValid = true,
                VariablePowerOption = new VoyageEnergyAdvisorVoyageOption { IsValid = true },
                VariableSpeedOption = new VoyageEnergyAdvisorVoyageOption { IsValid = true, IsVariableSpeedOption = true }
            },
            new()
            {
                IsValid = true,
                VariablePowerOption = new VoyageEnergyAdvisorVoyageOption { IsValid = true },
                VariableSpeedOption = null,
                VariableSpeedUnavailableReason = "No constant propulsion power can satisfy the requested ETA."
            }
        };

        _builderMock
            .Setup(b => b.PrepareVoyageOptionSets(It.IsAny<VoyageEnergyAdvisorRequest>()))
            .ReturnsAsync(voyageOptionSets);
        _builderMock
            .Setup(b => b.ToValidRequest(It.IsAny<VoyageEnergyAdvisorRequest>()))
            .Returns((VoyageEnergyAdvisorRequest req) => (VoyageEnergyAdvisorRequest?)req);

        // Act
        var result = await _service.GetVoyageOptions(request);

        // Assert
        Assert.Equal(voyageOptionSets.Count, result.VoyageOptionSets.Count);
        Assert.Same(voyageOptionSets[0].VariablePowerOption, result.VoyageOptionSets[0].VariablePowerOption);

        // A slot without a feasible constant-power solution must not take the rest of the response down.
        Assert.NotNull(result.VoyageOptionSets[0].VariableSpeedOption);
        Assert.Null(result.VoyageOptionSets[1].VariableSpeedOption);
        Assert.NotNull(result.VoyageOptionSets[1].VariableSpeedUnavailableReason);
    }

}
