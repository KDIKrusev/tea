namespace VoyageEnergyAdvisorService.Test.Weather
{
    using VoyageEnergyAdvisor.Core.CommonModels;
    using Xunit;

    public class GeoCoordinateHelperTests
    {
        [Fact]
        public void GetDistanceTo_ValidCoordinates_ReturnsCorrectDistance()
        {
            // Arrange
            var from = new GeoCoordinate(37.7749, -122.4194); // San Francisco
            var to = new GeoCoordinate(34.0522, -118.2437);   // Los Angeles

            // Act
            double distance = from.GetDistanceTo(to);

            // Assert
            Assert.Equal(559603.25845754123, distance);
        }

        [Fact]
        public void GetDistanceTo_SameLocation_ReturnsZero()
        {
            // Arrange
            var location = new GeoCoordinate(37.7749, -122.4194);

            // Act
            double distance = location.GetDistanceTo(location);

            // Assert
            Assert.Equal(0.0, distance, precision: 5);
        }

        [Fact]
        public void GetDistanceTo_Equator_ReturnsCorrectDistance()
        {
            // Arrange - Points on the equator
            var from = new GeoCoordinate(0.0, 0.0);
            var to = new GeoCoordinate(0.0, 1.0); // 1 degree longitude difference

            // Act
            double distance = from.GetDistanceTo(to);

            // Assert
            // At equator, 1 degree longitude ≈ 111 km
            Assert.InRange(distance, 110000, 112000);
        }

        [Fact]
        public void GetDistanceTo_Antipodal_ReturnsHalfCircumference()
        {
            // Arrange - Opposite sides of Earth
            var north = new GeoCoordinate(90.0, 0.0); // North Pole
            var south = new GeoCoordinate(-90.0, 0.0); // South Pole

            // Act
            double distance = north.GetDistanceTo(south);

            // Assert
            // Should be approximately half Earth's circumference (π * radius)
            double expectedDistance = Math.PI * 6376500.0;
            Assert.Equal(expectedDistance, distance, precision: 0);
        }

        [Fact]
        public void GetDistanceTo_NullFromCoordinate_ThrowsArgumentNullException()
        {
            // Arrange
            GeoCoordinate? from = null;
            var to = new GeoCoordinate(37.7749, -122.4194);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => from!.GetDistanceTo(to));
        }

        [Fact]
        public void GetDistanceTo_NullToCoordinate_ThrowsArgumentNullException()
        {
            // Arrange
            var from = new GeoCoordinate(37.7749, -122.4194);
            GeoCoordinate? to = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => from.GetDistanceTo(to!));
        }
    }
}
