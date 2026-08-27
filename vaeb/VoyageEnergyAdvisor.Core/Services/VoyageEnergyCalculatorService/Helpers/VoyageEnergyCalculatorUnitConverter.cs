namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Helpers
{
    public static class VoyageEnergyAdvisorUnitConverter
    {
        public static double MetersToNauticalMiles(this double inputMeters)
        {
            return inputMeters / 1852;
        }

        public static double NauticalMilesToMeters(this double inputNauticalMiles)
        {
            return inputNauticalMiles * 1852;
        }

        public static double KnotsToMetersPerSecond(this double inputKnots)
        {
            return inputKnots * 0.514444;
        }

        public static double MetersPerSecondToKnots(this double inputMs)
        {
            return inputMs / 0.514444;
        }

        public static double DegToRad(this double angleDeg)
        {
            return angleDeg * Math.PI / 180;
        }

        public static double RadToDeg(this double angleRad)
        {
            return angleRad * 180 / Math.PI;
        }
    }
}
