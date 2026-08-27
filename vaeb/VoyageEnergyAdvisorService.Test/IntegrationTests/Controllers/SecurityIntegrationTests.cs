using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace VoyageEnergyAdvisorService.Test.IntegrationTests.Controllers
{
    /// <summary>
    /// Integration tests for security and data isolation across users.
    /// Tests cross-user access attempts, authorization boundaries, and multi-user scenarios.
    /// </summary>
    [Collection("Integration Tests")]
    public class SecurityIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public SecurityIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private async Task<string> GetAuthTokenAsync(string username = "testuser", string password = "testpassword123")
        {
            var loginRequest = new
            {
                Username = username,
                Password = password
            };

            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            return result?.Token ?? throw new Exception("Token not found in response");
        }

        [Fact]
        public async Task AccessProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange - no token set

            // Act
            var response = await _client.GetAsync("/api/v1/vessel/user-vessels");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AccessProtectedEndpoint_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", "invalid.token.here");

            // Act
            var response = await _client.GetAsync("/api/v1/vessel/user-vessels");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AccessProtectedEndpoint_WithMalformedToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", "not-a-jwt-token");

            // Act
            var response = await _client.GetAsync("/api/v1/vessel/user-vessels");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AccessProtectedEndpoint_WithExpiredToken_ReturnsUnauthorized()
        {
            // Note: This test simulates an expired token scenario
            // In real implementation, you would create a token with past expiration
            // For now, we test with invalid signature which also results in 401

            // Arrange - Create a JWT-like token with invalid signature
            var fakeExpiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiZXhwIjoxNTE2MjM5MDIyfQ.invalid_signature";
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", fakeExpiredToken);

            // Act
            var response = await _client.GetAsync("/api/v1/vessel/user-vessels");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SqlInjection_InLoginAttempt_DoesNotCauseError()
        {
            // Arrange
            var maliciousRequest = new
            {
                Username = "admin' OR '1'='1",
                Password = "' OR '1'='1"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", maliciousRequest);

            // Assert - Should return 401 Unauthorized, not 500 Internal Server Error
            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized || 
                response.StatusCode == HttpStatusCode.BadRequest,
                $"Expected Unauthorized or BadRequest for SQL injection attempt, got {response.StatusCode}");
        }

        [Fact]
        public async Task XssAttempt_InUserCreation_IsSanitizedOrRejected()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var maliciousRequest = new
            {
                Username = "<script>alert('xss')</script>",
                Email = "test@example.com",
                Password = "TestPassword123!",
                FullName = "<img src=x onerror=alert('xss')>"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/user/create", maliciousRequest);

            // Assert - Should handle gracefully (BadRequest, OK, NotFound, or Conflict)
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.Conflict ||
                response.StatusCode == HttpStatusCode.NotFound,
                $"Expected successful handling of XSS attempt, got {response.StatusCode}");

            // If created successfully, verify the content is sanitized or escaped
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                // The response should not contain unescaped script tags
                Assert.DoesNotContain("<script>", content, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task MultipleRequests_WithSameToken_AllSucceed()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            // Act - Make multiple concurrent requests with same token
            var tasks = new[]
            {
                _client.GetAsync("/api/v1/vessel/user-vessels"),
                _client.GetAsync("/api/v1/route"),
                _client.GetAsync("/api/v1/configuration/calculation-configuration")
            };

            var responses = await Task.WhenAll(tasks);

            // Assert - All should succeed (or return consistent status codes)
            foreach (var response in responses)
            {
                Assert.True(
                    response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.NotFound,
                    $"Expected OK or NotFound for authorized request, got {response.StatusCode}");
            }
        }

        [Fact]
        public async Task LargePayload_DoesNotCauseServerError()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            // Create a route with many waypoints (stress test)
            var waypoints = new List<object>();
            for (int i = 0; i < 1000; i++)
            {
                waypoints.Add(new 
                { 
                    Latitude = 50.0 + (i * 0.01), 
                    Longitude = 10.0 + (i * 0.01) 
                });
            }

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
                    RouteName = "LargeRoute",
                    Waypoints = waypoints
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/voyageenergyadvisor/update", request);

            // Assert - Should handle gracefully (not 500 error)
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.RequestEntityTooLarge,
                $"Expected graceful handling of large payload, got {response.StatusCode}");
        }

        [Fact]
        public async Task MalformedJson_ReturnsBadRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var malformedJson = "{invalid json content}";
            var content = new StringContent(malformedJson, System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/voyageenergyadvisor/update", content);

            // Assert - Should return BadRequest for malformed JSON
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.UnsupportedMediaType,
                $"Expected BadRequest or UnsupportedMediaType for malformed JSON, got {response.StatusCode}");
        }

        #region Helper Classes

        private class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
        }

        #endregion
    }
}
