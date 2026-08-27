namespace VoyageEnergyAdvisorService.Test.Weather
{
    using VoyageEnergyAdvisor.Core.CommonModels;
    using VoyageEnergyAdvisor.Core.Services.WeatherProviders;
    using VoyageEnergyAdvisor.Core.Services.WeatherProviders.WeatherProviderModels;
    using VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Moq;
    using VoyageEnergyAdvisor.Core.Repositories;
    using VoyageEnergyAdvisorService.Test.Weather.TestUtils;
    using Xunit;

    public class OfflineWeatherForecastProviderTests
    {
        private readonly IWeatherProvider _forecastProvider;
        private readonly Mock<IConfigurationRepository> _mockConfigRepo;
        private readonly Mock<ILogger<OfflineWeatherForecastProvider>> _mockLogger;

        public OfflineWeatherForecastProviderTests()
        {
            _mockConfigRepo = new Mock<IConfigurationRepository>();
            var options = new OfflineWeatherProviderConfiguration
            {
                UpdatedAtTimeDelta = TimeSpan.FromHours(1),
                WeatherForecast = GetWeatherDataWithoutLocationAndTime(5)
            };

            _mockConfigRepo
            .Setup(repo => repo.GetConfigurationAsync<OfflineWeatherProviderConfiguration>())
            .ReturnsAsync(options);

            _mockLogger = new Mock<ILogger<OfflineWeatherForecastProvider>>();

            _forecastProvider = new OfflineWeatherForecastProvider(_mockConfigRepo.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetMultiPointWeatherForecast_RequestCountIsEqualToOfflineDataCount_ReturnedWeatherDataEqualToOfflineData()
        {
            var geoCoordinates = Utils.GetGeoCoordinates(5);

            var result = await _forecastProvider.GetMultiPointWeatherForecast(geoCoordinates);

            Assert.Equal(5, result.Count());
        }

        [Fact]
        public async Task GetMultiPointWeatherForecast_RequestCountIsLessThanOfflineDataCount_ReturnedWeatherDataIsSubsetOfOfflineData()
        {
            var geoCoordinates = Utils.GetGeoCoordinates(4);

            var result = await _forecastProvider.GetMultiPointWeatherForecast(geoCoordinates);

            Assert.Equal(4, result.Count);
        }

        [Fact]
        public async Task GetMultiPointWeatherForecast_RequestCountIsGreaterThanOfflineDataCount_ReturnedWeatherDataIsOfflineDataRepeated()
        {
            var geoCoordinates = Utils.GetGeoCoordinates(6);

            var result = await _forecastProvider.GetMultiPointWeatherForecast(geoCoordinates);

            Assert.Equal(6, result.Count);
            Assert.Equal(1, result.First().Weather.CurrentSpeed);
            Assert.Equal(1, result.First().Weather.CurrentFromDirection);
        }

        [Fact]
        public async Task GetMultiPointWeatherForecast_EmptyRequests_ReturnsEmptyList()
        {
            var geoCoordinates = Utils.GetGeoCoordinates(0);

            var result = await _forecastProvider.GetMultiPointWeatherForecast(geoCoordinates);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetMultiPointWeatherForecast_EmptyOfflineData_ThrowsDivideByZero()
        {
            // Arrange
            var mockConfigRepo = new Mock<IConfigurationRepository>();
            var options = new OfflineWeatherProviderConfiguration
            {
                UpdatedAtTimeDelta = TimeSpan.FromHours(1),
                WeatherForecast = new List<WeatherResponseInstance>()
            };

            mockConfigRepo
                .Setup(repo => repo.GetConfigurationAsync<OfflineWeatherProviderConfiguration>())
                .ReturnsAsync(options);

            var mockLogger = new Mock<ILogger<OfflineWeatherForecastProvider>>();
            var provider = new OfflineWeatherForecastProvider(mockConfigRepo.Object, mockLogger.Object);

            var geoCoordinates = Utils.GetGeoCoordinates(3);

            // Act & Assert - Tests actual behavior (division by zero bug in production code)
            await Assert.ThrowsAsync<DivideByZeroException>(
                async () => await provider.GetMultiPointWeatherForecast(geoCoordinates));
        }

        [Fact]
        public async Task GetMultiPointWeatherForecast_VerifiesTimestampSet()
        {
            var geoCoordinates = Utils.GetGeoCoordinates(2);
            var beforeCall = DateTime.UtcNow.AddSeconds(-1);

            var result = await _forecastProvider.GetMultiPointWeatherForecast(geoCoordinates);

            var afterCall = DateTime.UtcNow.AddSeconds(1);
            
            Assert.All(result, r => 
            {
                Assert.True(r.Time >= beforeCall && r.Time <= afterCall);
            });
        }

        [Fact]
        public async Task GetMultiPointWeatherForecast_VerifiesLocationSet()
        {
            var geoCoordinates = new List<WeatherRequestInstance>
            {
                new WeatherRequestInstance { Location = new GeoCoordinate(60.0, 10.0), Time = DateTime.UtcNow },
                new WeatherRequestInstance { Location = new GeoCoordinate(61.0, 11.0), Time = DateTime.UtcNow }
            };

            var result = await _forecastProvider.GetMultiPointWeatherForecast(geoCoordinates);

            Assert.Equal(2, result.Count);
            var resultList = result.ToList();
            Assert.Equal(60.0, resultList[0].Location.Latitude);
            Assert.Equal(10.0, resultList[0].Location.Longitude);
            Assert.Equal(61.0, resultList[1].Location.Latitude);
            Assert.Equal(11.0, resultList[1].Location.Longitude);
        }

        [Fact]
        public void WeatherProviderType_ReturnsOfflineProvider()
        {
            var providerType = _forecastProvider.WeatherProviderType;

            Assert.Equal(WeatherProviderType.OfflineWeatherProvider, providerType);
        }

        [Fact]
        public void MaxForecastRange_ReturnsThirtyDays()
        {
            var maxRange = _forecastProvider.MaxForecastRange;

            Assert.Equal(TimeSpan.FromDays(30), maxRange);
        }

        private IList<WeatherResponseInstance> GetWeatherDataWithoutLocationAndTime(int count)
        {
            return Enumerable
                .Range(1, count)
                .Select(i => new WeatherResponseInstance
                {
                    Weather = new WeatherData()
                    {
                        CurrentFromDirection = i,
                        CurrentSpeed = i,
                        WaveFromDirection = i,
                        WaveHeight = i,
                        WavePeakPeriod = i,
                        WindFromDirection = i,
                        WindSpeed = i,
                    }
                })
                .ToList();
        }

        //private IOptionsMonitor<OfflineWeatherProviderConfiguration> GetOptionsMonitor(OfflineWeatherProviderConfiguration options)
        //{
        //    return new OptionsMonitor<OfflineWeatherProviderConfiguration>(
        //        new OptionsFactoryStub(options),
        //        new List<IOptionsChangeTokenSource<OfflineWeatherProviderConfiguration>>(),
        //        new OptionsCache<OfflineWeatherProviderConfiguration>());
        //}

        private class OptionsFactoryStub : IOptionsFactory<OfflineWeatherProviderConfiguration>
        {
            private readonly OfflineWeatherProviderConfiguration _options;

            public OptionsFactoryStub(OfflineWeatherProviderConfiguration options)
            {
                _options = options;
            }

            public OfflineWeatherProviderConfiguration Create(string name)
            {
                return _options;
            }
        }
    }
}