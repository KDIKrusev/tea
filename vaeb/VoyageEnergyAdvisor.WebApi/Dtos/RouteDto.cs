namespace VoyageEnergyAdvisor.WebApi.Dtos
{
    public class RouteDto
    {
        public string RouteName { get; set; } = null!;
        public List<GeoCoordinateDto> Waypoints { get; set; } = new(); 
    }
}
