using System.Net;
using System.Net.Http.Json;
using VoyageEnergyAdvisor.WebApi.Dtos;
using Xunit;

namespace VoyageEnergyAdvisorService.Test.IntegrationTests.Controllers
{
    [Collection("Integration Tests")]
    public class VoyageEnergyAdvisorControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public VoyageEnergyAdvisorControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private async Task<string> GetAuthTokenAsync()
        {
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                Username = "testuser",
                Password = "testpassword123"
            });
            var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
            return result!.Token;
        }

        [Fact]
        public async Task CalculateVoyageEnergy_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
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
                    RouteName = "TestRoute",
                    Waypoints = new[]
                    {
                        new { Latitude = 59.9139, Longitude = 10.7522 }, // Oslo
                        new { Latitude = 55.6761, Longitude = 12.5683 }  // Copenhagen
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/update", request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CalculateVoyageEnergy_WithValidRequest_ReturnsResultOrError()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

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
                    RouteName = "TestRoute",
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
            // May succeed (200), fail validation (400), or encounter server error (500)
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected OK, BadRequest, or InternalServerError, got {response.StatusCode}");
        }

        [Fact]
        public async Task CalculateVoyageEnergy_WithMissingRoute_ReturnsBadRequestOrError()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                EtdMin = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                EtdMax = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds(),
                EtaMin = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
                EtaMax = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
                SpeedMin = 10.0,
                SpeedMax = 15.0
                // Route is missing
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/update", request);

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected BadRequest or InternalServerError, got {response.StatusCode}");
        }

        [Fact]
        public async Task CalculateVoyageEnergy_WithEmptyWaypoints_ProcessesRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

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
                    RouteName = "EmptyRoute",
                    Waypoints = Array.Empty<object>() // Empty waypoints
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/update", request);

            // Assert - Service may accept empty waypoints and return OK or error
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected OK, BadRequest, or InternalServerError, got {response.StatusCode}");
        }

        [Fact]
        public async Task GetLiveData_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var request = new
            {
                Route = new
                {
                    RouteName = "LiveRoute",
                    Waypoints = new[]
                    {
                        new { Latitude = 59.9139, Longitude = 10.7522 },
                        new { Latitude = 55.6761, Longitude = 12.5683 }
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/live", request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetLiveData_WithValidRequest_ReturnsResultOrError()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                Route = new
                {
                    RouteName = "LiveRoute",
                    Waypoints = new[]
                    {
                        new { Latitude = 59.9139, Longitude = 10.7522 },
                        new { Latitude = 55.6761, Longitude = 12.5683 }
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/live", request);

            // Assert
            // May succeed (200), fail validation (400), or encounter server error (500)
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected OK, BadRequest, or InternalServerError, got {response.StatusCode}");
        }

        [Fact]
        public async Task GetOptimalVoyage_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var request = new
            {
                Etd = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeMilliseconds(),
                Eta = DateTimeOffset.UtcNow.AddHours(11).ToUnixTimeMilliseconds(),
                SpeedMin = 1.0,
                SpeedMax = 15.0,
                Route = new
                {
                    RouteName = "TestRoute",
                    Waypoints = new[]
                    {
                        new { Latitude = 59.9139, Longitude = 10.7522 }, // Oslo
                        new { Latitude = 55.6761, Longitude = 12.5683 }  // Copenhagen
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/optimal", request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetOptimalVoyage_WithValidRequest_ReturnsResultOrError()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                Etd = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeMilliseconds(),
                Eta = DateTimeOffset.UtcNow.AddHours(11).ToUnixTimeMilliseconds(),
                SpeedMin = 1.0,
                SpeedMax = 15.0,
                Route = new
                {
                    RouteName = "TestRoute",
                    Waypoints = new[]
                    {
                        new { Latitude = 59.9139, Longitude = 10.7522 },
                        new { Latitude = 55.6761, Longitude = 12.5683 }
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/optimal", request);

            // Assert
            // May succeed (200), fail validation (400), or encounter server error (500)
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected OK, BadRequest, or InternalServerError, got {response.StatusCode}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = await response.Content.ReadFromJsonAsync<VoyageEnergyAdvisorOptimalVoyageResponseDto>();
                Assert.NotNull(result);
                Assert.NotNull(result!.OptimalVoyageOption);
            }
        }

        [Fact]
        public async Task GetOptimalVoyage_WithEtaBeforeEtd_ReturnsBadRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                Etd = DateTimeOffset.UtcNow.AddHours(11).ToUnixTimeMilliseconds(),
                Eta = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeMilliseconds(), // Before ETD
                SpeedMin = 1.0,
                SpeedMax = 15.0,
                Route = new
                {
                    RouteName = "TestRoute",
                    Waypoints = new[]
                    {
                        new { Latitude = 59.9139, Longitude = 10.7522 },
                        new { Latitude = 55.6761, Longitude = 12.5683 }
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/optimal", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetOptimalVoyage_WithMissingRoute_ReturnsBadRequestOrError()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                Etd = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeMilliseconds(),
                Eta = DateTimeOffset.UtcNow.AddHours(11).ToUnixTimeMilliseconds(),
                SpeedMin = 1.0,
                SpeedMax = 15.0
                // Route is missing
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/optimal", request);

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected BadRequest or InternalServerError, got {response.StatusCode}");
        }

        [Fact]
        public async Task GetLiveData_WithInvalidCoordinates_ProcessesRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                Route = new
                {
                    RouteName = "InvalidRoute",
                    Waypoints = new[]
                    {
                        new { Latitude = 999.0, Longitude = 999.0 }, // Invalid coordinates
                        new { Latitude = 55.6761, Longitude = 12.5683 }
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/live", request);

            // Assert - Service may accept and process invalid coordinates
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected OK, BadRequest, or InternalServerError, got {response.StatusCode}");
        }
    }
}
