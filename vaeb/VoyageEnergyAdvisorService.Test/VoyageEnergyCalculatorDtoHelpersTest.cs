using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Models;
using VoyageEnergyAdvisor.Core.Models.VoyageEnergyAdvisor;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyCalculatorService.Models;
using VoyageEnergyAdvisor.WebApi;
using VoyageEnergyAdvisor.WebApi.Dtos;
using Xunit;

namespace VoyageEnergyAdvisorService.Test
{
    public class VoyageEnergyAdvisorDtoHelpersTests
    {

        [Fact]
        public void GetRequestFromDto_ShouldReturnCorrectRequest()
        {
            // Arrange
            var requestDto = new VoyageEnergyAdvisorRequestDto
            {
                SpeedMin = 10,
                SpeedMax = 20,
                EtdMin = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                EtdMax = DateTimeOffset.Now.AddHours(1).ToUnixTimeMilliseconds(),
                EtaMin = DateTimeOffset.Now.AddHours(2).ToUnixTimeMilliseconds(),
                EtaMax = DateTimeOffset.Now.AddHours(3).ToUnixTimeMilliseconds(),
                Route = new RouteDto
                {
                    RouteName = "Test Route",
                    Waypoints = new List<GeoCoordinateDto>
                    {
                        new GeoCoordinateDto(10, 20),
                        new GeoCoordinateDto(30, 40)
                    }
                }
            };

            // Act
            var result = VoyageEnergyAdvisorDtoHelpers.GetRequestFromDto(requestDto);

            // Assert
            Assert.Equal(requestDto.SpeedMin, result.SpeedMin);
            Assert.Equal(requestDto.SpeedMax, result.SpeedMax);
            Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(requestDto.EtdMin / 1000).DateTime, result.EtdMin);
            Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(requestDto.EtdMax / 1000).DateTime, result.EtdMax);
            Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(requestDto.EtaMin / 1000).DateTime, result.EtaMin);
            Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(requestDto.EtaMax / 1000).DateTime, result.EtaMax);
            Assert.Equal(requestDto.Route.RouteName, result.Route.RouteName);
            Assert.Equal(requestDto.Route.Waypoints.Count, result.Route.Waypoints.Count);
            Assert.Equal(requestDto.Route.Waypoints.First().Latitude, result.Route.Waypoints.First().Latitude);
            Assert.Equal(requestDto.Route.Waypoints.First().Longitude, result.Route.Waypoints.First().Longitude);
        }
        
        // Test for GetLiveRequestFromDto
        [Fact]
        public void GetLiveRequestFromDto_ShouldReturnCorrectLiveRequest()
        {
            // Arrange
            var liveRequestDto = new VoyageEnergyAdvisorLiveRequestDto
            {
                Route = new RouteDto
                {
                    RouteName = "Live Test Route",
                    Waypoints = new List<GeoCoordinateDto>
                    {
                        new GeoCoordinateDto(10, 20),
                        new GeoCoordinateDto(30, 40)
                    }
                }
            };

            // Act
            var result = VoyageEnergyAdvisorDtoHelpers.GetLiveRequestFromDto(liveRequestDto);

            // Assert
            Assert.Equal(liveRequestDto.Route.RouteName, result.Route.RouteName);
            Assert.Equal(liveRequestDto.Route.Waypoints.Count, result.Route.Waypoints.Count);
            Assert.Equal(liveRequestDto.Route.Waypoints.First().Latitude, result.Route.Waypoints.First().Latitude);
            Assert.Equal(liveRequestDto.Route.Waypoints.First().Longitude, result.Route.Waypoints.First().Longitude);
        }

    }
}