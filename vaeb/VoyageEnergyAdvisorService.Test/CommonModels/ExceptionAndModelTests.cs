using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Services.WeatherService.Exceptions;
using Xunit;

namespace VoyageEnergyAdvisorService.Test.CommonModels;

/// <summary>
/// Unit tests for InvalidTimestampException.
/// Tests exception message formatting and property initialization.
/// </summary>
public class InvalidTimestampExceptionTests
{
    [Fact]
    public void Constructor_WithValidParameters_InitializesProperties()
    {
        // Arrange
        var minDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var invalidStart = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var invalidEnd = new DateTime(2025, 1, 11, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var exception = new InvalidTimestampException(minDate, maxDate, invalidStart, invalidEnd);

        // Assert
        Assert.Equal(minDate, exception.MinDateTime);
        Assert.Equal(maxDate, exception.MaxDateTime);
        Assert.Equal(invalidStart, exception.InvalidStartTime);
        Assert.Equal(invalidEnd, exception.InvalidEndTime);
    }

    [Fact]
    public void Constructor_WithoutInvalidTimes_InitializesWithNulls()
    {
        // Arrange
        var minDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var exception = new InvalidTimestampException(minDate, maxDate);

        // Assert
        Assert.Equal(minDate, exception.MinDateTime);
        Assert.Equal(maxDate, exception.MaxDateTime);
        Assert.Null(exception.InvalidStartTime);
        Assert.Null(exception.InvalidEndTime);
    }

    [Fact]
    public void UserMessage_WithInvalidTimes_IncludesSpecificDates()
    {
        // Arrange
        var minDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var invalidStart = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var invalidEnd = new DateTime(2025, 1, 11, 0, 0, 0, DateTimeKind.Utc);

        var exception = new InvalidTimestampException(minDate, maxDate, invalidStart, invalidEnd);

        // Act
        var message = exception.UserMessage;

        // Assert
        Assert.Contains("Weather forecast data is only available between", message);
        Assert.Contains("2025-01-01", message); // Min date
        Assert.Contains("2025-01-10", message); // Max date
        Assert.Contains("You requested a route from", message);
        Assert.Contains("2024-12-31", message); // Invalid start
        Assert.Contains("2025-01-11", message); // Invalid end
    }

    [Fact]
    public void UserMessage_WithoutInvalidTimes_IncludesGenericMessage()
    {
        // Arrange
        var minDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);

        var exception = new InvalidTimestampException(minDate, maxDate);

        // Act
        var message = exception.UserMessage;

        // Assert
        Assert.Contains("Weather forecast data is only available between", message);
        Assert.Contains("Please select a valid time range", message);
        Assert.DoesNotContain("You requested a route from", message);
    }

    [Fact]
    public void Message_ReturnsBaseMessage()
    {
        // Arrange
        var minDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);

        var exception = new InvalidTimestampException(minDate, maxDate);

        // Act
        var message = exception.Message;

        // Assert
        Assert.Equal("One or more weather request times fall outside the valid forecast range.", message);
    }

    [Fact]
    public void Exception_CanBeThrown()
    {
        // Arrange
        var minDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);

        // Act & Assert
        void ThrowException() => throw new InvalidTimestampException(minDate, maxDate);
        
        var exception = Assert.Throws<InvalidTimestampException>(ThrowException);

        Assert.NotNull(exception);
        Assert.Equal(minDate, exception.MinDateTime);
    }
}

/// <summary>
/// Unit tests for Route (CommonModels).
/// Tests property initialization and waypoint management.
/// </summary>
public class RouteTests
{
    [Fact]
    public void Constructor_InitializesEmptyWaypoints()
    {
        // Act
        var route = new Route();

        // Assert
        Assert.NotNull(route.Waypoints);
        Assert.Empty(route.Waypoints);
    }

    [Fact]
    public void RouteName_CanBeSetAndRetrieved()
    {
        // Arrange
        var route = new Route();
        var routeName = "Test Route 123";

        // Act
        route.RouteName = routeName;

        // Assert
        Assert.Equal(routeName, route.RouteName);
    }

    [Fact]
    public void Waypoints_CanAddGeoCoordinates()
    {
        // Arrange
        var route = new Route();
        var waypoint1 = new GeoCoordinate(60.0, 10.0);
        var waypoint2 = new GeoCoordinate(61.0, 11.0);

        // Act
        route.Waypoints.Add(waypoint1);
        route.Waypoints.Add(waypoint2);

        // Assert
        Assert.Equal(2, route.Waypoints.Count);
        Assert.Equal(60.0, route.Waypoints[0].Latitude);
        Assert.Equal(10.0, route.Waypoints[0].Longitude);
        Assert.Equal(61.0, route.Waypoints[1].Latitude);
        Assert.Equal(11.0, route.Waypoints[1].Longitude);
    }

    [Fact]
    public void Waypoints_CanBeInitializedInConstructor()
    {
        // Arrange & Act
        var route = new Route
        {
            RouteName = "Atlantic Crossing",
            Waypoints = new List<GeoCoordinate>
            {
                new GeoCoordinate(50.0, -5.0),
                new GeoCoordinate(40.0, -70.0)
            }
        };

        // Assert
        Assert.Equal("Atlantic Crossing", route.RouteName);
        Assert.Equal(2, route.Waypoints.Count);
    }

    [Fact]
    public void Waypoints_CanBeCleared()
    {
        // Arrange
        var route = new Route
        {
            Waypoints = new List<GeoCoordinate>
            {
                new GeoCoordinate(60.0, 10.0),
                new GeoCoordinate(61.0, 11.0)
            }
        };

        // Act
        route.Waypoints.Clear();

        // Assert
        Assert.Empty(route.Waypoints);
    }

    [Fact]
    public void Route_SupportsPropertyInitialization()
    {
        // Act
        var route = new Route
        {
            RouteName = "Short Route",
            Waypoints = new List<GeoCoordinate> { new GeoCoordinate(55.0, 12.0) }
        };

        // Assert
        Assert.Equal("Short Route", route.RouteName);
        Assert.Single(route.Waypoints);
        Assert.Equal(55.0, route.Waypoints[0].Latitude);
    }
}

/// <summary>
/// Tests for WeatherData model class.
/// </summary>
public class WeatherDataTests
{
    [Fact]
    public void WeatherData_DefaultConstructor_InitializesAllPropertiesAsNull()
    {
        // Arrange & Act
        var weatherData = new WeatherData();

        // Assert
        Assert.Null(weatherData.WindSpeed);
        Assert.Null(weatherData.WindFromDirection);
        Assert.Null(weatherData.WaveHeight);
        Assert.Null(weatherData.WavePeakPeriod);
        Assert.Null(weatherData.WaveFromDirection);
        Assert.Null(weatherData.CurrentSpeed);
        Assert.Null(weatherData.CurrentFromDirection);
    }

    [Fact]
    public void WeatherData_ParameterizedConstructor_InitializesAllProperties()
    {
        // Arrange
        double windSpeed = 15.5;
        double windDirection = 270.0;
        double waveHeight = 2.5;
        double wavePeakPeriod = 8.0;
        double waveDirection = 280.0;
        double currentSpeed = 1.2;
        double currentDirection = 90.0;

        // Act
        var weatherData = new WeatherData(windSpeed, windDirection, waveHeight, wavePeakPeriod, waveDirection, currentSpeed, currentDirection);

        // Assert
        Assert.Equal(windSpeed, weatherData.WindSpeed);
        Assert.Equal(windDirection, weatherData.WindFromDirection);
        Assert.Equal(waveHeight, weatherData.WaveHeight);
        Assert.Equal(wavePeakPeriod, weatherData.WavePeakPeriod);
        Assert.Equal(waveDirection, weatherData.WaveFromDirection);
        Assert.Equal(currentSpeed, weatherData.CurrentSpeed);
        Assert.Equal(currentDirection, weatherData.CurrentFromDirection);
    }

    [Fact]
    public void WeatherData_ParameterizedConstructor_AcceptsNullValues()
    {
        // Act
        var weatherData = new WeatherData(null, null, null, null, null, null, null);

        // Assert
        Assert.Null(weatherData.WindSpeed);
        Assert.Null(weatherData.WindFromDirection);
        Assert.Null(weatherData.WaveHeight);
        Assert.Null(weatherData.WavePeakPeriod);
        Assert.Null(weatherData.WaveFromDirection);
        Assert.Null(weatherData.CurrentSpeed);
        Assert.Null(weatherData.CurrentFromDirection);
    }

    [Fact]
    public void WeatherData_Properties_CanBeSetIndividually()
    {
        // Arrange
        var weatherData = new WeatherData();

        // Act
        weatherData.WindSpeed = 12.3;
        weatherData.WindFromDirection = 180.0;
        weatherData.WaveHeight = 1.8;
        weatherData.WavePeakPeriod = 6.5;
        weatherData.WaveFromDirection = 190.0;
        weatherData.CurrentSpeed = 0.8;
        weatherData.CurrentFromDirection = 45.0;

        // Assert
        Assert.Equal(12.3, weatherData.WindSpeed);
        Assert.Equal(180.0, weatherData.WindFromDirection);
        Assert.Equal(1.8, weatherData.WaveHeight);
        Assert.Equal(6.5, weatherData.WavePeakPeriod);
        Assert.Equal(190.0, weatherData.WaveFromDirection);
        Assert.Equal(0.8, weatherData.CurrentSpeed);
        Assert.Equal(45.0, weatherData.CurrentFromDirection);
    }

    [Fact]
    public void WeatherData_Properties_CanBeSetToNull()
    {
        // Arrange
        var weatherData = new WeatherData(10.0, 100.0, 2.0, 7.0, 110.0, 1.0, 50.0);

        // Act
        weatherData.WindSpeed = null;
        weatherData.WindFromDirection = null;
        weatherData.WaveHeight = null;
        weatherData.WavePeakPeriod = null;
        weatherData.WaveFromDirection = null;
        weatherData.CurrentSpeed = null;
        weatherData.CurrentFromDirection = null;

        // Assert
        Assert.Null(weatherData.WindSpeed);
        Assert.Null(weatherData.WindFromDirection);
        Assert.Null(weatherData.WaveHeight);
        Assert.Null(weatherData.WavePeakPeriod);
        Assert.Null(weatherData.WaveFromDirection);
        Assert.Null(weatherData.CurrentSpeed);
        Assert.Null(weatherData.CurrentFromDirection);
    }
}

/// <summary>
/// Tests for WeatherForecastProviderException.
/// </summary>
public class WeatherForecastProviderExceptionTests
{
    [Fact]
    public void WeatherForecastProviderException_DefaultConstructor_CreatesException()
    {
        // Act
        var exception = new WeatherForecastProviderException();

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<WeatherForecastProviderException>(exception);
    }

    [Fact]
    public void WeatherForecastProviderException_MessageConstructor_SetsMessage()
    {
        // Arrange
        var expectedMessage = "Weather forecast provider unavailable";

        // Act
        var exception = new WeatherForecastProviderException(expectedMessage);

        // Assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void WeatherForecastProviderException_InnerExceptionConstructor_SetsMessageAndInnerException()
    {
        // Arrange
        var expectedMessage = "Failed to fetch weather data";
        var innerException = new InvalidOperationException("Network error");

        // Act
        var exception = new WeatherForecastProviderException(expectedMessage, innerException);

        // Assert
        Assert.Equal(expectedMessage, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void WeatherForecastProviderException_CanBeThrown()
    {
        // Act & Assert
        void ThrowException() => throw new WeatherForecastProviderException("Test exception");
        var exception = Assert.Throws<WeatherForecastProviderException>(ThrowException);
        Assert.Equal("Test exception", exception.Message);
    }
}

