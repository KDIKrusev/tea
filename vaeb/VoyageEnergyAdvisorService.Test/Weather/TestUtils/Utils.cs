using VoyageEnergyAdvisor.Core.CommonModels;

namespace VoyageEnergyAdvisorService.Test.Weather.TestUtils
{
    internal class Utils
    {
        internal static IList<WeatherRequestInstance> GetGeoCoordinates(int count)
        {
            return Enumerable.Repeat(GetGeoCoordinate(), count)
                .ToList();
        }

        internal static WeatherRequestInstance GetGeoCoordinate()
        {
            return new WeatherRequestInstance
            {
                Location = new GeoCoordinate { Latitude = 1, Longitude = 2 },
                Time = DateTime.UtcNow
            };
        }
    }
}
