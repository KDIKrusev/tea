using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Models;
using VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models;

namespace VoyageEnergyAdvisorService.Test.Weather.MeteomaticsWeatherForecastProvider
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Text;
    using Microsoft.Extensions.Options;
    using Moq;
    using VoyageEnergyAdvisor.Core.Services.WeatherService.Exceptions;
    using Xunit;

    /// <summary>
    /// Tests for MeteomaticsWeatherForecastProvider covering:
    /// - Happy path integration (existing)
    /// - Error scenarios (invalid locations, HTTP errors, empty responses)
    /// - Edge cases (timestamp clamping, duplicate handling)
    /// - Configuration and null handling
    /// </summary>
    public class MeteomaticsWeatherForecastProviderTests
    {
        private readonly HttpClient _httpClient = new(new MyHttpMessageHandlerMock());
        //private readonly HttpClient _httpClient = new(); // Test code. Uncomment to use actual http client.
        private readonly Mock<IOptions<MeteomaticsWeatherProviderConfiguration>> _options = new();
        private static HttpRequestMessage _prevRequestMessage = new(HttpMethod.Get, string.Empty);
        
        public MeteomaticsWeatherForecastProviderTests()
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            _options.Setup(e => e.Value).Returns(MockConfiguration);
            _prevRequestMessage = new();
        }
        
        private class MyHttpMessageHandlerMock : DelegatingHandler // Cumbersome solution as GetAsync is not directly overridable 
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                _prevRequestMessage = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)
                {
                    Content = new StringContent(File.ReadAllText("../../../Weather/MeteomaticsWeatherForecastProvider/MeteomaticsWeatherForecastResponse.json"))
                });
            }
        }


        [Fact]
        public async Task TestGetMultiPointWeatherForecast()
        {
            // Arrange
            var provider = new VoyageEnergyAdvisor.Core.Services.WeatherProviders
                .MeteomaticsWeatherForecastProvider(_httpClient, _options.Object);

            var geoCoordinates = TestPoints
                .Select(e => new WeatherRequestInstance
                {
                    Time = e.input.Time,
                    Location = e.input.Location
                })
                .ToList();

            // Act
            var results = await provider.GetMultiPointWeatherForecast(geoCoordinates);

            // Assert
            var actualUri = _prevRequestMessage!.RequestUri!.ToString();

            Assert.StartsWith("https://api.meteomatics.com/", actualUri);
            Assert.Contains("max_individual_wave_height:m", actualUri);
            Assert.Contains("wind_speed_10m:ms", actualUri);
            Assert.Contains("json?route=true", actualUri);

            Assert.Equal(ExpectedAuthHeader, _prevRequestMessage.Headers.Authorization);


            for (int i = 0; i < TestPoints.Count; i++)
            {
                var testPoint = TestPoints[i];
                var input = testPoint.input;
                var expectedOutput = testPoint.expectedOutput;
                var res = results[i];

                Assert.Equal(input.Location.Latitude, res.Location.Latitude);
                Assert.Equal(input.Location.Longitude, res.Location.Longitude);

                // --- Weather data ---
                Assert.Equal(expectedOutput.Weather.WindFromDirection!.Value, res.Weather.WindFromDirection!.Value);
                Assert.Equal(expectedOutput.Weather.WindSpeed!.Value, res.Weather.WindSpeed!.Value);
                Assert.Equal(expectedOutput.Weather.CurrentFromDirection!.Value, res.Weather.CurrentFromDirection!.Value);
                Assert.Equal(expectedOutput.Weather.CurrentSpeed!.Value, res.Weather.CurrentSpeed!.Value);
                Assert.Equal(expectedOutput.Weather.WavePeakPeriod!.Value, res.Weather.WavePeakPeriod!.Value);
                Assert.Equal(expectedOutput.Weather.WaveHeight!.Value, res.Weather.WaveHeight!.Value);
                Assert.Equal(expectedOutput.Weather.WaveFromDirection!.Value, res.Weather.WaveFromDirection!.Value);
            }
        }


        // ==================== ERROR SCENARIO TESTS ====================

        [Fact]
        public async Task GetMultiPointWeatherForecast_InvalidLocation_ThrowsWeatherForecastProviderException()
        {
            // Arrange
            var httpClient = new HttpClient(new InvalidLocationHttpHandlerMock());
            var provider = new VoyageEnergyAdvisor.Core.Services.WeatherProviders
                .MeteomaticsWeatherForecastProvider(httpClient, _options.Object);

            var requests = new List<WeatherRequestInstance>
            {
                new WeatherRequestInstance
                {
                    Location = new GeoCoordinate { Latitude = 40.7128, Longitude = -74.0060 }, // New York City (land)
                    Time = DateTime.UtcNow.AddHours(1)
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<WeatherForecastProviderException>(
                async () => await provider.GetMultiPointWeatherForecast(requests));

            Assert.Contains("-74.006", exception.Message);
            Assert.Contains("40.7128", exception.Message);
        }

        [Fact]
        public async Task GetMultiPointWeatherForecast_HttpError_ThrowsWeatherForecastProviderException()
        {
            // Arrange
            var httpClient = new HttpClient(new HttpErrorHandlerMock());
            var provider = new VoyageEnergyAdvisor.Core.Services.WeatherProviders
                .MeteomaticsWeatherForecastProvider(httpClient, _options.Object);

            var requests = new List<WeatherRequestInstance>
            {
                new WeatherRequestInstance
                {
                    Location = new GeoCoordinate { Latitude = 20.0, Longitude = -160.0 },
                    Time = DateTime.UtcNow.AddHours(1)
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<WeatherForecastProviderException>(
                async () => await provider.GetMultiPointWeatherForecast(requests));

            Assert.Contains("Internal Server Error", exception.Message);
        }

        [Fact]
        public async Task GetMultiPointWeatherForecast_EmptyResponse_ThrowsException()
        {
            // Arrange
            var httpClient = new HttpClient(new EmptyResponseHandlerMock());
            var provider = new VoyageEnergyAdvisor.Core.Services.WeatherProviders
                .MeteomaticsWeatherForecastProvider(httpClient, _options.Object);

            var requests = new List<WeatherRequestInstance>
            {
                new WeatherRequestInstance
                {
                    Location = new GeoCoordinate { Latitude = 20.0, Longitude = -160.0 },
                    Time = DateTime.UtcNow.AddHours(1)
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<WeatherForecastProviderException>(
                async () => await provider.GetMultiPointWeatherForecast(requests));

            Assert.Contains("No data returned from Meteomatics API", exception.Message);
        }

        [Fact]
        public async Task GetMultiPointWeatherForecast_MalformedJson_ThrowsException()
        {
            // Arrange
            var httpClient = new HttpClient(new MalformedJsonHandlerMock());
            var provider = new VoyageEnergyAdvisor.Core.Services.WeatherProviders
                .MeteomaticsWeatherForecastProvider(httpClient, _options.Object);

            var requests = new List<WeatherRequestInstance>
            {
                new WeatherRequestInstance
                {
                    Location = new GeoCoordinate { Latitude = 20.0, Longitude = -160.0 },
                    Time = DateTime.UtcNow.AddHours(1)
                }
            };

            // Act & Assert - JsonException or InvalidOperationException expected
            await Assert.ThrowsAnyAsync<Exception>(
                async () => await provider.GetMultiPointWeatherForecast(requests));
        }

        // ==================== TIMESTAMP EDGE CASE TESTS ====================

        [Fact]
        public async Task GetMultiPointWeatherForecast_PastTimestamps_ClampsToCurrentTime()
        {
            // Arrange
            var httpClient = new HttpClient(new UriCapturingHttpHandlerMock());
            var provider = new VoyageEnergyAdvisor.Core.Services.WeatherProviders
                .MeteomaticsWeatherForecastProvider(httpClient, _options.Object);

            var testStartTime = DateTime.UtcNow;
            var requests = new List<WeatherRequestInstance>
            {
                new WeatherRequestInstance
                {
                    Location = new GeoCoordinate { Latitude = 20.0, Longitude = -160.0 },
                    Time = DateTime.UtcNow.AddHours(-2) // 2 hours in the past
                }
            };

            // Act
            await provider.GetMultiPointWeatherForecast(requests);

            // Assert
            var capturedUri = _prevRequestMessage.RequestUri!.ToString();
            
            // Extract timestamp from URI (format: YYYY-MM-DDTHH:MM:SSZ)
            var timestampMatch = System.Text.RegularExpressions.Regex.Match(
                capturedUri, 
                @"(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z)");
            
            Assert.True(timestampMatch.Success, "Should find timestamp in URI");
            
            var extractedTime = DateTime.Parse(timestampMatch.Groups[1].Value, 
                null, 
                System.Globalization.DateTimeStyles.RoundtripKind);
            
            // Timestamp should be >= test start time (clamped, not in the past)
            Assert.True(extractedTime >= testStartTime.AddSeconds(-5), 
                $"Timestamp {extractedTime} should be >= test start {testStartTime}");
        }

        [Fact]
        public async Task GetMultiPointWeatherForecast_FutureTimestamps_ClampsToMaxRange()
        {
            // Arrange
            var httpClient = new HttpClient(new UriCapturingHttpHandlerMock());
            var provider = new VoyageEnergyAdvisor.Core.Services.WeatherProviders
                .MeteomaticsWeatherForecastProvider(httpClient, _options.Object);

            var testStartTime = DateTime.UtcNow;
            var maxFutureTime = testStartTime.Add(provider.MaxForecastRange);
            
            var requests = new List<WeatherRequestInstance>
            {
                new WeatherRequestInstance
                {
                    Location = new GeoCoordinate { Latitude = 20.0, Longitude = -160.0 },
                    Time = DateTime.UtcNow.AddDays(15) // 15 days in future (beyond 9-day max)
                }
            };

            // Act
            await provider.GetMultiPointWeatherForecast(requests);

            // Assert
            var capturedUri = _prevRequestMessage.RequestUri!.ToString();
            
            var timestampMatch = System.Text.RegularExpressions.Regex.Match(
                capturedUri, 
                @"(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z)");
            
            Assert.True(timestampMatch.Success, "Should find timestamp in URI");
            
            var extractedTime = DateTime.Parse(timestampMatch.Groups[1].Value, 
                null, 
                System.Globalization.DateTimeStyles.RoundtripKind);
            
            // Timestamp should be <= max forecast range
            Assert.True(extractedTime <= maxFutureTime.AddSeconds(5), 
                $"Timestamp {extractedTime} should be <= max range {maxFutureTime}");
        }

        [Fact]
        public async Task GetMultiPointWeatherForecast_DuplicateTimestamps_IncrementsBy1Second()
        {
            // Arrange
            var httpClient = new HttpClient(new UriCapturingHttpHandlerMock());
            var provider = new VoyageEnergyAdvisor.Core.Services.WeatherProviders
                .MeteomaticsWeatherForecastProvider(httpClient, _options.Object);

            var baseTime = DateTime.UtcNow.AddHours(1);
            var requests = new List<WeatherRequestInstance>
            {
                new WeatherRequestInstance
                {
                    Location = new GeoCoordinate { Latitude = 20.0, Longitude = -160.0 },
                    Time = baseTime
                },
                new WeatherRequestInstance
                {
                    Location = new GeoCoordinate { Latitude = 21.0, Longitude = -161.0 },
                    Time = baseTime // Same timestamp
                },
                new WeatherRequestInstance
                {
                    Location = new GeoCoordinate { Latitude = 22.0, Longitude = -162.0 },
                    Time = baseTime // Same timestamp again
                }
            };

            // Act
            await provider.GetMultiPointWeatherForecast(requests);

            // Assert
            var capturedUri = _prevRequestMessage.RequestUri!.ToString();
            
            // Extract all timestamps from URI
            var timestampMatches = System.Text.RegularExpressions.Regex.Matches(
                capturedUri, 
                @"(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z)");
            
            Assert.True(timestampMatches.Count >= 3, "Should have at least 3 timestamps");
            
            // Parse timestamps and verify they're sequential
            var times = timestampMatches
                .Take(3)
                .Select(m => DateTime.Parse(m.Groups[1].Value, 
                    null, 
                    System.Globalization.DateTimeStyles.RoundtripKind))
                .ToList();
            
            // Verify timestamps are unique and sequential (1 second apart)
            for (int i = 1; i < times.Count; i++)
            {
                var diff = (times[i] - times[i - 1]).TotalSeconds;
                Assert.InRange(diff, 0.9, 1.1); // Allow small floating point variance
            }
        }

        // ==================== CONFIGURATION & NULL HANDLING TESTS ====================

        [Fact]
        public async Task GetMultiPointWeatherForecast_NullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var provider = new VoyageEnergyAdvisor.Core.Services.WeatherProviders
                .MeteomaticsWeatherForecastProvider(_httpClient, _options.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await provider.GetMultiPointWeatherForecast(null!));
        }

        [Fact]
        public async Task GetMultiPointWeatherForecast_EmptyRequestList_ReturnsEmptyList()
        {
            // Arrange
            var provider = new VoyageEnergyAdvisor.Core.Services.WeatherProviders
                .MeteomaticsWeatherForecastProvider(_httpClient, _options.Object);

            var emptyRequests = new List<WeatherRequestInstance>();

            // Act
            var result = await provider.GetMultiPointWeatherForecast(emptyRequests);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void MaxForecastRange_ReturnsNineDays()
        {
            // Arrange
            var provider = new VoyageEnergyAdvisor.Core.Services.WeatherProviders
                .MeteomaticsWeatherForecastProvider(_httpClient, _options.Object);

            // Act
            var maxRange = provider.MaxForecastRange;

            // Assert
            Assert.Equal(TimeSpan.FromDays(9), maxRange);
        }

        // ==================== MOCK HTTP HANDLERS ====================

        private class InvalidLocationHttpHandlerMock : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                _prevRequestMessage = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText("../../../Weather/MeteomaticsWeatherForecastProvider/MeteomaticsWeatherForecastResponse-InvalidLocation.json"))
                });
            }
        }

        private class HttpErrorHandlerMock : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                _prevRequestMessage = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("Internal Server Error: API endpoint unavailable")
                });
            }
        }

        private class EmptyResponseHandlerMock : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                _prevRequestMessage = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText("../../../Weather/MeteomaticsWeatherForecastProvider/MeteomaticsWeatherForecastResponse-Empty.json"))
                });
            }
        }

        private class MalformedJsonHandlerMock : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                _prevRequestMessage = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{invalid json syntax, missing quotes: true")
                });
            }
        }

        private class UriCapturingHttpHandlerMock : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                _prevRequestMessage = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText("../../../Weather/MeteomaticsWeatherForecastProvider/MeteomaticsWeatherForecastResponse.json"))
                });
            }
        }


        private static readonly MeteomaticsWeatherProviderConfiguration MockConfiguration = new()
        {
            User = "test_user",
            Password = "test_password"
        };
        private static readonly AuthenticationHeaderValue ExpectedAuthHeader = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{MockConfiguration.User}:{MockConfiguration.Password}")));
        private static readonly List<(WeatherRequestInstance input, WeatherResponseInstance expectedOutput)> TestPoints = new()
        {
            (new WeatherRequestInstance()
                {
                    Location = new GeoCoordinate {Latitude = 20.0, Longitude = -160.0},
                    Time = DateTimeOffset.Parse("2023-11-20T04:20:02Z").DateTime
                },
                new WeatherResponseInstance()
                {
                    
                    //UpdatedAt = DateTimeOffset.Parse("2023-11-16T11:51:31Z").DateTime,
                    Location = new GeoCoordinate {Latitude = 20.0, Longitude = -160.0},
                    Time = DateTimeOffset.Parse("2023-11-20T04:20:02Z").DateTime,
                    Weather = new WeatherData()
                    {
                        WindFromDirection = 282.3,
                        WindSpeed = 3.2,
                        CurrentSpeed = 0.21,
                        CurrentFromDirection = 158.4,
                        WaveFromDirection = 142.9,
                        WaveHeight = 3.1,
                        WavePeakPeriod = 10.2,
                    }
                }
            ),
            (
                new WeatherRequestInstance()
                {
                    Location = new GeoCoordinate {Latitude = 0, Longitude = -140.5},
                    Time = DateTimeOffset.Parse("2023-11-21T03:31:02Z").DateTime,
                },
                new WeatherResponseInstance()
                {
                    //UpdatedAt = DateTimeOffset.Parse("2023-11-16T11:51:31Z").DateTime,
                    Location = new GeoCoordinate {Latitude = 0, Longitude = -140.5},
                    Time = DateTimeOffset.Parse("2023-11-21T03:31:02Z").DateTime,
                    Weather = new WeatherData()
                    {
                        WindFromDirection = 137.4,
                        WindSpeed = 4.8,
                        CurrentSpeed = 0.44,
                        CurrentFromDirection = 75.4 ,
                        WaveFromDirection = 66 ,
                        WaveHeight = 3.2,
                        WavePeakPeriod = 10.3,
                    }
                }
            ),
            (
                new WeatherRequestInstance
                {
                    Location = new GeoCoordinate {Latitude = -15, Longitude = -170.1},
                    Time = DateTimeOffset.Parse("2023-11-22T08:05:00Z").DateTime,
                },
                new WeatherResponseInstance()
                {
                    //UpdatedAt = DateTimeOffset.Parse("2023-11-16T11:51:31Z").DateTime,
                    Location = new GeoCoordinate {Latitude = -15, Longitude = -170.1},
                    Time = DateTimeOffset.Parse("2023-11-22T08:05:00Z").DateTime,
                    Weather = new WeatherData()
                    {
                        WindFromDirection = 118.2 ,
                        WindSpeed = 8.5,
                        CurrentSpeed = 0.26,
                        CurrentFromDirection = 88.2 ,
                        WaveFromDirection = 115.5 ,
                        WaveHeight = 4.9,
                        WavePeakPeriod = 10.0,
                    }
                }
            )
        };
    }
}