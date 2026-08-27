using Microsoft.Extensions.Logging;
using Moq;
using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Configuration.RouteConfiguration.Models;
using VoyageEnergyAdvisor.Core.Repositories;
using VoyageEnergyAdvisor.Core.Services.RouteService;
using VoyageEnergyAdvisor.Core.Services.RouteService.RouteProviders;
using Xunit;

namespace VoyageEnergyAdvisorService.Test;

/// <summary>
/// Unit tests for RouteService.
/// Tests provider selection, route retrieval, and error handling.
/// </summary>
public class RouteServiceTests
{
    private readonly Mock<ILogger<RouteService>> _loggerMock;
    private readonly Mock<IConfigurationRepository> _configRepositoryMock;
    private readonly Mock<IRouteProvider> _mockProvider1;
    private readonly Mock<IRouteProvider> _mockProvider2;

    public RouteServiceTests()
    {
        _loggerMock = new Mock<ILogger<RouteService>>();
        _configRepositoryMock = new Mock<IConfigurationRepository>();
        _mockProvider1 = new Mock<IRouteProvider>();
        _mockProvider2 = new Mock<IRouteProvider>();
    }

    [Fact]
    public void Constructor_ValidConfiguration_SelectsCorrectProvider()
    {
        // Arrange
        var config = new RouteServiceConfiguration
        {
            SelectedRouteProvider = RouteProviderType.LocalFilesRouteProvider
        };

        _configRepositoryMock
            .Setup(x => x.GetConfigurationAsync<RouteServiceConfiguration>())
            .ReturnsAsync(config);

        _mockProvider1.Setup(x => x.RouteProviderType).Returns(RouteProviderType.LocalFilesRouteProvider);
        _mockProvider2.Setup(x => x.RouteProviderType).Returns(RouteProviderType.NavBoxRouteProvider);

        var providers = new List<IRouteProvider> { _mockProvider1.Object, _mockProvider2.Object };

        // Act
        var service = new RouteService(providers, _configRepositoryMock.Object, _loggerMock.Object);

        // Assert
        _configRepositoryMock.Verify(x => x.GetConfigurationAsync<RouteServiceConfiguration>(), Times.Once);
    }

    [Fact]
    public void Constructor_NullConfiguration_ThrowsException()
    {
        // Arrange
        _configRepositoryMock
            .Setup(x => x.GetConfigurationAsync<RouteServiceConfiguration>())
            .ReturnsAsync((RouteServiceConfiguration?)null);

        var providers = new List<IRouteProvider> { _mockProvider1.Object };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() =>
            new RouteService(providers, _configRepositoryMock.Object, _loggerMock.Object));

        Assert.Equal("Configuration not found.", exception.Message);
    }

    [Fact]
    public void Constructor_ProviderNotAvailable_ThrowsArgumentException()
    {
        // Arrange
        var config = new RouteServiceConfiguration
        {
            SelectedRouteProvider = RouteProviderType.NavBoxRouteProvider
        };

        _configRepositoryMock
            .Setup(x => x.GetConfigurationAsync<RouteServiceConfiguration>())
            .ReturnsAsync(config);

        _mockProvider1.Setup(x => x.RouteProviderType).Returns(RouteProviderType.LocalFilesRouteProvider);

        var providers = new List<IRouteProvider> { _mockProvider1.Object };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new RouteService(providers, _configRepositoryMock.Object, _loggerMock.Object));

        Assert.Contains("NavBoxRouteProvider", exception.Message);
        Assert.Contains("is not available", exception.Message);
    }

    [Fact]
    public void GetRoutesList_CallsSelectedProvider()
    {
        // Arrange
        var config = new RouteServiceConfiguration
        {
            SelectedRouteProvider = RouteProviderType.LocalFilesRouteProvider
        };

        _configRepositoryMock
            .Setup(x => x.GetConfigurationAsync<RouteServiceConfiguration>())
            .ReturnsAsync(config);

        _mockProvider1.Setup(x => x.RouteProviderType).Returns(RouteProviderType.LocalFilesRouteProvider);
        _mockProvider1.Setup(x => x.GetRoutesList()).Returns(new List<string> { "route1", "route2", "route3" });

        var providers = new List<IRouteProvider> { _mockProvider1.Object };
        var service = new RouteService(providers, _configRepositoryMock.Object, _loggerMock.Object);

        // Act
        var result = service.GetRoutesList();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains("route1", result);
        Assert.Contains("route2", result);
        Assert.Contains("route3", result);
        _mockProvider1.Verify(x => x.GetRoutesList(), Times.Once);
    }

    [Fact]
    public void GetRoutesList_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var config = new RouteServiceConfiguration
        {
            SelectedRouteProvider = RouteProviderType.LocalFilesRouteProvider
        };

        _configRepositoryMock
            .Setup(x => x.GetConfigurationAsync<RouteServiceConfiguration>())
            .ReturnsAsync(config);

        _mockProvider1.Setup(x => x.RouteProviderType).Returns(RouteProviderType.LocalFilesRouteProvider);
        _mockProvider1.Setup(x => x.GetRoutesList()).Returns(new List<string>());

        var providers = new List<IRouteProvider> { _mockProvider1.Object };
        var service = new RouteService(providers, _configRepositoryMock.Object, _loggerMock.Object);

        // Act
        var result = service.GetRoutesList();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetRoute_ValidId_ReturnsRoute()
    {
        // Arrange
        var config = new RouteServiceConfiguration
        {
            SelectedRouteProvider = RouteProviderType.LocalFilesRouteProvider
        };

        _configRepositoryMock
            .Setup(x => x.GetConfigurationAsync<RouteServiceConfiguration>())
            .ReturnsAsync(config);

        var expectedRoute = new Route
        {
            RouteName = "Test Route",
            Waypoints = new List<GeoCoordinate>
            {
                new GeoCoordinate(50.0, 10.0),
                new GeoCoordinate(51.0, 11.0)
            }
        };

        _mockProvider1.Setup(x => x.RouteProviderType).Returns(RouteProviderType.LocalFilesRouteProvider);
        _mockProvider1.Setup(x => x.GetRoute("test-route-123")).Returns(expectedRoute);

        var providers = new List<IRouteProvider> { _mockProvider1.Object };
        var service = new RouteService(providers, _configRepositoryMock.Object, _loggerMock.Object);

        // Act
        var result = service.GetRoute("test-route-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Route", result.RouteName);
        Assert.Equal(2, result.Waypoints.Count);
        _mockProvider1.Verify(x => x.GetRoute("test-route-123"), Times.Once);
    }

    [Fact]
    public void GetRoute_RouteNotFound_ReturnsNull()
    {
        // Arrange
        var config = new RouteServiceConfiguration
        {
            SelectedRouteProvider = RouteProviderType.LocalFilesRouteProvider
        };

        _configRepositoryMock
            .Setup(x => x.GetConfigurationAsync<RouteServiceConfiguration>())
            .ReturnsAsync(config);

        _mockProvider1.Setup(x => x.RouteProviderType).Returns(RouteProviderType.LocalFilesRouteProvider);
        _mockProvider1.Setup(x => x.GetRoute("nonexistent-route")).Returns((Route?)null);

        var providers = new List<IRouteProvider> { _mockProvider1.Object };
        var service = new RouteService(providers, _configRepositoryMock.Object, _loggerMock.Object);

        // Act
        var result = service.GetRoute("nonexistent-route");

        // Assert
        Assert.Null(result);
        _mockProvider1.Verify(x => x.GetRoute("nonexistent-route"), Times.Once);
    }

    [Fact]
    public void GetRoute_NullId_PassesNullToProvider()
    {
        // Arrange
        var config = new RouteServiceConfiguration
        {
            SelectedRouteProvider = RouteProviderType.LocalFilesRouteProvider
        };

        _configRepositoryMock
            .Setup(x => x.GetConfigurationAsync<RouteServiceConfiguration>())
            .ReturnsAsync(config);

        _mockProvider1.Setup(x => x.RouteProviderType).Returns(RouteProviderType.LocalFilesRouteProvider);
        _mockProvider1.Setup(x => x.GetRoute(It.IsAny<string>())).Returns((Route?)null);

        var providers = new List<IRouteProvider> { _mockProvider1.Object };
        var service = new RouteService(providers, _configRepositoryMock.Object, _loggerMock.Object);

        // Act
        var result = service.GetRoute(null!);

        // Assert
        Assert.Null(result);
        _mockProvider1.Verify(x => x.GetRoute(It.IsAny<string>()), Times.Once);
    }
}
