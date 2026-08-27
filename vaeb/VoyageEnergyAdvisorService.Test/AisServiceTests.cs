using Microsoft.Extensions.Logging;
using Moq;
using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Repositories;
using VoyageEnergyAdvisor.Core.Services.AisProviders;
using VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels;
using VoyageEnergyAdvisor.Core.Services.AisService;
using Xunit;

namespace VoyageEnergyAdvisorService.Test;

public class AisServiceTests
{
    private readonly Mock<IConfigurationRepository> _configRepoMock;
    private readonly Mock<IUserVesselRepository> _vesselRepoMock;
    private readonly Mock<ILogger<AisService>> _loggerMock;
    private readonly Mock<IAisProvider> _offlineProviderMock;
    private readonly Mock<IAisProvider> _aisStreamProviderMock;

    public AisServiceTests()
    {
        _configRepoMock = new Mock<IConfigurationRepository>();
        _vesselRepoMock = new Mock<IUserVesselRepository>();
        _loggerMock = new Mock<ILogger<AisService>>();
        _offlineProviderMock = new Mock<IAisProvider>();
        _aisStreamProviderMock = new Mock<IAisProvider>();

        // Setup provider types
        _offlineProviderMock.Setup(p => p.AisProviderType).Returns(AisProviderType.OfflineAisProvider);
        _aisStreamProviderMock.Setup(p => p.AisProviderType).Returns(AisProviderType.AisStreamProvider);
    }

    [Fact]
    public void Constructor_ValidConfiguration_SelectsCorrectProvider()
    {
        // Arrange
        var config = new AisServiceConfiguration
        {
            SelectedAisProvider = AisProviderType.OfflineAisProvider
        };
        _configRepoMock.Setup(r => r.GetConfigurationAsync<AisServiceConfiguration>())
            .ReturnsAsync(config);

        var providers = new[] { _offlineProviderMock.Object, _aisStreamProviderMock.Object };

        // Act
        var service = new AisService(providers, _configRepoMock.Object, _vesselRepoMock.Object, _loggerMock.Object);

        // Assert
        Assert.NotNull(service);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Selected AIS Provider: OfflineAisProvider")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_MissingConfiguration_ThrowsException()
    {
        // Arrange
        _configRepoMock.Setup(r => r.GetConfigurationAsync<AisServiceConfiguration>())
            .ReturnsAsync((AisServiceConfiguration?)null);

        var providers = new[] { _offlineProviderMock.Object };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() =>
            new AisService(providers, _configRepoMock.Object, _vesselRepoMock.Object, _loggerMock.Object));
        Assert.Equal("AIS Service Configuration not found.", exception.Message);
    }

    [Fact]
    public void Constructor_InvalidProviderName_ThrowsArgumentException()
    {
        // Arrange
        var config = new AisServiceConfiguration
        {
            SelectedAisProvider = AisProviderType.AisStreamProvider // Request AisStreamProvider
        };
        _configRepoMock.Setup(r => r.GetConfigurationAsync<AisServiceConfiguration>())
            .ReturnsAsync(config);

        var providers = new[] { _offlineProviderMock.Object }; // Only provide OfflineAisProvider

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new AisService(providers, _configRepoMock.Object, _vesselRepoMock.Object, _loggerMock.Object));
        Assert.Contains("Provider 'AisStreamProvider' is not available.", exception.Message);
    }

    [Fact]
    public async Task GetCurrentVesselDataAsync_ValidVessel_ReturnsAisData()
    {
        // Arrange
        var config = new AisServiceConfiguration
        {
            SelectedAisProvider = AisProviderType.OfflineAisProvider
        };
        _configRepoMock.Setup(r => r.GetConfigurationAsync<AisServiceConfiguration>())
            .ReturnsAsync(config);

        var vessel = new VesselDto
        {
            Id = 123,
            Name = "Test Vessel",
            VesselNumber = "IMO1234567"
        };
        _vesselRepoMock.Setup(r => r.GetCurrentVesselAsync())
            .ReturnsAsync(vessel);

        var expectedResponse = new AisResponseInstance
        {
            VesselId = 123,
            VesselName = "Test Vessel",
            MMSI = 123456789,
            Latitude = 60.5,
            Longitude = 5.3,
            Speed = 12.5,
            Course = 180.0,
            Status = "Under way using engine"
        };

        _offlineProviderMock.Setup(p => p.GetAisData(It.IsAny<AisRequestInstance>()))
            .Returns(expectedResponse);

        var providers = new[] { _offlineProviderMock.Object };
        var service = new AisService(providers, _configRepoMock.Object, _vesselRepoMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetCurrentVesselDataAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.VesselId, result.VesselId);
        Assert.Equal(expectedResponse.VesselName, result.VesselName);
        Assert.Equal(expectedResponse.Latitude, result.Latitude);
        Assert.Equal(expectedResponse.Longitude, result.Longitude);
        Assert.Equal(expectedResponse.Speed, result.Speed);

        // Verify AisRequestInstance was created with correct data
        _offlineProviderMock.Verify(p => p.GetAisData(It.Is<AisRequestInstance>(req =>
            req.VesselId == vessel.Id &&
            req.VesselName == vessel.Name &&
            req.VesselNumber == vessel.VesselNumber
        )), Times.Once);

        // Verify ValidateRequest was called
        _offlineProviderMock.Verify(p => p.ValidateRequest(It.IsAny<AisRequestInstance>()), Times.Once);

        // Verify info log was called
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieved vessel data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCurrentVesselDataAsync_VesselNotFound_ReturnsNull()
    {
        // Arrange
        var config = new AisServiceConfiguration
        {
            SelectedAisProvider = AisProviderType.OfflineAisProvider
        };
        _configRepoMock.Setup(r => r.GetConfigurationAsync<AisServiceConfiguration>())
            .ReturnsAsync(config);

        _vesselRepoMock.Setup(r => r.GetCurrentVesselAsync())
            .ReturnsAsync((VesselDto?)null);

        var providers = new[] { _offlineProviderMock.Object };
        var service = new AisService(providers, _configRepoMock.Object, _vesselRepoMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetCurrentVesselDataAsync();

        // Assert
        Assert.Null(result);

        // Verify provider was not called
        _offlineProviderMock.Verify(p => p.GetAisData(It.IsAny<AisRequestInstance>()), Times.Never);
        _offlineProviderMock.Verify(p => p.ValidateRequest(It.IsAny<AisRequestInstance>()), Times.Never);

        // Verify warning log was called
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Vessel is not found in database")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCurrentVesselDataAsync_ProviderReturnsNull_ReturnsNull()
    {
        // Arrange
        var config = new AisServiceConfiguration
        {
            SelectedAisProvider = AisProviderType.OfflineAisProvider
        };
        _configRepoMock.Setup(r => r.GetConfigurationAsync<AisServiceConfiguration>())
            .ReturnsAsync(config);

        var vessel = new VesselDto
        {
            Id = 123,
            Name = "Test Vessel",
            VesselNumber = "IMO1234567"
        };
        _vesselRepoMock.Setup(r => r.GetCurrentVesselAsync())
            .ReturnsAsync(vessel);

        _offlineProviderMock.Setup(p => p.GetAisData(It.IsAny<AisRequestInstance>()))
            .Returns((AisResponseInstance?)null);

        var providers = new[] { _offlineProviderMock.Object };
        var service = new AisService(providers, _configRepoMock.Object, _vesselRepoMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetCurrentVesselDataAsync();

        // Assert
        Assert.Null(result);

        // Verify warning log for no data received
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No vessel data received from provider")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCurrentVesselDataAsync_ProviderThrowsException_ReturnsNull()
    {
        // Arrange
        var config = new AisServiceConfiguration
        {
            SelectedAisProvider = AisProviderType.OfflineAisProvider
        };
        _configRepoMock.Setup(r => r.GetConfigurationAsync<AisServiceConfiguration>())
            .ReturnsAsync(config);

        var vessel = new VesselDto
        {
            Id = 123,
            Name = "Test Vessel",
            VesselNumber = "IMO1234567"
        };
        _vesselRepoMock.Setup(r => r.GetCurrentVesselAsync())
            .ReturnsAsync(vessel);

        _offlineProviderMock.Setup(p => p.GetAisData(It.IsAny<AisRequestInstance>()))
            .Throws(new Exception("Provider error"));

        var providers = new[] { _offlineProviderMock.Object };
        var service = new AisService(providers, _configRepoMock.Object, _vesselRepoMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetCurrentVesselDataAsync();

        // Assert
        Assert.Null(result);

        // Verify error log was called
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting vessel data from provider")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_SuccessfulInitialization_LogsProviderSelection()
    {
        // Arrange
        var config = new AisServiceConfiguration
        {
            SelectedAisProvider = AisProviderType.AisStreamProvider
        };
        _configRepoMock.Setup(r => r.GetConfigurationAsync<AisServiceConfiguration>())
            .ReturnsAsync(config);

        var providers = new[] { _offlineProviderMock.Object, _aisStreamProviderMock.Object };

        // Act
        var service = new AisService(providers, _configRepoMock.Object, _vesselRepoMock.Object, _loggerMock.Object);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Selected AIS Provider: AisStreamProvider")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
