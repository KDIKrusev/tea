using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using VoyageEnergyAdvisor.Core.CommonModels.Exceptions;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService;
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

        private sealed class TestUserFacingException : UserFacingException
        {
            public TestUserFacingException(string message) : base(message) { }
            public override string UserMessage => Message;
        }

        private static VoyageEnergyAdvisorRequestDto GetValidRequestDto()
        {
            var etd = System.DateTimeOffset.UtcNow.AddHours(1);
            var eta = etd.AddHours(10);

            return new VoyageEnergyAdvisorRequestDto
            {
                EtdMin = etd.ToUnixTimeMilliseconds() * 1000,
                EtdMax = etd.AddHours(2).ToUnixTimeMilliseconds() * 1000,
                EtaMin = eta.ToUnixTimeMilliseconds() * 1000,
                EtaMax = eta.AddHours(2).ToUnixTimeMilliseconds() * 1000,
                SpeedMin = 1,
                SpeedMax = 10,
                Route = new RouteDto
                {
                    RouteName = "Controller Test Route",
                    Waypoints = new List<GeoCoordinateDto>
                    {
                        new GeoCoordinateDto(60.0, 5.0),
                        new GeoCoordinateDto(61.0, 6.0)
                    }
                }
            };
        }

        [Fact]
        public async System.Threading.Tasks.Task CalculateVoyageEnergy_CallsService_AndReturnsVoyageOptionSets()
        {
            var requestDto = GetValidRequestDto();

            var response = new VoyageEnergyAdvisorResponse
            {
                VoyageDistance = 1234.0,
                VoyageOptionSets = new List<VoyageEnergyAdvisorVoyageOptionSet>
                {
                    new()
                    {
                        IsValid = true,
                        VariablePowerOption = new VoyageEnergyAdvisorVoyageOption { IsValid = true },
                        VariableSpeedOption = new VoyageEnergyAdvisorVoyageOption
                        {
                            IsValid = true,
                            IsVariableSpeedOption = true
                        }
                    }
                }
            };

            _voyageServiceMock
                .Setup(s => s.GetVoyageOptions(It.IsAny<VoyageEnergyAdvisorRequest>()))
                .ReturnsAsync(response);

            var actionResult = await _controller.CalculateVoyageEnergy(requestDto);

            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var responseDto = Assert.IsType<VoyageEnergyAdvisorResponseDto>(okResult.Value);

            var set = Assert.Single(responseDto.VoyageOptionSets);
            Assert.NotNull(set.VariablePowerOption);
            Assert.NotNull(set.VariableSpeedOption);
            Assert.True(set.VariableSpeedOption!.IsVariableSpeedOption);

            _voyageServiceMock.Verify(
                s => s.GetVoyageOptions(It.IsAny<VoyageEnergyAdvisorRequest>()), Times.Once);
        }

        [Fact]
        public async System.Threading.Tasks.Task CalculateVoyageEnergy_ReturnsBadRequest_WhenServiceThrowsUserFacingException()
        {
            var requestDto = GetValidRequestDto();

            _voyageServiceMock
                .Setup(s => s.GetVoyageOptions(It.IsAny<VoyageEnergyAdvisorRequest>()))
                .ThrowsAsync(new TestUserFacingException("ETA must be after ETD."));

            var actionResult = await _controller.CalculateVoyageEnergy(requestDto);

            Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        }

        [Fact]
        public async System.Threading.Tasks.Task CalculateVoyageEnergy_ReturnsInternalServerError_WhenServiceThrowsUnexpectedException()
        {
            var requestDto = GetValidRequestDto();

            _voyageServiceMock
                .Setup(s => s.GetVoyageOptions(It.IsAny<VoyageEnergyAdvisorRequest>()))
                .ThrowsAsync(new System.InvalidOperationException("boom"));

            var actionResult = await _controller.CalculateVoyageEnergy(requestDto);

            var statusResult = Assert.IsType<ObjectResult>(actionResult.Result);
            Assert.Equal(500, statusResult.StatusCode);
        }
    }
}
