using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Helpers;

namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyCalculatorService.Helpers
{
    public static class VoyageEnergyAdvisorApparentWeatherHelper
    {
        public static double GetApparentWindSpeed(double trueWindSpeed, double trueWindFromDirection,
            double vesselCourse, double speedOverGround)
        {
            var windDirRelVessel = trueWindFromDirection - vesselCourse;
            // Now, 0 deg means wind blowing bow->stern, 90 deg sb->port, 180 deg stern->bow
            var windSpeedX = trueWindSpeed * Math.Cos(windDirRelVessel.DegToRad()) + speedOverGround;
            var windSpeedY = trueWindSpeed * Math.Sin(windDirRelVessel.DegToRad());
            return Math.Sqrt(Math.Pow(windSpeedX, 2) + Math.Pow(windSpeedY, 2));
        }
        
        public static double GetApparentWindFromDirection(
            double trueWindSpeed,
            double trueWindFromDirection,
            double vesselCourse,
            double speedOverGround)
        {
            var windDirRelVessel = trueWindFromDirection - vesselCourse;

            // 0° = bow→stern, 180° = stern→bow
            var windSpeedX = trueWindSpeed * Math.Cos(windDirRelVessel.DegToRad()) + speedOverGround;
            var windSpeedY = trueWindSpeed * Math.Sin(windDirRelVessel.DegToRad());

            return RoundDegrees(Math.Atan2(windSpeedY, windSpeedX).RadToDeg());
        }

        public static double GetRelativeCurrentSpeed(double trueCurrentSpeed, double trueCurrentFromDirection,
            double vesselCourse, double speedOverGround)
        {
            return trueCurrentSpeed;
        }

        public static double GetRelativeCurrentFromDirection(double trueCurrentSpeed, double trueCurrentFromDirection,
            double vesselCourse, double speedOverGround)
        {
            return RoundDegrees(trueCurrentFromDirection - vesselCourse);
        }

        public static double GetRelativeWaveFromDirection(double trueWaveFromDirection, double vesselCourse)
        {
            return RoundDegrees(trueWaveFromDirection - vesselCourse);
        }
        
        private static double RoundDegrees(double degrees)
        {
            // Normalize the degrees to be within the range of 0 to 360
            var normalizedDegrees = degrees % 360;
            // If the result is negative, convert it to a positive angle
            if (normalizedDegrees < 0)
            {
                normalizedDegrees += 360;
            }
            return normalizedDegrees;
        }
    }
}
