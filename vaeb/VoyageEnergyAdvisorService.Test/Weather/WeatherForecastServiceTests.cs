using System.ComponentModel.Design;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VoyageEnergyAdvisor.Core.Repositories;
using VoyageEnergyAdvisor.Core.Services.WeatherProviders;
using VoyageEnergyAdvisor.Data.DataRepositories;
using VoyageEnergyAdvisor.WebApi.Services;

namespace VoyageEnergyAdvisorService.Test.Weather
{
    using Microsoft.Extensions.Configuration;
    using Xunit;
    using VoyageEnergyAdvisorService.Test.Weather.TestUtils;
    using VoyageEnergyAdvisor.Core.CommonModels;
    using VoyageEnergyAdvisor.Core.Services.WeatherService;

    //private readonly WeatherCacheService _cacheService;
    
    //private Mock<IUserVesselRepository> _userVesselRepositoryMock = new Mock<IUserVesselRepository>();
    
    
    public class WeatherForecastServiceTests
    {
        private readonly IWeatherProvider _service;
        private readonly WeatherCacheService _cacheService;
        private readonly Mock<IUserVesselRepository> _userVesselRepositoryMock;

        public WeatherForecastServiceTests()
        {
            IWeatherProvider[] forecastProviderStub = { new WeatherForecastProviderStub() };
            var configValues = new Dictionary<string, string> { { "SelectedDataProvider", "TestStub" } };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues.Select(kv => new KeyValuePair<string, string?>(kv.Key, kv.Value)))
                .Build();
            _service = new WeatherForecastProviderStub();
            
            // Mock IUserVesselRepository
            _userVesselRepositoryMock = new Mock<IUserVesselRepository>();
            _userVesselRepositoryMock.Setup(repo => repo.GetCurrentVesselAsync())
                .ReturnsAsync(new VesselDto { Id = 2, Name = "Test Vessel" });

            // Mock the scoped service provider
            var scopedServiceProviderMock = new Mock<IServiceProvider>();
            scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(IUserVesselRepository)))
                .Returns(_userVesselRepositoryMock.Object);

            // Mock the scope
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(s => s.ServiceProvider).Returns(scopedServiceProviderMock.Object);

            // Mock the scope factory
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

            // Mock the root service provider to return the scope factory
            var rootServiceProviderMock = new Mock<IServiceProvider>();
            rootServiceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                .Returns(scopeFactoryMock.Object);

            // Pass the root service provider to your service
            _cacheService = new WeatherCacheService(rootServiceProviderMock.Object);
        }

        [Fact]
        public async Task GetMultiPointWeatherForecast_ConfiguredToUseStubProvider_ReturnsForecastDataFromStubProvider()
        {
            var geoCoordinates = Utils.GetGeoCoordinates(2) ?? throw new ArgumentNullException("Utils.GetGeoCoordinates(2)");

            var result = await _service.GetMultiPointWeatherForecast(geoCoordinates);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(2, result.First()!.Weather.CurrentFromDirection);
            Assert.Equal(3, result.First()!.Weather.CurrentSpeed);
        }

        [Fact]
        public void GetCachedData_EmptyCache_ReturnsEmptyCollection()
        {
            // Arrange
            var requests = new List<WeatherRequestInstance>
        {
            new WeatherRequestInstance
            {
                Time = DateTime.UtcNow,
                Location = new GeoCoordinate { Latitude = 60.332277, Longitude = 5.195882 }
            }
        };

            // Act
            var result = _cacheService.GetCachedData(requests);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetCachedData_MatchingEntries_ReturnsCachedData()
        {
            // Arrange
            var requests = new List<WeatherRequestInstance>
                {
                    new WeatherRequestInstance
                    {
                        Time = DateTime.UtcNow,
                        Location = new GeoCoordinate { Latitude = 60.332277, Longitude = 5.195882 }
                    }
                };

            var forecast = new WeatherResponseInstance
            {
                Location = requests[0].Location,
                Time = requests[0].Time,
                Weather = new WeatherData { WindSpeed = 5.0 },
                ExpirationDateTime = DateTime.UtcNow.AddMinutes(30),
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                EndTime = DateTime.UtcNow.AddMinutes(20)
            };

             _cacheService.AddCacheData(new[] { ( forecast) });

            // Act
            var result = _cacheService.GetCachedData(requests);

            // Assert
            Assert.NotEmpty(result);
            var cachedData = result.First();
            Assert.Equal(forecast.Weather.WindSpeed, cachedData.Weather.WindSpeed);
            Assert.Equal(forecast.Location, cachedData.Location);
        }

        [Fact]
        public void GetCachedData_NonMatchingEntries_ReturnsEmptyCollection()
        {
            // Arrange
            var requests = new List<WeatherRequestInstance>
                {
                    new WeatherRequestInstance
                    {
                        Time = DateTime.UtcNow.AddMinutes(10),
                        Location = new GeoCoordinate { Latitude = 60.332277, Longitude = 5.195882 }
                    }
                };

            var forecast = new WeatherResponseInstance
            {
                Location = new GeoCoordinate { Latitude = 60.999999, Longitude = 5.999999 },
                Time = DateTime.UtcNow,
                Weather = new WeatherData { WindSpeed = 5.0 }
            };

            _cacheService.AddCacheData(new[] { ( forecast) });

            // Act
            var result = _cacheService.GetCachedData(requests);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetCachedData_ExpiredEntries_ReturnsEmptyCollection()
        {
            // Arrange
            var requests = new List<WeatherRequestInstance>
                {
                    new WeatherRequestInstance
                    {
                        Time = DateTime.UtcNow,
                        Location = new GeoCoordinate { Latitude = 60.332277, Longitude = 5.195882 }
                    }
                };

            var forecast = new WeatherResponseInstance
            {
                Location = requests[0].Location,
                Time = requests[0].Time,
                Weather = new WeatherData { WindSpeed = 5.0 },
                ExpirationDateTime = DateTime.UtcNow.AddSeconds(-1) // Expired entry
            };

            _cacheService.AddCacheData(new[] { forecast });

            // Act
            var result = _cacheService.GetCachedData(requests);

            // Assert
            Assert.Empty(result);
            Assert.Empty(_cacheService.GetCacheEntries());
        }

        [Fact]
        public void GetCachedData_MultipleMatchingEntries_ReturnsAllMatches()
        {
            // Arrange
            var requests = new List<WeatherRequestInstance>
            {
                new WeatherRequestInstance
                {
                    Time = DateTime.UtcNow,
                    Location = new GeoCoordinate { Latitude = 60.332277, Longitude = 5.195882 }
                },
                new WeatherRequestInstance
                {
                    Time = DateTime.UtcNow.AddSeconds(1),
                    Location = new GeoCoordinate { Latitude = 60.332278, Longitude = 5.195883 }
                }
             };

            var forecasts = new List<WeatherResponseInstance>
             {
                new WeatherResponseInstance
                {
                    Location = requests[0].Location,
                    Time = requests[0].Time,
                    Weather = new WeatherData { WindSpeed = 5.0 },
                    ExpirationDateTime = DateTime.UtcNow.AddMinutes(10),
                    StartTime = DateTime.UtcNow.AddMinutes(-10),
                    EndTime = DateTime.UtcNow.AddMinutes(10),
                },
                new WeatherResponseInstance
                {
                    Location = requests[1].Location,
                    Time = requests[1].Time,
                    Weather = new WeatherData { WindSpeed = 3.0 },
                    ExpirationDateTime = DateTime.UtcNow.AddMinutes(10),
                    StartTime = DateTime.UtcNow.AddMinutes(-10),
                    EndTime = DateTime.UtcNow.AddMinutes(10)
                }
             };
    
            _cacheService.AddCacheData(forecasts);

            // Act
            var result = _cacheService.GetCachedData(requests);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, r => r.Weather.WindSpeed == 5.0);
            Assert.Contains(result, r => r.Weather.WindSpeed == 3.0);
        }
    }
}
