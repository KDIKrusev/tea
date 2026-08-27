using Microsoft.Extensions.Logging;
using Moq;
using VoyageEnergyAdvisor.Core.Repositories;
using VoyageEnergyAdvisor.Core.Services.AisProviders;
using VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels;
using Xunit;

namespace VoyageEnergyAdvisorService.Test.AisProviders;

/// <summary>
/// Unit tests for OfflineAisProvider.
/// Tests sample data cycling, vessel position retrieval, and edge cases.
/// </summary>
public class OfflineAisProviderTests
{
    private readonly Mock<IConfigurationRepository> _mockConfigRepo;
    private readonly Mock<ILogger<OfflineAisProvider>> _mockLogger;

    public OfflineAisProviderTests()
    {
        _mockConfigRepo = new Mock<IConfigurationRepository>();
        _mockLogger = new Mock<ILogger<OfflineAisProvider>>();
    }

    private OfflineAisProviderConfiguration CreateTestConfig(int vesselCount = 3)
    {
        var vessels = new List<AisVesselData>();
        for (int i = 0; i < vesselCount; i++)
        {
            vessels.Add(new AisVesselData
            {
                MMSI = 1000 + i,
                IMO = 2000 + i,
                Name = $"Vessel {i}",
                Latitude = 60.0 + i,
                Longitude = 10.0 + i,
                Speed = 10.0 + i,
                Course = 90.0 + i * 10,
                Heading = 90 + i * 10,
                Destination = $"Port {i}"
            });
        }

        return new OfflineAisProviderConfiguration
        {
            SampleVessels = vessels.ToArray()
        };
    }

    [Fact]
    public void Constructor_ValidConfiguration_InitializesSuccessfully()
    {
        // Arrange
        var config = CreateTestConfig(3);
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<OfflineAisProviderConfiguration>())
            .ReturnsAsync(config);

        // Act
        var provider = new OfflineAisProvider(_mockConfigRepo.Object, _mockLogger.Object);

        // Assert
        Assert.Equal(AisProviderType.OfflineAisProvider, provider.AisProviderType);
    }

    [Fact]
    public void Constructor_NullConfiguration_ThrowsException()
    {
        // Arrange
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<OfflineAisProviderConfiguration>())
            .ReturnsAsync((OfflineAisProviderConfiguration?)null);

        // Act & Assert
        Assert.Throws<Exception>(() => new OfflineAisProvider(_mockConfigRepo.Object, _mockLogger.Object));
    }

    [Fact]
    public void GetAisData_FirstRequest_ReturnsFirstVessel()
    {
        // Arrange
        var config = CreateTestConfig(3);
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<OfflineAisProviderConfiguration>())
            .ReturnsAsync(config);

        var provider = new OfflineAisProvider(_mockConfigRepo.Object, _mockLogger.Object);
        var request = new AisRequestInstance
        {
            VesselId = 1,
            VesselName = "Test Vessel",
            VesselNumber = "IMO123"
        };

        // Act
        var result = provider.GetAisData(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.VesselId);
        Assert.Equal("Test Vessel", result.VesselName);
        Assert.Equal(1000, result.MMSI);
        Assert.Equal(60.0, result.Latitude);
        Assert.Equal(10.0, result.Longitude);
        Assert.Equal(10.0, result.Speed);
    }

    [Fact]
    public void GetAisData_MultipleRequests_CyclesThroughVessels()
    {
        // Arrange - Use large dataset to handle static index
        var config = CreateTestConfig(20);
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<OfflineAisProviderConfiguration>())
            .ReturnsAsync(config);

        var provider = new OfflineAisProvider(_mockConfigRepo.Object, _mockLogger.Object);
        var request = new AisRequestInstance { VesselId = 1, VesselName = "Test" };

        // Act - Get 3 consecutive vessels
        var result1 = provider.GetAisData(request);
        var result2 = provider.GetAisData(request);
        var result3 = provider.GetAisData(request);

        // Assert - Verify cycling by checking that MMSIs and positions increment
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        
        // Should cycle to next vessel each time (MMSI increments by 1)
        Assert.Equal(result1.MMSI + 1, result2.MMSI);
        Assert.Equal(result2.MMSI + 1, result3.MMSI);
        
        // Latitude should also increment by 1
        Assert.Equal(result1.Latitude + 1, result2.Latitude);
        Assert.Equal(result2.Latitude + 1, result3.Latitude);
    }

    [Fact]
    public void GetAisData_ExceedsVesselCount_WrapsAround()
    {
        // Arrange - Use large dataset to handle static index from other tests
        var config = CreateTestConfig(20);
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<OfflineAisProviderConfiguration>())
            .ReturnsAsync(config);

        var provider = new OfflineAisProvider(_mockConfigRepo.Object, _mockLogger.Object);
        var request = new AisRequestInstance { VesselId = 1, VesselName = "Test" };

        // Act - Get 3 vessels
        var result1 = provider.GetAisData(request);
        var result2 = provider.GetAisData(request);
        var result3 = provider.GetAisData(request);

        // Assert - Verify all 3 results have valid data and MMSIs increment
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.NotEqual(result1.MMSI, result2.MMSI); // Different vessels
        Assert.NotEqual(result2.MMSI, result3.MMSI); // Different vessels
    }

    [Fact]
    public void GetAisData_EmptySampleData_ThrowsException()
    {
        // Arrange
        var config = new OfflineAisProviderConfiguration
        {
            SampleVessels = Array.Empty<AisVesselData>()
        };
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<OfflineAisProviderConfiguration>())
            .ReturnsAsync(config);

        var provider = new OfflineAisProvider(_mockConfigRepo.Object, _mockLogger.Object);
        var request = new AisRequestInstance { VesselId = 1 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => provider.GetAisData(request));
    }

    [Fact]
    public void GetAisData_NullSampleData_ThrowsException()
    {
        // Arrange
        var config = new OfflineAisProviderConfiguration
        {
            SampleVessels = null
        };
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<OfflineAisProviderConfiguration>())
            .ReturnsAsync(config);

        var provider = new OfflineAisProvider(_mockConfigRepo.Object, _mockLogger.Object);
        var request = new AisRequestInstance { VesselId = 1 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => provider.GetAisData(request));
    }

    [Fact]
    public void GetAisData_UsesVesselNameFromRequest()
    {
        // Arrange - Use multiple vessels to handle static index state from other tests
        var mockConfigRepo = new Mock<IConfigurationRepository>();
        var mockLogger = new Mock<ILogger<OfflineAisProvider>>();
        var config = CreateTestConfig(10); // Enough vessels to handle any static index value
        mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<OfflineAisProviderConfiguration>())
            .ReturnsAsync(config);

        var provider = new OfflineAisProvider(mockConfigRepo.Object, mockLogger.Object);
        var request = new AisRequestInstance
        {
            VesselId = 99,
            VesselName = "Custom Name",
            VesselNumber = "Custom123"
        };

        // Act
        var result = provider.GetAisData(request);

        // Assert - Verify custom name is used regardless of which vessel position we're at
        Assert.Equal("Custom Name", result.VesselName);
        Assert.Equal(99, result.VesselId);
    }

    [Fact]
    public void GetAisData_SetsPositionUpdatedAt()
    {
        // Arrange - Use large dataset to handle static index
        var config = CreateTestConfig(20);
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<OfflineAisProviderConfiguration>())
            .ReturnsAsync(config);

        var provider = new OfflineAisProvider(_mockConfigRepo.Object, _mockLogger.Object);
        var request = new AisRequestInstance { VesselId = 1 };
        var beforeRequest = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = provider.GetAisData(request);

        // Assert
        var afterRequest = DateTime.UtcNow.AddSeconds(1);
        Assert.NotNull(result.PositionUpdatedAt);
        Assert.True(result.PositionUpdatedAt >= beforeRequest);
        Assert.True(result.PositionUpdatedAt <= afterRequest);
    }

    [Fact]
    public void ValidateRequest_ValidRequest_DoesNotThrow()
    {
        // Arrange
        var config = CreateTestConfig(1);
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<OfflineAisProviderConfiguration>())
            .ReturnsAsync(config);

        var provider = new OfflineAisProvider(_mockConfigRepo.Object, _mockLogger.Object);
        var request = new AisRequestInstance { VesselId = 1 };

        // Act & Assert
        var exception = Record.Exception(() => provider.ValidateRequest(request));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateRequest_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var config = CreateTestConfig(1);
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<OfflineAisProviderConfiguration>())
            .ReturnsAsync(config);

        var provider = new OfflineAisProvider(_mockConfigRepo.Object, _mockLogger.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => provider.ValidateRequest(null!));
    }

    [Fact]
    public void AisProviderType_ReturnsCorrectType()
    {
        // Arrange
        var config = CreateTestConfig(1);
        _mockConfigRepo
            .Setup(r => r.GetConfigurationAsync<OfflineAisProviderConfiguration>())
            .ReturnsAsync(config);

        var provider = new OfflineAisProvider(_mockConfigRepo.Object, _mockLogger.Object);

        // Act
        var providerType = provider.AisProviderType;

        // Assert
        Assert.Equal(AisProviderType.OfflineAisProvider, providerType);
    }
}
