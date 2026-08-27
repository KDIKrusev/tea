using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace VoyageEnergyAdvisorService.Test.IntegrationTests.Controllers
{
    [Collection("Integration Tests")]
    public class ConfigurationControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public ConfigurationControllerIntegrationTests(CustomWebApplicationFactory factory)
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
        public async Task GetCalculationConfiguration_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/configuration/calculation-configuration");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCalculationConfiguration_WithAuthentication_ReturnsConfigOrNotFound()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/v1/configuration/calculation-configuration");

            // Assert
            // May return config (200) or not found (404) if no config exists for vessel
            Assert.True(
                response.StatusCode == HttpStatusCode.OK || 
                response.StatusCode == HttpStatusCode.NotFound,
                $"Expected OK or NotFound, got {response.StatusCode}");
        }

        [Fact]
        public async Task UpdateCalculationConfiguration_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var updateRequest = new
            {
                FuelPricePerKg = 1.5,
                EmissionFactorCO2PerKg = 3.2
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/v1/configuration/calculation-configuration", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateCalculationConfiguration_WithValidData_ReturnsSuccessOrNotFound()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var updateRequest = new
            {
                FuelPricePerKg = 1.5,
                EmissionFactorCO2PerKg = 3.2
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/v1/configuration/calculation-configuration", updateRequest);

            // Assert
            // May succeed (200) or not found (404) if config doesn't exist
            Assert.True(
                response.StatusCode == HttpStatusCode.OK || 
                response.StatusCode == HttpStatusCode.NotFound,
                $"Expected OK or NotFound, got {response.StatusCode}");
        }

        [Fact]
        public async Task UpdateCalculationConfiguration_WithNegativeFuelPrice_ReturnsBadRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var updateRequest = new
            {
                FuelPricePerKg = -1.5,
                EmissionFactorCO2PerKg = 3.2
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/v1/configuration/calculation-configuration", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateCalculationConfiguration_WithZeroFuelPrice_ReturnsBadRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var updateRequest = new
            {
                FuelPricePerKg = 0,
                EmissionFactorCO2PerKg = 3.2
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/v1/configuration/calculation-configuration", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateCalculationConfiguration_WithNoFields_ReturnsBadRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var updateRequest = new
            {
                // Empty - no fields to update
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/v1/configuration/calculation-configuration", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
