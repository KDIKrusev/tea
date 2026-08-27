using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.WebApi.Services;
using Xunit;

namespace VoyageEnergyAdvisorService.Test;

public class JwtTokenServiceTests
{
    private const string TestSecretKey = "ThisIsASecretKeyForTestingPurposesOnly123456789";

    private Mock<IConfiguration> CreateConfigurationMock(string? secretKey = TestSecretKey)
    {
        var configMock = new Mock<IConfiguration>();
        var jwtSectionMock = new Mock<IConfigurationSection>();
        jwtSectionMock.Setup(s => s["SecretKey"]).Returns(secretKey);
        configMock.Setup(c => c.GetSection("JwtSettings")).Returns(jwtSectionMock.Object);
        return configMock;
    }

    [Fact]
    public void GenerateToken_ValidUserAndVessel_ReturnsValidToken()
    {
        // Arrange
        var configMock = CreateConfigurationMock();
        var service = new JwtTokenService(configMock.Object);
        var user = new CurrentUserDto { Id = "user123", Name = "Test User" };
        var vesselId = 42;

        // Act
        var token = service.GenerateToken(user, vesselId);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // Decode and validate token structure
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        Assert.Equal("Test User", jwtToken.Claims.First(c => c.Type == "unique_name").Value);
        Assert.Equal("user123", jwtToken.Claims.First(c => c.Type == "nameid").Value);
        Assert.Equal("42", jwtToken.Claims.First(c => c.Type == "VesselId").Value);
    }

    [Fact]
    public void GenerateToken_ValidInputs_TokenContainsCorrectClaims()
    {
        // Arrange
        var configMock = CreateConfigurationMock();
        var service = new JwtTokenService(configMock.Object);
        var user = new CurrentUserDto { Id = "admin-456", Name = "Admin User" };
        var vesselId = 999;

        // Act
        var token = service.GenerateToken(user, vesselId);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        var claims = jwtToken.Claims.ToList();

        // Verify all three required claims exist
        Assert.Contains(claims, c => c.Type == "unique_name" && c.Value == "Admin User");
        Assert.Contains(claims, c => c.Type == "nameid" && c.Value == "admin-456");
        Assert.Contains(claims, c => c.Type == "VesselId" && c.Value == "999");
    }

    [Fact]
    public void GenerateToken_ValidInputs_TokenExpiresInSixHours()
    {
        // Arrange
        var configMock = CreateConfigurationMock();
        var service = new JwtTokenService(configMock.Object);
        var user = new CurrentUserDto { Id = "user123", Name = "Test User" };
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = service.GenerateToken(user, 1);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        var afterGeneration = DateTime.UtcNow;

        var expectedExpiration = beforeGeneration.AddHours(6);
        var actualExpiration = jwtToken.ValidTo;

        // Allow 1 minute tolerance for test execution time
        var timeDifference = Math.Abs((actualExpiration - expectedExpiration).TotalMinutes);
        Assert.True(timeDifference < 1, $"Token expiration time difference is {timeDifference} minutes, expected < 1 minute");

        // Verify expiration is approximately 6 hours from now
        var hoursUntilExpiration = (actualExpiration - beforeGeneration).TotalHours;
        Assert.True(hoursUntilExpiration >= 5.99 && hoursUntilExpiration <= 6.01, 
            $"Token should expire in ~6 hours, but expires in {hoursUntilExpiration} hours");
    }

    [Fact]
    public void GenerateToken_NullVesselId_UsesZeroAsDefault()
    {
        // Arrange
        var configMock = CreateConfigurationMock();
        var service = new JwtTokenService(configMock.Object);
        var user = new CurrentUserDto { Id = "user123", Name = "Test User" };

        // Act
        var token = service.GenerateToken(user, null);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        var vesselIdClaim = jwtToken.Claims.First(c => c.Type == "VesselId");

        Assert.Equal("0", vesselIdClaim.Value);
    }

    [Fact]
    public void GenerateToken_MissingSecretKey_ThrowsArgumentNullException()
    {
        // Arrange
        var configMock = CreateConfigurationMock(secretKey: null);
        var service = new JwtTokenService(configMock.Object);
        var user = new CurrentUserDto { Id = "user123", Name = "Test User" };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => service.GenerateToken(user, 1));
        Assert.Contains("JWT SecretKey is missing!", exception.Message);
    }

    [Fact]
    public void GenerateToken_EmptySecretKey_ThrowsException()
    {
        // Arrange
        var configMock = CreateConfigurationMock(secretKey: string.Empty);
        var service = new JwtTokenService(configMock.Object);
        var user = new CurrentUserDto { Id = "user123", Name = "Test User" };

        // Act & Assert - Empty key throws ArgumentException from SymmetricSecurityKey constructor
        Assert.Throws<ArgumentException>(() => service.GenerateToken(user, 1));
    }

    [Fact]
    public void GenerateToken_ValidToken_CanBeDecodedAndValidated()
    {
        // Arrange
        var configMock = CreateConfigurationMock();
        var service = new JwtTokenService(configMock.Object);
        var user = new CurrentUserDto { Id = "test-user-789", Name = "John Doe" };
        var vesselId = 12345;

        // Act
        var token = service.GenerateToken(user, vesselId);

        // Assert - Decode and validate using JwtSecurityTokenHandler
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        // Validate Subject contains ClaimsIdentity
        Assert.NotNull(jwtToken.Claims);

        // Extract and validate each claim
        var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name");
        var nameIdentifierClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid");
        var vesselIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "VesselId");

        Assert.NotNull(nameClaim);
        Assert.Equal("John Doe", nameClaim.Value);

        Assert.NotNull(nameIdentifierClaim);
        Assert.Equal("test-user-789", nameIdentifierClaim.Value);

        Assert.NotNull(vesselIdClaim);
        Assert.Equal("12345", vesselIdClaim.Value);
    }

    [Fact]
    public void GenerateToken_ValidToken_UsesHmacSha256Algorithm()
    {
        // Arrange
        var configMock = CreateConfigurationMock();
        var service = new JwtTokenService(configMock.Object);
        var user = new CurrentUserDto { Id = "user123", Name = "Test User" };

        // Act
        var token = service.GenerateToken(user, 1);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        // Verify signing algorithm
        Assert.Equal("HS256", jwtToken.Header.Alg);
        Assert.Equal(SecurityAlgorithms.HmacSha256, jwtToken.SignatureAlgorithm);
    }

    [Fact]
    public void GenerateToken_MultipleTokens_EachHasUniqueExpiration()
    {
        // Arrange
        var configMock = CreateConfigurationMock();
        var service = new JwtTokenService(configMock.Object);
        var user = new CurrentUserDto { Id = "user123", Name = "Test User" };

        // Act - Generate two tokens with 1 second delay to ensure different expiration times
        var token1 = service.GenerateToken(user, 1);
        Thread.Sleep(1000); // 1 second delay to ensure different timestamps
        var token2 = service.GenerateToken(user, 1);

        // Assert - Tokens should be different
        Assert.NotEqual(token1, token2);

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken1 = tokenHandler.ReadJwtToken(token1);
        var jwtToken2 = tokenHandler.ReadJwtToken(token2);

        // Expiration times should be different (token2 expires slightly later)
        Assert.NotEqual(jwtToken1.ValidTo, jwtToken2.ValidTo);
        Assert.True(jwtToken2.ValidTo > jwtToken1.ValidTo);
    }

    [Fact]
    public void GenerateToken_DifferentVesselIds_GeneratesDifferentTokens()
    {
        // Arrange
        var configMock = CreateConfigurationMock();
        var service = new JwtTokenService(configMock.Object);
        var user = new CurrentUserDto { Id = "user123", Name = "Test User" };

        // Act
        var token1 = service.GenerateToken(user, 1);
        var token2 = service.GenerateToken(user, 2);

        // Assert
        Assert.NotEqual(token1, token2);

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken1 = tokenHandler.ReadJwtToken(token1);
        var jwtToken2 = tokenHandler.ReadJwtToken(token2);

        var vesselId1 = jwtToken1.Claims.First(c => c.Type == "VesselId").Value;
        var vesselId2 = jwtToken2.Claims.First(c => c.Type == "VesselId").Value;

        Assert.Equal("1", vesselId1);
        Assert.Equal("2", vesselId2);
    }

    [Fact]
    public void GenerateToken_ValidToken_CanBeValidatedWithSecretKey()
    {
        // Arrange
        var configMock = CreateConfigurationMock();
        var service = new JwtTokenService(configMock.Object);
        var user = new CurrentUserDto { Id = "user123", Name = "Test User" };

        // Act
        var token = service.GenerateToken(user, 1);

        // Assert - Validate token signature
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(TestSecretKey);
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };

        // Should not throw exception if token is valid
        var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

        Assert.NotNull(principal);
        Assert.NotNull(validatedToken);
        Assert.IsType<JwtSecurityToken>(validatedToken);
    }
}
