using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models;
using VoyageEnergyAdvisor.Core.Services.WeatherProviders;
using VoyageEnergyAdvisor.Core.Services.WeatherProviders.WeatherProviderModels;
using VoyageEnergyAdvisor.Core.Services.WeatherService.Exceptions;
using Xunit;

namespace VoyageEnergyAdvisorService.Test.Weather;

/// <summary>
/// Unit tests for MetWeatherForecastProvider (MET Norway API).
/// Tests HTTP communication, JSON parsing, error handling, and timestamp logic.
/// </summary>
public class MetWeatherForecastProviderTests
{
    private readonly Mock<ICancellationTokenService> _cancellationServiceMock;
    private readonly Mock<IOptionsMonitor<MetWeatherForecastProviderConfiguration>> _optionsMonitorMock;
    private readonly MetWeatherForecastProviderConfiguration _config;

    public MetWeatherForecastProviderTests()
    {
        _cancellationServiceMock = new Mock<ICancellationTokenService>();
        _cancellationServiceMock.Setup(x => x.Token).Returns(CancellationToken.None);

        _config = new MetWeatherForecastProviderConfiguration();

        _optionsMonitorMock = new Mock<IOptionsMonitor<MetWeatherForecastProviderConfiguration>>();
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(_config);
    }

    private Mock<HttpMessageHandler> CreateMockHttpHandler()
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        // Mock location forecast response
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("locationforecast")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(File.ReadAllText("../../../Weather/MetWeatherForecastProvider/MetWeatherForecastResponse.json"))
            });

        // Mock ocean forecast response
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("oceanforecast")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(File.ReadAllText("../../../Weather/MetWeatherForecastProvider/MetOceanForecastResponse.json"))
            });

        return handlerMock;
    }

    [Fact]
    public void WeatherProviderType_ReturnsMetWeatherProvider()
    {
        // Arrange
        var httpClient = new HttpClient(CreateMockHttpHandler().Object);
        var provider = new MetWeatherForecastProvider(httpClient, _optionsMonitorMock.Object, _cancellationServiceMock.Object);

        // Act
        var result = provider.WeatherProviderType;

        // Assert
        Assert.Equal(WeatherProviderType.MetWeatherProvider, result);
    }

    [Fact]
    public void MaxForecastRange_ReturnsNineDays()
    {
        // Arrange
        var httpClient = new HttpClient(CreateMockHttpHandler().Object);
        var provider = new MetWeatherForecastProvider(httpClient, _optionsMonitorMock.Object, _cancellationServiceMock.Object);

        // Act
        var result = provider.MaxForecastRange;

        // Assert
        Assert.Equal(TimeSpan.FromDays(9), result);
    }

    [Fact]
    public void GetValidForecastRange_ReturnsCorrectRange()
    {
        // Arrange
        var httpClient = new HttpClient(CreateMockHttpHandler().Object);
        var provider = new MetWeatherForecastProvider(httpClient, _optionsMonitorMock.Object, _cancellationServiceMock.Object);
        var now = DateTime.UtcNow;

        // Act
        var (minTimestamp, maxTimestamp) = provider.GetValidForecastRange();

        // Assert
        Assert.InRange(minTimestamp, now.AddDays(9).AddMinutes(-1), now.AddDays(9).AddMinutes(1));
        Assert.InRange(maxTimestamp, now.AddDays(9).AddMinutes(-1), now.AddDays(9).AddMinutes(1));
    }

    [Fact]
    public async Task GetMultiPointWeatherForecast_SinglePoint_ReturnsWeatherData()
    {
        // Arrange
        var httpClient = new HttpClient(CreateMockHttpHandler().Object);
        var provider = new MetWeatherForecastProvider(httpClient, _optionsMonitorMock.Object, _cancellationServiceMock.Object);

        var requests = new List<WeatherRequestInstance>
        {
            new WeatherRequestInstance
            {
                Location = new GeoCoordinate(60.0, 10.0),
                Time = DateTime.UtcNow.AddHours(1)
            }
        };

        // Act
        var result = await provider.GetMultiPointWeatherForecast(requests);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var weatherData = result.First();
        Assert.Equal(60.0, weatherData.Location.Latitude);
        Assert.Equal(10.0, weatherData.Location.Longitude);
        Assert.NotNull(weatherData.Weather);
    }

    [Fact]
    public async Task GetMultiPointWeatherForecast_MultiplePoints_ReturnsMultipleResults()
    {
        // Arrange
        var httpClient = new HttpClient(CreateMockHttpHandler().Object);
        var provider = new MetWeatherForecastProvider(httpClient, _optionsMonitorMock.Object, _cancellationServiceMock.Object);

        var requests = new List<WeatherRequestInstance>
        {
            new WeatherRequestInstance
            {
                Location = new GeoCoordinate(60.0, 10.0),
                Time = DateTime.UtcNow.AddHours(1)
            },
            new WeatherRequestInstance
            {
                Location = new GeoCoordinate(61.0, 11.0),
                Time = DateTime.UtcNow.AddHours(2)
            }
        };

        // Act
        var result = await provider.GetMultiPointWeatherForecast(requests);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetMultiPointWeatherForecast_EmptyRequests_ReturnsEmptyList()
    {
        // Arrange
        var httpClient = new HttpClient(CreateMockHttpHandler().Object);
        var provider = new MetWeatherForecastProvider(httpClient, _optionsMonitorMock.Object, _cancellationServiceMock.Object);

        var requests = new List<WeatherRequestInstance>();

        // Act
        var result = await provider.GetMultiPointWeatherForecast(requests);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMultiPointWeatherForecast_LocationForecastFails_ThrowsException()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        
        // Mock both location and ocean forecast to fail
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Server Error")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var provider = new MetWeatherForecastProvider(httpClient, _optionsMonitorMock.Object, _cancellationServiceMock.Object);

        var requests = new List<WeatherRequestInstance>
        {
            new WeatherRequestInstance
            {
                Location = new GeoCoordinate(60.0, 10.0),
                Time = DateTime.UtcNow.AddHours(1)
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<WeatherForecastProviderException>(
            async () => await provider.GetMultiPointWeatherForecast(requests));
    }

    [Fact]
    public async Task GetMultiPointWeatherForecast_OceanForecastFails_ThrowsException()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("locationforecast")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(File.ReadAllText("../../../Weather/MetWeatherForecastProvider/MetWeatherForecastResponse.json"))
            });

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("oceanforecast")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent("Service Unavailable")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var provider = new MetWeatherForecastProvider(httpClient, _optionsMonitorMock.Object, _cancellationServiceMock.Object);

        var requests = new List<WeatherRequestInstance>
        {
            new WeatherRequestInstance
            {
                Location = new GeoCoordinate(60.0, 10.0),
                Time = DateTime.UtcNow.AddHours(1)
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<WeatherForecastProviderException>(
            async () => await provider.GetMultiPointWeatherForecast(requests));
    }

    [Fact]
    public async Task GetMultiPointWeatherForecast_InvalidJson_ThrowsException()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Invalid JSON {{{")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var provider = new MetWeatherForecastProvider(httpClient, _optionsMonitorMock.Object, _cancellationServiceMock.Object);

        var requests = new List<WeatherRequestInstance>
        {
            new WeatherRequestInstance
            {
                Location = new GeoCoordinate(60.0, 10.0),
                Time = DateTime.UtcNow.AddHours(1)
            }
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(
            async () => await provider.GetMultiPointWeatherForecast(requests));
    }

    [Fact]
    public async Task GetMultiPointWeatherForecast_UserAgentHeader_IsSet()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, token) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(File.ReadAllText("../../../Weather/MetWeatherForecastProvider/MetWeatherForecastResponse.json"))
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var provider = new MetWeatherForecastProvider(httpClient, _optionsMonitorMock.Object, _cancellationServiceMock.Object);

        var requests = new List<WeatherRequestInstance>
        {
            new WeatherRequestInstance
            {
                Location = new GeoCoordinate(60.0, 10.0),
                Time = DateTime.UtcNow.AddHours(1)
            }
        };

        // Act
        try
        {
            await provider.GetMultiPointWeatherForecast(requests);
        }
        catch
        {
            // Ignore any exception, we just want to check the request
        }

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Contains(capturedRequest.Headers, h => h.Key == "User-Agent");
    }

    [Fact]
    public async Task GetMultiPointWeatherForecast_UrlFormat_IsCorrect()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("locationforecast")),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, token) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(File.ReadAllText("../../../Weather/MetWeatherForecastProvider/MetWeatherForecastResponse.json"))
            });

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("oceanforecast")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(File.ReadAllText("../../../Weather/MetWeatherForecastProvider/MetOceanForecastResponse.json"))
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var provider = new MetWeatherForecastProvider(httpClient, _optionsMonitorMock.Object, _cancellationServiceMock.Object);

        var requests = new List<WeatherRequestInstance>
        {
            new WeatherRequestInstance
            {
                Location = new GeoCoordinate(60.5, 10.25),
                Time = DateTime.UtcNow.AddHours(1)
            }
        };

        // Act
        await provider.GetMultiPointWeatherForecast(requests);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Contains("lat=60.5", capturedRequest.RequestUri!.ToString());
        Assert.Contains("lon=10.25", capturedRequest.RequestUri.ToString());
    }

    [Fact]
    public async Task GetMultiPointWeatherForecast_NullResponse_ThrowsException()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("null")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var provider = new MetWeatherForecastProvider(httpClient, _optionsMonitorMock.Object, _cancellationServiceMock.Object);

        var requests = new List<WeatherRequestInstance>
        {
            new WeatherRequestInstance
            {
                Location = new GeoCoordinate(60.0, 10.0),
                Time = DateTime.UtcNow.AddHours(1)
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await provider.GetMultiPointWeatherForecast(requests));
    }

    [Fact]
    public async Task GetMultiPointWeatherForecast_Timeout_ThrowsException()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        var httpClient = new HttpClient(handlerMock.Object);
        var provider = new MetWeatherForecastProvider(httpClient, _optionsMonitorMock.Object, _cancellationServiceMock.Object);

        var requests = new List<WeatherRequestInstance>
        {
            new WeatherRequestInstance
            {
                Location = new GeoCoordinate(60.0, 10.0),
                Time = DateTime.UtcNow.AddHours(1)
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            async () => await provider.GetMultiPointWeatherForecast(requests));
    }
}
