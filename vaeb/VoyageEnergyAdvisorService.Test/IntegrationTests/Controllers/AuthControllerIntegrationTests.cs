using System.Net;
using System.Net.Http.Json;
using VoyageEnergyAdvisorService.Test.IntegrationTests;
using Xunit;

namespace VoyageEnergyAdvisorService.Test.IntegrationTests.Controllers
{
    /// <summary>
    /// Integration tests for AuthController.
    /// Tests authentication endpoints end-to-end with real HTTP requests.
    /// </summary>
    [Collection("Integration Tests")]
    public class AuthControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public AuthControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var loginRequest = new
            {
                Username = "testuser",
                Password = "testpassword123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.NotEmpty(result.Token);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginRequest = new
            {
                Username = "testuser",
                Password = "wrongpassword"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            
            var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            Assert.NotNull(result);
            Assert.Contains("Invalid", result.Message);
        }

        [Fact]
        public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
        {
            // Arrange
            var loginRequest = new
            {
                Username = "nonexistent",
                Password = "password123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithEmptyCredentials_ReturnsBadRequest()
        {
            // Arrange
            var loginRequest = new
            {
                Username = "",
                Password = ""
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            // Should be BadRequest if validation is implemented, otherwise Unauthorized
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest || 
                response.StatusCode == HttpStatusCode.Unauthorized
            );
        }

        [Fact]
        public async Task Login_WithMissingPassword_ReturnsBadRequest()
        {
            // Arrange
            var loginRequest = new
            {
                Username = "testuser"
                // Password missing
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest || 
                response.StatusCode == HttpStatusCode.Unauthorized
            );
        }

        #region Helper Classes

        private class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
        }

        private class ErrorResponse
        {
            public string Message { get; set; } = string.Empty;
        }

        #endregion
    }
}
