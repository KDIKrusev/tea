using VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels;
using Xunit;

namespace VoyageEnergyAdvisorService.Test.AisProviders;

/// <summary>
/// Unit tests for AisVesselData model.
/// Tests property initialization and data integrity.
/// </summary>
public class AisVesselDataTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        // Act
        var vesselData = new AisVesselData();

        // Assert
        Assert.Equal(0, vesselData.MMSI);
        Assert.Null(vesselData.Name);
        Assert.Null(vesselData.Latitude);
        Assert.Null(vesselData.Longitude);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange & Act
        var vesselData = new AisVesselData
        {
            MMSI = 123456789,
            IMO = 9876543,
            Name = "Test Vessel",
            CallSign = "ABC123",
            Flag = "Norway",
            Latitude = 60.5,
            Longitude = 10.7,
            Speed = 15.2,
            Course = 270.0,
            Heading = 268,
            Draught = 12.5,
            Length = 250.0,
            Width = 40.0,
            Destination = "Oslo",
            ShipType = "Cargo",
            ShipTypeCode = 70,
            Status = 0,
            Maneuver = 0,
            Accuracy = 1,
            ROT = 0.5,
            CollectionType = "AIS"
        };

        // Assert
        Assert.Equal(123456789, vesselData.MMSI);
        Assert.Equal(9876543, vesselData.IMO);
        Assert.Equal("Test Vessel", vesselData.Name);
        Assert.Equal("ABC123", vesselData.CallSign);
        Assert.Equal("Norway", vesselData.Flag);
        Assert.Equal(60.5, vesselData.Latitude);
        Assert.Equal(10.7, vesselData.Longitude);
        Assert.Equal(15.2, vesselData.Speed);
        Assert.Equal(270.0, vesselData.Course);
        Assert.Equal(268, vesselData.Heading);
        Assert.Equal(12.5, vesselData.Draught);
        Assert.Equal(250.0, vesselData.Length);
        Assert.Equal(40.0, vesselData.Width);
        Assert.Equal("Oslo", vesselData.Destination);
        Assert.Equal("Cargo", vesselData.ShipType);
        Assert.Equal(70, vesselData.ShipTypeCode);
        Assert.Equal(0, vesselData.Status);
        Assert.Equal(0, vesselData.Maneuver);
        Assert.Equal(1, vesselData.Accuracy);
        Assert.Equal(0.5, vesselData.ROT);
        Assert.Equal("AIS", vesselData.CollectionType);
    }

    [Fact]
    public void DateTimeProperties_CanBeSet()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var vesselData = new AisVesselData
        {
            CreatedAt = now,
            Timestamp = now.AddMinutes(-5),
            StaticUpdatedAt = now.AddHours(-1),
            PositionUpdatedAt = now.AddMinutes(-2),
            ETA = now.AddHours(24)
        };

        // Act & Assert
        Assert.Equal(now, vesselData.CreatedAt);
        Assert.Equal(now.AddMinutes(-5), vesselData.Timestamp);
        Assert.Equal(now.AddHours(-1), vesselData.StaticUpdatedAt);
        Assert.Equal(now.AddMinutes(-2), vesselData.PositionUpdatedAt);
        Assert.Equal(now.AddHours(24), vesselData.ETA);
    }

    [Fact]
    public void NullableProperties_AcceptNull()
    {
        // Arrange & Act
        var vesselData = new AisVesselData
        {
            MMSI = 123456789,
            Latitude = null,
            Longitude = null,
            Speed = null,
            IMO = null,
            Name = null
        };

        // Assert
        Assert.Null(vesselData.Latitude);
        Assert.Null(vesselData.Longitude);
        Assert.Null(vesselData.Speed);
        Assert.Null(vesselData.IMO);
        Assert.Null(vesselData.Name);
    }

    [Fact]
    public void CompleteVesselData_AllPropertiesWork()
    {
        // Arrange
        var now = DateTime.UtcNow;
        
        // Act
        var vesselData = new AisVesselData
        {
            CreatedAt = now,
            Timestamp = now,
            StaticUpdatedAt = now,
            PositionUpdatedAt = now,
            MMSI = 987654321,
            Latitude = 58.95,
            Longitude = 5.73,
            Speed = 12.5,
            Course = 180.0,
            Heading = 175,
            IMO = 1234567,
            Name = "MV Atlantic",
            CallSign = "XXYZ",
            Flag = "UK",
            Draught = 10.2,
            ShipTypeCode = 70,
            ShipType = "Cargo ship",
            Length = 200.0,
            Width = 32.0,
            ETA = now.AddHours(12),
            Destination = "Bergen",
            Status = 5,
            Maneuver = 0,
            Accuracy = 1,
            ROT = -0.3,
            CollectionType = "Terrestrial AIS"
        };

        // Assert - verify all properties set correctly
        Assert.NotEqual(DateTime.MinValue, vesselData.CreatedAt);
        Assert.True(vesselData.MMSI > 0);
        Assert.NotNull(vesselData.Name);
        Assert.True(vesselData.Latitude.HasValue);
        Assert.True(vesselData.Speed.HasValue);
    }
}
