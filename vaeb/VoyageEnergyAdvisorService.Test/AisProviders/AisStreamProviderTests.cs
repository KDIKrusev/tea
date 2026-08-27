using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using VoyageEnergyAdvisor.Core.Repositories;
using VoyageEnergyAdvisor.Core.Services.AisProviders;
using VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels;
using VoyageEnergyAdvisor.Core.Services.CacheService;
using Xunit;

namespace VoyageEnergyAdvisorService.Test.AisProviders;

/// <summary>
/// Unit tests for AisStreamProvider.
/// Tests cache retrieval, stale data handling, and validation.
/// </summary>
public class AisStreamProviderTests
{
    private readonly Mock<IConfigurationRepository> _mockConfigRepo;
    private readonly Mock<ILogger<AisStreamProvider>> _mockLogger;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IServiceProvider> _mockServiceProvider;

    public AisStreamProviderTests()
    {
        _mockConfigRepo = new Mock<IConfigurationRepository>();
        _mockLogger = new Mock<ILogger<AisStreamProvider>>();
        _mockCacheService = new Mock<ICacheService>();
        _mockServiceProvider = new Mock<IServiceProvider>();

        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(ICacheService)))
            .Returns(_mockCacheService.Object);
    }

    private AisStreamProviderConfiguration CreateTestConfig(params string[] mmsiList)
    {
        return new AisStreamProviderConfiguration
        {
            ApiKey = "test-key",
            FilterShipMMSI = mmsiList,
            FilterMessageTypes = new[] { "PositionReport" },
            GlobalBoundingBox = new[] { new[] { 0.0, 0.0 }, new[] { 90.0, 90.0 } },
            MaxReconnectAttempts = 5,
            ReconnectDelayMs = 1000
        };
    }

    [Fact]
    public void Constructor_ValidConfiguration_InitializesSuccessfully()
    {
        // Arrange
        var config = CreateTestConfig("123456789");
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<AisStreamProviderConfiguration>())
            .ReturnsAsync(config);

        // Act
        var provider = new AisStreamProvider(
            _mockConfigRepo.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object);

        // Assert
        Assert.Equal(AisProviderType.AisStreamProvider, provider.AisProviderType);
    }

    [Fact]
    public void Constructor_NullConfiguration_ThrowsException()
    {
        // Arrange
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<AisStreamProviderConfiguration>())
            .ReturnsAsync((AisStreamProviderConfiguration?)null);

        // Act & Assert
        Assert.Throws<Exception>(() => new AisStreamProvider(
            _mockConfigRepo.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void GetAisData_CachedFreshData_ReturnsData()
    {
        // Arrange
        var config = CreateTestConfig("123456789");
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<AisStreamProviderConfiguration>())
            .ReturnsAsync(config);

        var cachedData = new AisResponseInstance
        {
            MMSI = 123456789,
            VesselName = "Test Vessel",
            Latitude = 60.0,
            Longitude = 10.0,
            Speed = 15.0,
            PositionUpdatedAt = DateTime.UtcNow.AddMinutes(-10) // Fresh data
        };

        _mockCacheService
            .Setup(cs => cs.GenerateCacheKey("ais_vessel", "123456789"))
            .Returns("cache_key_123");

        _mockCacheService
            .Setup(cs => cs.TryGetCachedItem<AisResponseInstance>("cache_key_123", out It.Ref<AisResponseInstance?>.IsAny))
            .Callback(new TryGetCachedItemCallback((string key, out AisResponseInstance? value) => { value = cachedData; }))
            .Returns(true);

        var provider = new AisStreamProvider(
            _mockConfigRepo.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object);

        var request = new AisRequestInstance
        {
            VesselId = 1,
            VesselName = "Test Vessel",
            VesselNumber = "123456789"
        };

        // Act
        var result = provider.GetAisData(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.VesselId); // Should be updated from request
        Assert.Equal(123456789, result.MMSI);
        Assert.Equal("Test Vessel", result.VesselName);
    }

    private delegate void TryGetCachedItemCallback(string key, out AisResponseInstance? value);

    [Fact]
    public void GetAisData_StaleData_RemovesFromCacheAndReturnsNull()
    {
        // Arrange
        var config = CreateTestConfig("123456789");
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<AisStreamProviderConfiguration>())
            .ReturnsAsync(config);

        var staleData = new AisResponseInstance
        {
            MMSI = 123456789,
            VesselName = "Test Vessel",
            PositionUpdatedAt = DateTime.UtcNow.AddHours(-2) // Stale (> 1 hour)
        };

        _mockCacheService
            .Setup(cs => cs.GenerateCacheKey("ais_vessel", "123456789"))
            .Returns("cache_key_123");

        _mockCacheService
            .Setup(cs => cs.TryGetCachedItem<AisResponseInstance>("cache_key_123", out It.Ref<AisResponseInstance?>.IsAny))
            .Callback(new TryGetCachedItemCallback((string key, out AisResponseInstance? value) => { value = staleData; }))
            .Returns(true);

        var provider = new AisStreamProvider(
            _mockConfigRepo.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object);

        var request = new AisRequestInstance
        {
            VesselId = 1,
            VesselNumber = "123456789"
        };

        // Act
        var result = provider.GetAisData(request);

        // Assert
        Assert.Null(result);
        _mockCacheService.Verify(cs => cs.Remove("cache_key_123"), Times.Once);
    }

    [Fact]
    public void GetAisData_NoCachedData_ReturnsNull()
    {
        // Arrange
        var config = CreateTestConfig("123456789");
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<AisStreamProviderConfiguration>())
            .ReturnsAsync(config);

        _mockCacheService
            .Setup(cs => cs.GenerateCacheKey("ais_vessel", "123456789"))
            .Returns("cache_key_123");

        _mockCacheService
            .Setup(cs => cs.TryGetCachedItem<AisResponseInstance>("cache_key_123", out It.Ref<AisResponseInstance?>.IsAny))
            .Returns(false);

        var provider = new AisStreamProvider(
            _mockConfigRepo.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object);

        var request = new AisRequestInstance
        {
            VesselId = 1,
            VesselNumber = "123456789"
        };

        // Act
        var result = provider.GetAisData(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAisData_NullPositionUpdatedAt_TreatsAsFresh()
    {
        // Arrange
        var config = CreateTestConfig("123456789");
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<AisStreamProviderConfiguration>())
            .ReturnsAsync(config);

        var dataWithoutTimestamp = new AisResponseInstance
        {
            MMSI = 123456789,
            VesselName = "Test Vessel",
            PositionUpdatedAt = null
        };

        _mockCacheService
            .Setup(cs => cs.GenerateCacheKey("ais_vessel", "123456789"))
            .Returns("cache_key_123");

        _mockCacheService
            .Setup(cs => cs.TryGetCachedItem<AisResponseInstance>("cache_key_123", out It.Ref<AisResponseInstance?>.IsAny))
            .Callback(new TryGetCachedItemCallback((string key, out AisResponseInstance? value) => { value = dataWithoutTimestamp; }))
            .Returns(true);

        var provider = new AisStreamProvider(
            _mockConfigRepo.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object);

        var request = new AisRequestInstance
        {
            VesselId = 1,
            VesselNumber = "123456789"
        };

        // Act
        var result = provider.GetAisData(request);

        // Assert - When PositionUpdatedAt is null, dataAge = UtcNow - UtcNow = ~0, so data is fresh
        Assert.NotNull(result);
        Assert.Equal(123456789, result.MMSI);
    }

    [Fact]
    public void ValidateRequest_ValidRequest_DoesNotThrow()
    {
        // Arrange
        var config = CreateTestConfig("123456789");
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<AisStreamProviderConfiguration>())
            .ReturnsAsync(config);

        var provider = new AisStreamProvider(
            _mockConfigRepo.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object);

        var request = new AisRequestInstance
        {
            VesselId = 1,
            VesselNumber = "123456789"
        };

        // Act & Assert
        var exception = Record.Exception(() => provider.ValidateRequest(request));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateRequest_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var config = CreateTestConfig("123456789");
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<AisStreamProviderConfiguration>())
            .ReturnsAsync(config);

        var provider = new AisStreamProvider(
            _mockConfigRepo.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => provider.ValidateRequest(null!));
    }

    [Fact]
    public void ValidateRequest_EmptyMMSI_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateTestConfig(); // No MMSI in config
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<AisStreamProviderConfiguration>())
            .ReturnsAsync(config);

        var provider = new AisStreamProvider(
            _mockConfigRepo.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object);

        var request = new AisRequestInstance
        {
            VesselId = 1,
            VesselNumber = "" // Empty MMSI
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => provider.ValidateRequest(request));
    }

    [Fact]
    public void AisProviderType_ReturnsCorrectType()
    {
        // Arrange
        var config = CreateTestConfig("123456789");
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<AisStreamProviderConfiguration>())
            .ReturnsAsync(config);

        var provider = new AisStreamProvider(
            _mockConfigRepo.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object);

        // Act
        var providerType = provider.AisProviderType;

        // Assert
        Assert.Equal(AisProviderType.AisStreamProvider, providerType);
    }

    [Fact]
    public void GetAisData_UsesMMSIFromConfig_WhenRequestHasNoMMSI()
    {
        // Arrange
        var config = CreateTestConfig("999888777");
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<AisStreamProviderConfiguration>())
            .ReturnsAsync(config);

        _mockCacheService
            .Setup(cs => cs.GenerateCacheKey("ais_vessel", "999888777"))
            .Returns("cache_key_999");

        _mockCacheService
            .Setup(cs => cs.TryGetCachedItem<AisResponseInstance>("cache_key_999", out It.Ref<AisResponseInstance?>.IsAny))
            .Returns(false);

        var provider = new AisStreamProvider(
            _mockConfigRepo.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object);

        var request = new AisRequestInstance
        {
            VesselId = 1,
            VesselNumber = null // No MMSI in request, should use config
        };

        // Act
        var result = provider.GetAisData(request);

        // Assert
        _mockCacheService.Verify(cs => cs.GenerateCacheKey("ais_vessel", "999888777"), Times.Once);
    }
}
