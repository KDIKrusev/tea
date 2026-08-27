namespace VoyageEnergyAdvisor.Core.CommonModels
{
    public static class GeoCoordinateHelper
    {
        private const double EarthRadiusMeters = 6376500.0;

        // TODO add unit test
        public static double GetDistanceTo(this GeoCoordinate from, GeoCoordinate to)
        {
            if (from == null || to == null)
            {
                throw new ArgumentNullException(from == null ? nameof(from) : nameof(to));
            }

            double num = from.Latitude * (Math.PI / 180.0);
            double num2 = from.Longitude * (Math.PI / 180.0);
            double num3 = to.Latitude * (Math.PI / 180.0);
            double num4 = to.Longitude * (Math.PI / 180.0) - num2;
            double num5 = Math.Pow(Math.Sin((num3 - num) / 2.0), 2.0) + Math.Cos(num) * Math.Cos(num3) * Math.Pow(Math.Sin(num4 / 2.0), 2.0);
            return EarthRadiusMeters * (2.0 * Math.Atan2(Math.Sqrt(num5), Math.Sqrt(1.0 - num5)));
        }
    }
}
