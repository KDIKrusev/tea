using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace VoyageEnergyAdvisorService.Test.IntegrationTests.Controllers
{
    [Collection("Integration Tests")]
    public class VesselControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public VesselControllerIntegrationTests(CustomWebApplicationFactory factory)
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
        public async Task GetUserVessels_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/vessel/user-vessels");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUserVessels_WithAuthentication_ReturnsVesselsOrNotFound()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/v1/vessel/user-vessels");

            // Assert
            // User may have vessels (200) or not (404), both are valid
            Assert.True(
                response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
                $"Expected OK or NotFound, got {response.StatusCode}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                Assert.NotNull(content);
            }
        }

        [Fact]
        public async Task SetCurrentVessel_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/vessel/set-current-vessel", 1);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SetCurrentVessel_WithAuthentication_ProcessesRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/vessel/set-current-vessel", 1);

            // Assert
            // May succeed (200) or fail (401/404/500) depending on vessel existence
            Assert.True(response.StatusCode != HttpStatusCode.InternalServerError || response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task SetCurrentVessel_WithInvalidVesselId_HandlesGracefully()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/vessel/set-current-vessel", -1);

            // Assert
            // Should handle invalid vessel ID gracefully (not crash)
            Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }
}
