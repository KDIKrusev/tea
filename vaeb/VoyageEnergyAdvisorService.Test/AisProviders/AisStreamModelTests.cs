using VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels;
using Xunit;

namespace VoyageEnergyAdvisor.Test.AisProviders
{
    /// <summary>
    /// Tests for AIS Stream data model classes.
    /// </summary>
    public class AisStreamModelTests
    {
        #region AisStreamShipStaticData Tests

        [Fact]
        public void AisStreamShipStaticData_Constructor_InitializesWithDefaults()
        {
            // Arrange & Act
            var shipData = new AisStreamShipStaticData();

            // Assert
            Assert.Equal(0, shipData.AisVersion);
            Assert.Equal(string.Empty, shipData.CallSign);
            Assert.Equal(string.Empty, shipData.Destination);
            Assert.Null(shipData.Dimension);
            Assert.False(shipData.Dte);
            Assert.Null(shipData.Eta);
            Assert.Equal(0, shipData.FixType);
            Assert.Equal(0, shipData.ImoNumber);
            Assert.Equal(0, shipData.MaximumStaticDraught);
            Assert.Equal(0, shipData.MessageID);
            Assert.Equal(string.Empty, shipData.Name);
            Assert.Equal(0, shipData.RepeatIndicator);
            Assert.False(shipData.Spare);
            Assert.Equal(0, shipData.Type);
            Assert.Equal(0, shipData.UserID);
            Assert.False(shipData.Valid);
        }

        [Fact]
        public void AisStreamShipStaticData_AllProperties_CanBeSetAndRetrieved()
        {
            // Arrange
            var shipData = new AisStreamShipStaticData();
            var dimension = new AisStreamDimension { A = 100, B = 50, C = 10, D = 10 };
            var eta = new AisStreamEta { Day = 15, Hour = 14, Minute = 30, Month = 11 };

            // Act
            shipData.AisVersion = 2;
            shipData.CallSign = "TEST123";
            shipData.Destination = "OSLO";
            shipData.Dimension = dimension;
            shipData.Dte = true;
            shipData.Eta = eta;
            shipData.FixType = 1;
            shipData.ImoNumber = 9876543;
            shipData.MaximumStaticDraught = 12.5;
            shipData.MessageID = 5;
            shipData.Name = "TEST VESSEL";
            shipData.RepeatIndicator = 0;
            shipData.Spare = false;
            shipData.Type = 70;
            shipData.UserID = 123456789;
            shipData.Valid = true;

            // Assert
            Assert.Equal(2, shipData.AisVersion);
            Assert.Equal("TEST123", shipData.CallSign);
            Assert.Equal("OSLO", shipData.Destination);
            Assert.Same(dimension, shipData.Dimension);
            Assert.True(shipData.Dte);
            Assert.Same(eta, shipData.Eta);
            Assert.Equal(1, shipData.FixType);
            Assert.Equal(9876543, shipData.ImoNumber);
            Assert.Equal(12.5, shipData.MaximumStaticDraught);
            Assert.Equal(5, shipData.MessageID);
            Assert.Equal("TEST VESSEL", shipData.Name);
            Assert.Equal(0, shipData.RepeatIndicator);
            Assert.False(shipData.Spare);
            Assert.Equal(70, shipData.Type);
            Assert.Equal(123456789, shipData.UserID);
            Assert.True(shipData.Valid);
        }

        [Fact]
        public void AisStreamShipStaticData_NullableProperties_AcceptNull()
        {
            // Arrange
            var shipData = new AisStreamShipStaticData();

            // Act
            shipData.Dimension = null;
            shipData.Eta = null;

            // Assert
            Assert.Null(shipData.Dimension);
            Assert.Null(shipData.Eta);
        }

        #endregion

        #region AisStreamDimension Tests

        [Fact]
        public void AisStreamDimension_Constructor_InitializesWithDefaults()
        {
            // Arrange & Act
            var dimension = new AisStreamDimension();

            // Assert
            Assert.Equal(0, dimension.A);
            Assert.Equal(0, dimension.B);
            Assert.Equal(0, dimension.C);
            Assert.Equal(0, dimension.D);
        }

        [Fact]
        public void AisStreamDimension_AllProperties_CanBeSetAndRetrieved()
        {
            // Arrange
            var dimension = new AisStreamDimension();

            // Act
            dimension.A = 120;
            dimension.B = 30;
            dimension.C = 15;
            dimension.D = 10;

            // Assert
            Assert.Equal(120, dimension.A);
            Assert.Equal(30, dimension.B);
            Assert.Equal(15, dimension.C);
            Assert.Equal(10, dimension.D);
        }

        #endregion

        #region AisStreamEta Tests

        [Fact]
        public void AisStreamEta_Constructor_InitializesWithDefaults()
        {
            // Arrange & Act
            var eta = new AisStreamEta();

            // Assert
            Assert.Equal(0, eta.Day);
            Assert.Equal(0, eta.Hour);
            Assert.Equal(0, eta.Minute);
            Assert.Equal(0, eta.Month);
        }

        [Fact]
        public void AisStreamEta_AllProperties_CanBeSetAndRetrieved()
        {
            // Arrange
            var eta = new AisStreamEta();

            // Act
            eta.Day = 25;
            eta.Hour = 18;
            eta.Minute = 45;
            eta.Month = 12;

            // Assert
            Assert.Equal(25, eta.Day);
            Assert.Equal(18, eta.Hour);
            Assert.Equal(45, eta.Minute);
            Assert.Equal(12, eta.Month);
        }

        #endregion
    }

    /// <summary>
    /// Tests for small AIS Stream model classes.
    /// </summary>
    public class AisStreamSmallModelTests
    {
        [Fact]
        public void AisStreamMessageContent_Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var content = new AisStreamMessageContent();
            var positionReport = new AisStreamPositionReport();
            var staticData = new AisStreamShipStaticData();

            // Act
            content.PositionReport = positionReport;
            content.ShipStaticData = staticData;

            // Assert
            Assert.Same(positionReport, content.PositionReport);
            Assert.Same(staticData, content.ShipStaticData);
        }

        [Fact]
        public void AisStreamMessage_Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var message = new AisStreamMessage();
            var metaData = new AisStreamMetaData { MMSI = 123456789 };
            var jsonElement = System.Text.Json.JsonDocument.Parse("{}").RootElement;

            // Act
            message.MessageType = "PositionReport";
            message.MetaData = metaData;
            message.Message = jsonElement;

            // Assert
            Assert.Equal("PositionReport", message.MessageType);
            Assert.Same(metaData, message.MetaData);
            Assert.Equal(System.Text.Json.JsonValueKind.Object, message.Message.ValueKind);
        }
    }
}
