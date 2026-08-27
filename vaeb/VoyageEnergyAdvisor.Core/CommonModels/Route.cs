namespace VoyageEnergyAdvisor.Core.CommonModels
{
    public class Route
    {
        public string RouteName { get; set; } = null!;
        public List<GeoCoordinate> Waypoints { get; set; } = new(); 
    }
}
