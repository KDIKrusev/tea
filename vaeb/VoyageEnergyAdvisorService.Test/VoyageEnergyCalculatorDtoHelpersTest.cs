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

        [Fact]
        public void GetOptimalVoyageRequestFromDto_ShouldReturnCorrectRequest()
        {
            // Arrange
            var etd = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            var eta = etd.AddHours(10);

            var requestDto = new VoyageEnergyAdvisorOptimalVoyageRequestDto
            {
                Etd = etd.ToUnixTimeMilliseconds(),
                Eta = eta.ToUnixTimeMilliseconds(),
                SpeedMin = 5,
                SpeedMax = 10,
                Route = new RouteDto
                {
                    RouteName = "Optimal Test Route",
                    Waypoints = new List<GeoCoordinateDto>
                    {
                        new GeoCoordinateDto(10, 20),
                        new GeoCoordinateDto(30, 40)
                    }
                }
            };

            // Act
            var result = VoyageEnergyAdvisorDtoHelpers.GetOptimalVoyageRequestFromDto(requestDto);

            // Assert
            Assert.Equal(requestDto.SpeedMin, result.SpeedMin);
            Assert.Equal(requestDto.SpeedMax, result.SpeedMax);
            Assert.Equal(etd.UtcDateTime, result.Etd);
            Assert.Equal(eta.UtcDateTime, result.Eta);
            Assert.Equal(requestDto.Route.RouteName, result.Route.RouteName);
            Assert.Equal(requestDto.Route.Waypoints.Count, result.Route.Waypoints.Count);
            Assert.Equal(requestDto.Route.Waypoints.First().Latitude, result.Route.Waypoints.First().Latitude);
            Assert.Equal(requestDto.Route.Waypoints.First().Longitude, result.Route.Waypoints.First().Longitude);
        }

        [Fact]
        public void GetOptimalVoyageResponseDto_ShouldReturnCorrectResponse()
        {
            // Arrange
            var etd = DateTimeOffset.UtcNow;
            var eta = etd.AddHours(10);

            var option = new VoyageEnergyAdvisorVoyageOption
            {
                Etd = etd.UtcDateTime,
                Eta = eta.UtcDateTime,
                IsValid = true,
                AverageSpeed = 6.5,
                DurationInSeconds = (eta - etd).TotalSeconds,
                AverageResistancePower = 12345,
                RouteSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>
                {
                    new VoyageEnergyAdvisorVoyageOptionRouteSegment
                    {
                        StartPosition = new GeoCoordinate(10, 20),
                        EndPosition = new GeoCoordinate(11, 21),
                        TrueWeather = new WeatherData(),
                        ApparentWeather = new WeatherData()
                    }
                }
            };

            // Act
            var result = VoyageEnergyAdvisorDtoHelpers.GetOptimalVoyageResponseDto(option);

            // Assert
            Assert.NotNull(result.OptimalVoyageOption);
            Assert.Equal(option.IsValid, result.OptimalVoyageOption.IsValid);
            Assert.Equal(option.AverageSpeed, result.OptimalVoyageOption.AverageSpeed);
            Assert.Equal(option.AverageResistancePower, result.OptimalVoyageOption.AveragePower);
            Assert.Equal(new DateTimeOffset(option.Etd, TimeSpan.Zero).ToUnixTimeMilliseconds(), result.OptimalVoyageOption.Etd);
            Assert.Equal(new DateTimeOffset(option.Eta, TimeSpan.Zero).ToUnixTimeMilliseconds(), result.OptimalVoyageOption.Eta);
            Assert.Single(result.OptimalVoyageOption.RouteSegments);
        }

    }
}