using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using VoyageEnergyAdvisor.Core.Models.VoyageEnergyAdvisor;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Exceptions;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models;
using VoyageEnergyAdvisor.Core.Services.WeatherProviders;
using VoyageEnergyAdvisor.WebApi;
using VoyageEnergyAdvisor.WebApi.Dtos;
using Xunit;

namespace VoyageEnergyAdvisorService.Test
{
    public class VoyageEnergyAdvisorControllerTests
    {
        private readonly Mock<IVoyageEnergyAdvisorService> _voyageServiceMock;
        private readonly Mock<ILogger<VoyageEnergyAdvisorController>> _loggerMock;
        private readonly Mock<ICancellationTokenService> _cancellationTokenServiceMock;
        private readonly VoyageEnergyAdvisorController _controller;

        public VoyageEnergyAdvisorControllerTests()
        {
            _voyageServiceMock = new Mock<IVoyageEnergyAdvisorService>();
            _loggerMock = new Mock<ILogger<VoyageEnergyAdvisorController>>();
            _cancellationTokenServiceMock = new Mock<ICancellationTokenService>();

            _controller = new VoyageEnergyAdvisorController(
                _voyageServiceMock.Object,
                _loggerMock.Object,
                _cancellationTokenServiceMock.Object);
        }

        private static VoyageEnergyAdvisorOptimalVoyageRequestDto GetValidRequestDto()
        {
            var etd = System.DateTimeOffset.UtcNow.AddHours(1);
            var eta = etd.AddHours(10);

            return new VoyageEnergyAdvisorOptimalVoyageRequestDto
            {
                Etd = etd.ToUnixTimeMilliseconds(),
                Eta = eta.ToUnixTimeMilliseconds(),
                SpeedMin = 1,
                SpeedMax = 10,
                Route = new RouteDto
                {
                    RouteName = "Optimal Test Route",
                    Waypoints = new System.Collections.Generic.List<GeoCoordinateDto>
                    {
                        new GeoCoordinateDto(60.0, 5.0),
                        new GeoCoordinateDto(61.0, 6.0)
                    }
                }
            };
        }

        [Fact]
        public async System.Threading.Tasks.Task GetOptimalVoyage_CallsService_AndReturnsSingleVoyageOptionDto()
        {
            var requestDto = GetValidRequestDto();
            var voyageOption = new VoyageEnergyAdvisorVoyageOption
            {
                IsValid = true,
                AverageSpeed = 6.0
            };

            _voyageServiceMock
                .Setup(s => s.GetOptimalVoyageOption(It.IsAny<VoyageEnergyAdvisorOptimalVoyageRequest>()))
                .ReturnsAsync(voyageOption);

            var actionResult = await _controller.GetOptimalVoyage(requestDto);

            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var responseDto = Assert.IsType<VoyageEnergyAdvisorOptimalVoyageResponseDto>(okResult.Value);

            Assert.NotNull(responseDto.OptimalVoyageOption);
            _voyageServiceMock.Verify(s => s.GetOptimalVoyageOption(It.IsAny<VoyageEnergyAdvisorOptimalVoyageRequest>()), Times.Once);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetOptimalVoyage_ReturnsBadRequest_WhenServiceThrowsUserFacingException()
        {
            var requestDto = GetValidRequestDto();

            _voyageServiceMock
                .Setup(s => s.GetOptimalVoyageOption(It.IsAny<VoyageEnergyAdvisorOptimalVoyageRequest>()))
                .ThrowsAsync(new OptimalVoyageRequestException("ETA must be after ETD."));

            var actionResult = await _controller.GetOptimalVoyage(requestDto);

            Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetOptimalVoyage_ReturnsInternalServerError_WhenServiceThrowsUnexpectedException()
        {
            var requestDto = GetValidRequestDto();

            _voyageServiceMock
                .Setup(s => s.GetOptimalVoyageOption(It.IsAny<VoyageEnergyAdvisorOptimalVoyageRequest>()))
                .ThrowsAsync(new System.InvalidOperationException("boom"));

            var actionResult = await _controller.GetOptimalVoyage(requestDto);

            var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
            Assert.Equal(500, objectResult.StatusCode);
        }
    }
}
