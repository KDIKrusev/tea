using Microsoft.Extensions.Logging;
using Moq;
using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Repositories;
using VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models;
using VoyageEnergyAdvisor.Core.Services.WeatherProviders;
using VoyageEnergyAdvisor.Core.Services.WeatherService;
using Xunit;

namespace VoyageEnergyAdvisorService.Test.Weather
{
    /// <summary>
    /// Tests for WeatherService covering:
    /// - Constructor and provider selection logic
    /// - Cache integration (all cached, partial, none)
    /// - Batch processing with various request sizes
    /// - Progress callback functionality
    /// - Error scenarios and edge cases
    /// </summary>
    public class WeatherServiceTests
    {
        private readonly Mock<IConfigurationRepository> _mockConfigRepo;
        private readonly Mock<IWeatherCacheService> _mockCacheService;
        private readonly Mock<ILogger<WeatherService>> _mockLogger;

        public WeatherServiceTests()
        {
            _mockConfigRepo = new Mock<IConfigurationRepository>();
            _mockCacheService = new Mock<IWeatherCacheService>();
            _mockLogger = new Mock<ILogger<WeatherService>>();
        }

        // ==================== CONSTRUCTOR AND PROVIDER SELECTION TESTS ====================

        [Fact]
        public void Constructor_ValidConfiguration_SelectsCorrectProvider()
        {
            // Arrange
            var offlineProvider = CreateMockProvider(WeatherProviderType.OfflineWeatherProvider, TimeSpan.FromDays(7));
            var meteomaticsProvider = CreateMockProvider(WeatherProviderType.MeteomaticsWeatherProvider, TimeSpan.FromDays(9));
            var metProvider = CreateMockProvider(WeatherProviderType.MetWeatherProvider, TimeSpan.FromDays(10));

            var providers = new List<IWeatherProvider> { offlineProvider.Object, meteomaticsProvider.Object, metProvider.Object };

            var config = new WeatherServiceConfiguration
            {
                SelectedWeatherProvider = WeatherProviderType.MeteomaticsWeatherProvider
            };

            _mockConfigRepo
                .Setup(repo => repo.GetConfigurationAsync<WeatherServiceConfiguration>())
                .ReturnsAsync(config);

            // Act
            var service = new WeatherService(providers, _mockConfigRepo.Object, _mockCacheService.Object, _mockLogger.Object);

            // Assert
            Assert.Equal(TimeSpan.FromDays(9), service.MaxForecastRange);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MeteomaticsWeatherProvider")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Constructor_MissingConfiguration_ThrowsException()
        {
            // Arrange
            var providers = new List<IWeatherProvider>();

            _mockConfigRepo
                .Setup(repo => repo.GetConfigurationAsync<WeatherServiceConfiguration>())
                .ReturnsAsync((WeatherServiceConfiguration?)null);

            // Act & Assert
            var exception = Assert.Throws<Exception>(() =>
                new WeatherService(providers, _mockConfigRepo.Object, _mockCacheService.Object, _mockLogger.Object));

            Assert.Contains("Weather Service Configuration not found", exception.Message);
        }

        [Fact]
        public void Constructor_InvalidProviderType_ThrowsArgumentException()
        {
            // Arrange
            var offlineProvider = CreateMockProvider(WeatherProviderType.OfflineWeatherProvider, TimeSpan.FromDays(7));
            var providers = new List<IWeatherProvider> { offlineProvider.Object };

            var config = new WeatherServiceConfiguration
            {
                SelectedWeatherProvider = WeatherProviderType.MeteomaticsWeatherProvider // Not available in providers
            };

            _mockConfigRepo
                .Setup(repo => repo.GetConfigurationAsync<WeatherServiceConfiguration>())
                .ReturnsAsync(config);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new WeatherService(providers, _mockConfigRepo.Object, _mockCacheService.Object, _mockLogger.Object));

            Assert.Contains("MeteomaticsWeatherProvider", exception.Message);
            Assert.Contains("not available", exception.Message);
        }

        [Fact]
        public void Constructor_EmptyProviderList_ThrowsArgumentException()
        {
            // Arrange
            var providers = new List<IWeatherProvider>();

            var config = new WeatherServiceConfiguration
            {
                SelectedWeatherProvider = WeatherProviderType.MeteomaticsWeatherProvider
            };

            _mockConfigRepo
                .Setup(repo => repo.GetConfigurationAsync<WeatherServiceConfiguration>())
                .ReturnsAsync(config);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new WeatherService(providers, _mockConfigRepo.Object, _mockCacheService.Object, _mockLogger.Object));

            Assert.Contains("not available", exception.Message);
        }

        // ==================== CACHE INTEGRATION TESTS ====================

        [Fact]
        public async Task GetWeather_AllDataCached_ReturnsCachedDataOnly()
        {
            // Arrange
            var provider = CreateMockProvider(WeatherProviderType.MeteomaticsWeatherProvider, TimeSpan.FromDays(9));
            var service = CreateWeatherService(provider.Object);

            var requests = CreateWeatherRequests(5);
            var cachedResponses = requests.Select(r => new WeatherResponseInstance
            {
                Location = r.Location,
                Time = r.Time,
                Weather = CreateSampleWeatherData()
            }).ToList();

            _mockCacheService
                .Setup(cs => cs.GetCachedData(It.IsAny<IEnumerable<WeatherRequestInstance>>()))
                .Returns(cachedResponses);

            // Act
            var result = await service.GetWeather(requests);

            // Assert
            Assert.Equal(5, result.Count());
            provider.Verify(p => p.GetMultiPointWeatherForecast(It.IsAny<IList<WeatherRequestInstance>>()), Times.Never);
            _mockCacheService.Verify(cs => cs.AddCacheData(It.IsAny<IEnumerable<WeatherResponseInstance>>()), Times.Never);
        }

        [Fact]
        public async Task GetWeather_NoCachedData_FetchesFromProvider()
        {
            // Arrange
            var provider = CreateMockProvider(WeatherProviderType.StormglassWeatherProvider, TimeSpan.FromDays(9));
            var service = CreateWeatherService(provider.Object);

            var location = new GeoCoordinate { Latitude = 60.0, Longitude = 5.0 };
            var baseTime = DateTimeOffset.UtcNow.UtcDateTime;
            var requests = Enumerable.Range(0, 5)
                .Select(i => new WeatherRequestInstance
                {
                    Location = location,
                    Time = baseTime.AddHours(i)
                }).ToList();
            var fetchedResponses = requests.Select(r => new WeatherResponseInstance
            {
                Location = location,
                Time = r.Time,
                Weather = CreateSampleWeatherData()
            }).ToList();

            _mockCacheService
                .Setup(cs => cs.GetCachedData(It.IsAny<IEnumerable<WeatherRequestInstance>>()))
                .Returns(new List<WeatherResponseInstance>());

            provider
                .Setup(p => p.GetMultiPointWeatherForecast(It.IsAny<IList<WeatherRequestInstance>>()))
                .ReturnsAsync(fetchedResponses);

            // Act
            var result = await service.GetWeather(requests);

            // Assert
            Assert.Equal(5, result.Count());
            provider.Verify(p => p.GetMultiPointWeatherForecast(It.Is<IList<WeatherRequestInstance>>(r => r.Count == 5)), Times.Once);
            _mockCacheService.Verify(cs => cs.AddCacheData(It.Is<IEnumerable<WeatherResponseInstance>>(r => r.Count() == 5)), Times.Once);
        }

        [Fact]
        public async Task GetWeather_PartialCachedData_FetchesOnlyMissingData()
        {
            // Arrange
            var provider = CreateMockProvider(WeatherProviderType.MeteomaticsWeatherProvider, TimeSpan.FromDays(9));
            var service = CreateWeatherService(provider.Object);

            var location = new GeoCoordinate { Latitude = 60.0, Longitude = 5.0 };
            var baseTime = DateTimeOffset.UtcNow.UtcDateTime;
            var requests = Enumerable.Range(0, 5)
                .Select(i => new WeatherRequestInstance
                {
                    Location = location,
                    Time = baseTime.AddHours(i)
                }).ToList();

            // Cache responses for requests 0, 2, 4 (indices)
            var cachedResponses = new List<WeatherResponseInstance>
            {
                new WeatherResponseInstance { Location = location, Time = requests[0].Time, Weather = CreateSampleWeatherData() },
                new WeatherResponseInstance { Location = location, Time = requests[2].Time, Weather = CreateSampleWeatherData() },
                new WeatherResponseInstance { Location = location, Time = requests[4].Time, Weather = CreateSampleWeatherData() }
            };

            // Fetch responses for requests 1, 3 (the missing ones)
            var fetchedResponses = new List<WeatherResponseInstance>
            {
                new WeatherResponseInstance { Location = location, Time = requests[1].Time, Weather = CreateSampleWeatherData() },
                new WeatherResponseInstance { Location = location, Time = requests[3].Time, Weather = CreateSampleWeatherData() }
            };

            _mockCacheService
                .Setup(cs => cs.GetCachedData(It.IsAny<IEnumerable<WeatherRequestInstance>>()))
                .Returns(cachedResponses);

            provider
                .Setup(p => p.GetMultiPointWeatherForecast(It.IsAny<IList<WeatherRequestInstance>>()))
                .ReturnsAsync((IList<WeatherRequestInstance> reqs) =>
                    fetchedResponses.Where(fr => reqs.Any(r => r.Location == fr.Location && r.Time == fr.Time)).ToList());

            // Act
            var result = await service.GetWeather(requests);

            // Assert
            Assert.Equal(5, result.Count());
            provider.Verify(p => p.GetMultiPointWeatherForecast(It.Is<IList<WeatherRequestInstance>>(r => r.Count == 2)), Times.Once);
            _mockCacheService.Verify(cs => cs.AddCacheData(It.Is<IEnumerable<WeatherResponseInstance>>(r => r.Count() == 2)), Times.Once);

            // Verify we have data for all 5 requests
            var resultList = result.ToList();
            Assert.Contains(resultList, r => r.Location == requests[0].Location && r.Time == requests[0].Time);
            Assert.Contains(resultList, r => r.Location == requests[1].Location && r.Time == requests[1].Time);
            Assert.Contains(resultList, r => r.Location == requests[2].Location && r.Time == requests[2].Time);
            Assert.Contains(resultList, r => r.Location == requests[3].Location && r.Time == requests[3].Time);
            Assert.Contains(resultList, r => r.Location == requests[4].Location && r.Time == requests[4].Time);
        }

        [Fact]
        public async Task GetWeather_CacheKeyMatching_UsesLocationAndTimeTuples()
        {
            // Arrange
            var provider = CreateMockProvider(WeatherProviderType.MeteomaticsWeatherProvider, TimeSpan.FromDays(9));
            var service = CreateWeatherService(provider.Object);

            var location1 = new GeoCoordinate { Latitude = 60.0, Longitude = 5.0 };
            var location2 = new GeoCoordinate { Latitude = 61.0, Longitude = 6.0 };
            var time1 = DateTimeOffset.UtcNow.UtcDateTime;
            var time2 = DateTimeOffset.UtcNow.AddHours(1).UtcDateTime;

            var requests = new List<WeatherRequestInstance>
            {
                new WeatherRequestInstance { Location = location1, Time = time1 }, // Should be cached
                new WeatherRequestInstance { Location = location1, Time = time2 }, // Different time - not cached
                new WeatherRequestInstance { Location = location2, Time = time1 }, // Different location - not cached
                new WeatherRequestInstance { Location = location2, Time = time2 }  // Should be cached
            };

            var cachedResponses = new List<WeatherResponseInstance>
            {
                new WeatherResponseInstance { Location = location1, Time = time1, Weather = CreateSampleWeatherData() },
                new WeatherResponseInstance { Location = location2, Time = time2, Weather = CreateSampleWeatherData() }
            };

            var fetchedResponses = new List<WeatherResponseInstance>
            {
                new WeatherResponseInstance { Location = location1, Time = time2, Weather = CreateSampleWeatherData() },
                new WeatherResponseInstance { Location = location2, Time = time1, Weather = CreateSampleWeatherData() }
            };

            _mockCacheService
                .Setup(cs => cs.GetCachedData(It.IsAny<IEnumerable<WeatherRequestInstance>>()))
                .Returns(cachedResponses);

            provider
                .Setup(p => p.GetMultiPointWeatherForecast(It.IsAny<IList<WeatherRequestInstance>>()))
                .ReturnsAsync((IList<WeatherRequestInstance> reqs) =>
                    fetchedResponses.Where(fr => reqs.Any(r => r.Location == fr.Location && r.Time == fr.Time)).ToList());

            // Act
            var result = await service.GetWeather(requests);

            // Assert
            Assert.Equal(4, result.Count());
            provider.Verify(p => p.GetMultiPointWeatherForecast(It.Is<IList<WeatherRequestInstance>>(r => r.Count == 1 && r[0].Location.Equals(location1) && r[0].Time == time2)), Times.Once);
            provider.Verify(p => p.GetMultiPointWeatherForecast(It.Is<IList<WeatherRequestInstance>>(r => r.Count == 1 && r[0].Location.Equals(location2) && r[0].Time == time1)), Times.Once);
        }


        [Fact]
        public async Task GetWeather_250Requests_ProcessesInMultipleBatches()
        {
            // Arrange
            var provider = CreateMockProvider(WeatherProviderType.MeteomaticsWeatherProvider, TimeSpan.FromDays(9));
            var service = CreateWeatherService(provider.Object);

            var baseTime = DateTimeOffset.UtcNow.UtcDateTime;
            var locations = new[]
            {
                new GeoCoordinate { Latitude = 60.0, Longitude = 5.0 },
                new GeoCoordinate { Latitude = 61.0, Longitude = 6.0 },
                new GeoCoordinate { Latitude = 62.0, Longitude = 7.0 }
            };
            var requests = Enumerable.Range(0, 250)
                .Select(i => new WeatherRequestInstance
                {
                    Location = locations[i % 3],
                    Time = baseTime.AddHours(i)
                })
                .ToList();

            var batchSizes = new List<int>();

            _mockCacheService
                .Setup(cs => cs.GetCachedData(It.IsAny<IEnumerable<WeatherRequestInstance>>()))
                .Returns(new List<WeatherResponseInstance>());

            provider
                .Setup(p => p.GetMultiPointWeatherForecast(It.IsAny<IList<WeatherRequestInstance>>()))
                .ReturnsAsync((IList<WeatherRequestInstance> reqs) =>
                {
                    batchSizes.Add(reqs.Count);
                    return reqs.Select(r => new WeatherResponseInstance
                    {
                        Location = r.Location,
                        Time = r.Time,
                        Weather = CreateSampleWeatherData()
                    }).ToList();
                });

            // Act
            var result = await service.GetWeather(requests);

            // Assert
            Assert.Equal(250, result.Count());
            Assert.Equal(3, batchSizes.Count); 
            Assert.All(batchSizes, size => Assert.InRange(size, 83, 84));
        }

        [Fact]
        public async Task GetWeather_ProviderReturnsNulls_FiltersNullResults()
        {
            // Arrange
            var provider = CreateMockProvider(WeatherProviderType.StormglassWeatherProvider, TimeSpan.FromDays(9));
            var service = CreateWeatherService(provider.Object);

            var location = new GeoCoordinate { Latitude = 60.0, Longitude = 5.0 };
            var baseTime = DateTimeOffset.UtcNow.UtcDateTime;
            var requests = Enumerable.Range(0, 5)
                .Select(i => new WeatherRequestInstance
                {
                    Location = location,
                    Time = baseTime.AddHours(i)
                }).ToList();
            var responsesWithNulls = new List<WeatherResponseInstance?>
            {
                new WeatherResponseInstance { Location = location, Time = requests[0].Time, Weather = CreateSampleWeatherData() },
                null,
                new WeatherResponseInstance { Location = location, Time = requests[2].Time, Weather = CreateSampleWeatherData() },
                null,
                new WeatherResponseInstance { Location = location, Time = requests[4].Time, Weather = CreateSampleWeatherData() }
            };

            _mockCacheService
                .Setup(cs => cs.GetCachedData(It.IsAny<IEnumerable<WeatherRequestInstance>>()))
                .Returns(new List<WeatherResponseInstance>());

            provider
                .Setup(p => p.GetMultiPointWeatherForecast(It.IsAny<IList<WeatherRequestInstance>>()))
                .ReturnsAsync(responsesWithNulls!);

            // Act
            var result = await service.GetWeather(requests);

            // Assert
            Assert.Equal(3, result.Count()); // Only 3 non-null responses
            Assert.All(result, r => Assert.NotNull(r));
        }

        // ==================== PROGRESS CALLBACK TESTS ====================

        [Fact]
        public async Task GetWeather_WithProgressCallback_InvokesCallbackForEachBatch()
        {
            // Arrange
            var provider = CreateMockProvider(WeatherProviderType.StormglassWeatherProvider, TimeSpan.FromDays(9));
            var service = CreateWeatherService(provider.Object);

            var baseTime = DateTimeOffset.UtcNow.UtcDateTime;
            var locations = new[]
            {
                new GeoCoordinate { Latitude = 60.0, Longitude = 5.0 },
                new GeoCoordinate { Latitude = 61.0, Longitude = 6.0 },
                new GeoCoordinate { Latitude = 62.0, Longitude = 7.0 }
            };
            var requests = Enumerable.Range(0, 250)
                .Select(i => new WeatherRequestInstance
                {
                    Location = locations[i % 3],
                    Time = baseTime.AddHours(i)
                })
                .ToList();
            var progressValues = new List<double>();
            var progressMessages = new List<string>();

            _mockCacheService
                .Setup(cs => cs.GetCachedData(It.IsAny<IEnumerable<WeatherRequestInstance>>()))
                .Returns(new List<WeatherResponseInstance>());

            provider
                .Setup(p => p.GetMultiPointWeatherForecast(It.IsAny<IList<WeatherRequestInstance>>()))
                .ReturnsAsync((IList<WeatherRequestInstance> reqs) =>
                    reqs.Select(r => new WeatherResponseInstance
                    {
                        Location = r.Location,
                        Time = r.Time,
                        Weather = CreateSampleWeatherData()
                    }).ToList());

            Func<double, string, Task> callback = (progress, message) =>
            {
                progressValues.Add(progress);
                progressMessages.Add(message);
                return Task.CompletedTask;
            };

            // Act
            await service.GetWeather(requests, callback);

            // Assert
            Assert.Equal(3, progressValues.Count); // 3 batches = 3 callbacks
            Assert.All(progressValues, p => Assert.InRange(p, 5, 85)); // Within 5%-85% range
            Assert.True(progressValues.SequenceEqual(progressValues.OrderBy(p => p))); // Monotonically increasing
            Assert.All(progressMessages, m => Assert.Equal("Fetching weather data", m));
        }

        [Fact]
        public async Task GetWeather_NullProgressCallback_DoesNotThrow()
        {
            // Arrange
            var provider = CreateMockProvider(WeatherProviderType.MeteomaticsWeatherProvider, TimeSpan.FromDays(9));
            var service = CreateWeatherService(provider.Object);

            var requests = CreateWeatherRequests(250);

            _mockCacheService
                .Setup(cs => cs.GetCachedData(It.IsAny<IEnumerable<WeatherRequestInstance>>()))
                .Returns(new List<WeatherResponseInstance>());

            provider
                .Setup(p => p.GetMultiPointWeatherForecast(It.IsAny<IList<WeatherRequestInstance>>()))
                .ReturnsAsync((IList<WeatherRequestInstance> reqs) =>
                    reqs.Select(r => new WeatherResponseInstance
                    {
                        Location = r.Location,
                        Time = r.Time,
                        Weather = CreateSampleWeatherData()
                    }).ToList());

            // Act & Assert
            var result = await service.GetWeather(requests, null);
            Assert.Equal(250, result.Count());
        }

        [Fact]
        public async Task GetWeather_ProgressCalculation_MapsTo5To85PercentRange()
        {
            // Arrange
            var provider = CreateMockProvider(WeatherProviderType.StormglassWeatherProvider, TimeSpan.FromDays(9));
            var service = CreateWeatherService(provider.Object);

            var location = new GeoCoordinate { Latitude = 60.0, Longitude = 5.0 };
            var baseTime = DateTimeOffset.UtcNow.UtcDateTime;
            var requests = Enumerable.Range(0, 100)
                .Select(i => new WeatherRequestInstance
                {
                    Location = location,
                    Time = baseTime.AddHours(i)
                }).ToList();
            var progressValues = new List<double>();

            _mockCacheService
                .Setup(cs => cs.GetCachedData(It.IsAny<IEnumerable<WeatherRequestInstance>>()))
                .Returns(new List<WeatherResponseInstance>());

            provider
                .Setup(p => p.GetMultiPointWeatherForecast(It.IsAny<IList<WeatherRequestInstance>>()))
                .ReturnsAsync((IList<WeatherRequestInstance> reqs) =>
                    reqs.Select(r => new WeatherResponseInstance
                    {
                        Location = r.Location,
                        Time = r.Time,
                        Weather = CreateSampleWeatherData()
                    }).ToList());

            Func<double, string, Task> callback = (progress, message) =>
            {
                progressValues.Add(progress);
                return Task.CompletedTask;
            };

            // Act
            await service.GetWeather(requests, callback);

            // Assert
            Assert.Single(progressValues); // Single batch = single callback
            Assert.Equal(85, progressValues[0]); // 100% of 1 batch = 85% global progress
        }

        // ==================== DELEGATION AND ERROR SCENARIOS ====================

        [Fact]
        public void MaxForecastRange_ReturnsSelectedProviderMaxRange()
        {
            // Arrange
            var provider = CreateMockProvider(WeatherProviderType.MeteomaticsWeatherProvider, TimeSpan.FromDays(9));
            var service = CreateWeatherService(provider.Object);

            // Act
            var maxRange = service.MaxForecastRange;

            // Assert
            Assert.Equal(TimeSpan.FromDays(9), maxRange);
        }

        [Fact]
        public async Task GetWeather_ProviderThrowsException_PropagatesException()
        {
            // Arrange
            var provider = CreateMockProvider(WeatherProviderType.MeteomaticsWeatherProvider, TimeSpan.FromDays(9));
            var service = CreateWeatherService(provider.Object);

            var requests = CreateWeatherRequests(5);

            _mockCacheService
                .Setup(cs => cs.GetCachedData(It.IsAny<IEnumerable<WeatherRequestInstance>>()))
                .Returns(new List<WeatherResponseInstance>());

            provider
                .Setup(p => p.GetMultiPointWeatherForecast(It.IsAny<IList<WeatherRequestInstance>>()))
                .ThrowsAsync(new Exception("Provider error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => service.GetWeather(requests));
            Assert.Equal("Provider error", exception.Message);

            // Verify AddCacheData was not called after exception
            _mockCacheService.Verify(cs => cs.AddCacheData(It.IsAny<IEnumerable<WeatherResponseInstance>>()), Times.Never);
        }

        [Fact]
        public async Task GetWeather_EmptyRequestList_ReturnsEmptyResult()
        {
            // Arrange
            var provider = CreateMockProvider(WeatherProviderType.MeteomaticsWeatherProvider, TimeSpan.FromDays(9));
            var service = CreateWeatherService(provider.Object);

            var emptyRequests = new List<WeatherRequestInstance>();

            _mockCacheService
                .Setup(cs => cs.GetCachedData(It.IsAny<IEnumerable<WeatherRequestInstance>>()))
                .Returns(new List<WeatherResponseInstance>());

            // Act
            var result = await service.GetWeather(emptyRequests);

            // Assert
            Assert.Empty(result);
            provider.Verify(p => p.GetMultiPointWeatherForecast(It.IsAny<IList<WeatherRequestInstance>>()), Times.Never);
        }

        [Fact]
        public async Task GetWeather_CacheServiceThrowsException_PropagatesException()
        {
            // Arrange
            var provider = CreateMockProvider(WeatherProviderType.MeteomaticsWeatherProvider, TimeSpan.FromDays(9));
            var service = CreateWeatherService(provider.Object);

            var requests = CreateWeatherRequests(5);

            _mockCacheService
                .Setup(cs => cs.GetCachedData(It.IsAny<IEnumerable<WeatherRequestInstance>>()))
                .Throws(new Exception("Cache error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => service.GetWeather(requests));
            Assert.Equal("Cache error", exception.Message);
        }

        // ==================== HELPER METHODS ====================

        private Mock<IWeatherProvider> CreateMockProvider(WeatherProviderType type, TimeSpan maxRange)
        {
            var mock = new Mock<IWeatherProvider>();
            mock.Setup(p => p.WeatherProviderType).Returns(type);
            mock.Setup(p => p.MaxForecastRange).Returns(maxRange);
            mock.Setup(p => p.GetMultiPointWeatherForecast(It.IsAny<IList<WeatherRequestInstance>>()))
                .ReturnsAsync((IList<WeatherRequestInstance> requests) =>
                    requests.Select(r => new WeatherResponseInstance
                    {
                        Location = r.Location,
                        Time = r.Time,
                        Weather = CreateSampleWeatherData()
                    }).ToList());
            return mock;
        }

        private WeatherService CreateWeatherService(IWeatherProvider provider)
        {
            var providers = new List<IWeatherProvider> { provider };

            var config = new WeatherServiceConfiguration
            {
                SelectedWeatherProvider = provider.WeatherProviderType
            };

            _mockConfigRepo
                .Setup(repo => repo.GetConfigurationAsync<WeatherServiceConfiguration>())
                .ReturnsAsync(config);

            return new WeatherService(providers, _mockConfigRepo.Object, _mockCacheService.Object, _mockLogger.Object);
        }

        private List<WeatherRequestInstance> CreateWeatherRequests(int count)
        {
            var baseTime = DateTimeOffset.UtcNow.UtcDateTime;
            return Enumerable.Range(0, count)
                .Select(i => new WeatherRequestInstance
                {
                    Location = new GeoCoordinate
                    {
                        Latitude = 60.0 + i * 0.1,
                        Longitude = 5.0 + i * 0.1
                    },
                    Time = baseTime.AddHours(i) 
                })
                .ToList();
        }

        private WeatherData CreateSampleWeatherData()
        {
            return new WeatherData
            {
                WindSpeed = 10.0,
                WindFromDirection = 180.0,
                CurrentSpeed = 1.0,
                CurrentFromDirection = 90.0,
                WaveHeight = 2.0,
                WaveFromDirection = 180.0,
                WavePeakPeriod = 8.0
            };
        }
    }
}
