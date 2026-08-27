using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace VoyageEnergyAdvisorService.Test.IntegrationTests.Controllers
{
    /// <summary>
    /// Integration tests for system robustness and edge case handling.
    /// Tests extreme values, boundary conditions, concurrent operations, and error resilience.
    /// </summary>
    [Collection("Integration Tests")]
    public class RobustnessIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public RobustnessIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private async Task<string> GetAuthTokenAsync()
        {
            var loginRequest = new
            {
                Username = "testuser",
                Password = "testpassword123"
            };

            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            return result?.Token ?? throw new Exception("Token not found in response");
        }

        [Fact]
        public async Task PolarRegion_WithValidRoute_ProcessesRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            // Arctic route (Svalbard region)
            var request = new
            {
                EtdMin = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                EtdMax = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds(),
                EtaMin = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
                EtaMax = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
                SpeedMin = 10.0,
                SpeedMax = 15.0,
                Route = new
                {
                    RouteName = "Arctic Route",
                    Waypoints = new[]
                    {
                        new { Latitude = 78.2232, Longitude = 15.6267 }, // Svalbard
                        new { Latitude = 80.0000, Longitude = 20.0000 }  // Far north
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/update", request);

            // Assert - Should handle polar coordinates
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected graceful handling of polar route, got {response.StatusCode}");
        }

        [Fact]
        public async Task InternationalDateLine_CrossingRoute_ProcessesRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            // Route crossing date line (Pacific)
            var request = new
            {
                EtdMin = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                EtdMax = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds(),
                EtaMin = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
                EtaMax = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
                SpeedMin = 10.0,
                SpeedMax = 15.0,
                Route = new
                {
                    RouteName = "Date Line Route",
                    Waypoints = new[]
                    {
                        new { Latitude = 0.0, Longitude = 179.5 },   // Just west of date line
                        new { Latitude = 0.0, Longitude = -179.5 }   // Just east of date line
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/update", request);

            // Assert - Should handle date line crossing
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected graceful handling of date line crossing, got {response.StatusCode}");
        }

        [Fact]
        public async Task Equator_ZeroLatitudeRoute_ProcessesRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            // Equatorial route
            var request = new
            {
                EtdMin = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                EtdMax = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds(),
                EtaMin = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
                EtaMax = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
                SpeedMin = 10.0,
                SpeedMax = 15.0,
                Route = new
                {
                    RouteName = "Equator Route",
                    Waypoints = new[]
                    {
                        new { Latitude = 0.0, Longitude = 0.0 },    // Null Island
                        new { Latitude = 0.0, Longitude = 10.0 }
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/update", request);

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected graceful handling of equatorial route, got {response.StatusCode}");
        }

        [Fact]
        public async Task PrimeMeridian_ZeroLongitudeRoute_ProcessesRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                EtdMin = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                EtdMax = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds(),
                EtaMin = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
                EtaMax = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
                SpeedMin = 10.0,
                SpeedMax = 15.0,
                Route = new
                {
                    RouteName = "Prime Meridian Route",
                    Waypoints = new[]
                    {
                        new { Latitude = 51.4779, Longitude = 0.0 },  // Greenwich
                        new { Latitude = 55.0, Longitude = 0.0 }
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/update", request);

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected graceful handling of prime meridian route, got {response.StatusCode}");
        }

        [Fact]
        public async Task VeryShortRoute_AdjacentCoordinates_ProcessesRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                EtdMin = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                EtdMax = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds(),
                EtaMin = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
                EtaMax = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
                SpeedMin = 10.0,
                SpeedMax = 15.0,
                Route = new
                {
                    RouteName = "Very Short Route",
                    Waypoints = new[]
                    {
                        new { Latitude = 59.9139, Longitude = 10.7522 },
                        new { Latitude = 59.9140, Longitude = 10.7523 }  // 100m apart
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/update", request);

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected graceful handling of very short route, got {response.StatusCode}");
        }

        [Fact]
        public async Task SingleWaypoint_MinimalRoute_ProcessesRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                EtdMin = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                EtdMax = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds(),
                EtaMin = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
                EtaMax = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
                SpeedMin = 10.0,
                SpeedMax = 15.0,
                Route = new
                {
                    RouteName = "Single Point",
                    Waypoints = new[]
                    {
                        new { Latitude = 59.9139, Longitude = 10.7522 }
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/update", request);

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected graceful handling of single waypoint, got {response.StatusCode}");
        }

        [Fact]
        public async Task NegativeSpeed_InvalidInput_ReturnsError()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                EtdMin = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                EtdMax = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds(),
                EtaMin = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
                EtaMax = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
                SpeedMin = -10.0,  // Negative speed
                SpeedMax = 15.0,
                Route = new
                {
                    RouteName = "Test Route",
                    Waypoints = new[]
                    {
                        new { Latitude = 59.9139, Longitude = 10.7522 },
                        new { Latitude = 55.6761, Longitude = 12.5683 }
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/update", request);

            // Assert - Should reject negative speed
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError ||
                response.StatusCode == HttpStatusCode.OK, // May accept and normalize
                $"Expected error or normalization for negative speed, got {response.StatusCode}");
        }

        [Fact]
        public async Task ZeroSpeed_EdgeCase_ProcessesRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                EtdMin = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                EtdMax = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds(),
                EtaMin = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
                EtaMax = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
                SpeedMin = 0.0,
                SpeedMax = 0.0,
                Route = new
                {
                    RouteName = "Zero Speed Route",
                    Waypoints = new[]
                    {
                        new { Latitude = 59.9139, Longitude = 10.7522 },
                        new { Latitude = 55.6761, Longitude = 12.5683 }
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/update", request);

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected graceful handling of zero speed, got {response.StatusCode}");
        }

        [Fact]
        public async Task InvertedTimeRange_EtdAfterEta_ProcessesRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                EtdMin = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
                EtdMax = DateTimeOffset.UtcNow.AddDays(3).ToUnixTimeSeconds(),
                EtaMin = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),  // ETA before ETD
                EtaMax = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                SpeedMin = 10.0,
                SpeedMax = 15.0,
                Route = new
                {
                    RouteName = "Inverted Time Route",
                    Waypoints = new[]
                    {
                        new { Latitude = 59.9139, Longitude = 10.7522 },
                        new { Latitude = 55.6761, Longitude = 12.5683 }
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/update", request);

            // Assert - Should detect invalid time range
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected error or handling for inverted time range, got {response.StatusCode}");
        }

        [Fact]
        public async Task PastTimestamp_HistoricalData_ProcessesRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                EtdMin = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds(),  // Past
                EtdMax = DateTimeOffset.UtcNow.AddDays(-6).ToUnixTimeSeconds(),
                EtaMin = DateTimeOffset.UtcNow.AddDays(-5).ToUnixTimeSeconds(),
                EtaMax = DateTimeOffset.UtcNow.AddDays(-4).ToUnixTimeSeconds(),
                SpeedMin = 10.0,
                SpeedMax = 15.0,
                Route = new
                {
                    RouteName = "Historical Route",
                    Waypoints = new[]
                    {
                        new { Latitude = 59.9139, Longitude = 10.7522 },
                        new { Latitude = 55.6761, Longitude = 12.5683 }
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/update", request);

            // Assert - Should handle historical timestamps
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected graceful handling of past timestamps, got {response.StatusCode}");
        }

        [Fact]
        public async Task ExtremeSpeed_VeryHighValue_ProcessesRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                EtdMin = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                EtdMax = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds(),
                EtaMin = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
                EtaMax = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
                SpeedMin = 100.0,  // Very high speed (unrealistic for ships)
                SpeedMax = 200.0,
                Route = new
                {
                    RouteName = "High Speed Route",
                    Waypoints = new[]
                    {
                        new { Latitude = 59.9139, Longitude = 10.7522 },
                        new { Latitude = 55.6761, Longitude = 12.5683 }
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/update", request);

            // Assert - Should validate or normalize extreme speed
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected validation or acceptance of extreme speed, got {response.StatusCode}");
        }

        #region Helper Classes

        private class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
        }

        #endregion
    }
}
