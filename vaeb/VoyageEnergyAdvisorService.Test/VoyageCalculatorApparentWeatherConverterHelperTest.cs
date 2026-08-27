
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyCalculatorService.Helpers;

namespace VoyageEnergyAdvisorService.Test
{
    using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Helpers;
    using Xunit;

    public class VoyageEnergyAdvisorApparentWeatherHelperTest
    {
        [Fact]
        public void CanGetApparentWindSpeedAndDirection()
        {
            // No speed over ground. True wind FROM 0° (north), vessel course 90° (east).
            // rel = 0 - 90 = -90. Apparent FROM-direction should be 270° (west), speed equals true wind speed.
            Assert.Equal(270.0,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindFromDirection(25, 0, 90, 0), 1);
            Assert.Equal(25.0,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindSpeed(25, 0, 90, 0), 1);

            // No speed over ground. True wind FROM 180° (south), vessel course 135°.
            // rel = 180 - 135 = 45°. Apparent FROM-direction should be 45° (NE), speed equals true wind speed.
            Assert.Equal(45.0,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindFromDirection(25, 180, 135, 0), 1);
            Assert.Equal(25.0,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindSpeed(25, 180, 135, 0), 1);

            // Head wind due to speed over ground only.
            // No true wind -> apparent FROM-direction = 0° (north), apparent speed = SOG.
            Assert.Equal(0.0,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindFromDirection(0, 1180, 1000, 10), 1);
            Assert.Equal(10.0,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindSpeed(0, 1180, 1000, 10), 1);

            // Effective tail wind scenario: true wind FROM 180° (south), course 180° (south), SOG = 10.
            // Apparent FROM-direction becomes 0° (north), apparent speed = 10.
            Assert.Equal(0.0,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindFromDirection(20, 180, 180, 10), 1);
            Assert.Equal(30.0,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindSpeed(20, 180, 180, 10), 1);

            // True wind FROM 90° (east), heading north (course 0°), SOG = 10.
            // Apparent FROM-direction = 45°, apparent speed = 10*sqrt(2) ≈ 14.142.
            Assert.Equal(45.0,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindFromDirection(10, 90, 0, 10), 1);
            Assert.Equal(10.0 * System.Math.Sqrt(2.0),
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindSpeed(10, 90, 0, 10), 3);

            // No vessel speed and course = 0 => apparent equals true.
            // True wind FROM 195°.
            Assert.Equal(10.0,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindSpeed(10, 195, 0, 0), 6);
            Assert.Equal(195.0,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindFromDirection(10, 195, 0, 0), 6);

            // No vessel speed but course ≠ 0 => speed equals true magnitude; direction = true - course (normalized).
            // True wind FROM 195°, course 20° -> apparent FROM-direction 175°.
            Assert.Equal(10.0,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindSpeed(10, 195, 20, 0), 6);
            Assert.Equal(175.0,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindFromDirection(10, 195, 20, 0), 6);

            // Same as previous, except vessel speed is 1 => speed changes; direction ≈ 174.445°.
            Assert.NotEqual(10.0,
                System.Math.Round(VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindSpeed(10, 195, 20, 1), 3));
            Assert.Equal(174.445,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindFromDirection(10, 195, 20, 1), 3);

            // True wind FROM 270° (west), heading west (course -90° == 270°). If true magnitude equals vessel speed => apparent speed = 0.
            // Apparent FROM-direction normalizes to 360° (equivalent to 0°).
            Assert.Equal(666,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindSpeed(333, 270, -90, 333), 6);
            Assert.Equal(0.0,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindFromDirection(333, 270, -90, 333) % 360.0, 6);

            // No true wind magnitude => apparent speed equals vessel speed; apparent FROM-direction = 0°.
            Assert.Equal(18.0,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindSpeed(0, 180, 77, 18), 6);
            Assert.Equal(0.0,
                VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindFromDirection(0, 180, 77, 18), 6);
        }

        [Fact]
        public void CanGetRelativeCurrent()
        {
            // Use doubles to avoid ambiguous Math.Round overloads and to match helper signatures.
            double trueCurrentSpeed = 10.0;
            double trueCurrentDirection = 85.0; // FROM 85° (east-northeast)
            double course = 60.0;

            // Helper returns RoundDegrees(true - course) with speed unchanged.
            double expectedRelativeDirection = trueCurrentDirection - course; // 25°

            double relativeDirection = VoyageEnergyAdvisorApparentWeatherHelper.GetRelativeCurrentFromDirection(
                trueCurrentSpeed, trueCurrentDirection, course, 0.0);
            double relativeSpeed = VoyageEnergyAdvisorApparentWeatherHelper.GetRelativeCurrentSpeed(
                trueCurrentSpeed, trueCurrentDirection, course, 0.0);

            // Compare with precision instead of rounding.
            Assert.Equal(expectedRelativeDirection, relativeDirection, 6);
            Assert.Equal(trueCurrentSpeed, relativeSpeed, 6);
        }

        [Fact]
        public void CanGetRelativeWaveDirection()
        {
            // Helper returns RoundDegrees(trueWaveFromDirection - vesselCourse)
            // True wave FROM 270° (west), course -90° (== 270°): 270 - (-90) = 360 => normalized to 0°.
            var relativeWaveDirection = VoyageEnergyAdvisorApparentWeatherHelper.GetRelativeWaveFromDirection(270, -90);
            Assert.Equal(0.0, relativeWaveDirection % 360.0, 6);
        }
    }
}
