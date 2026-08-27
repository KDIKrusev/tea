namespace VoyageEnergyAdvisor.WebApi.Dtos
{
    public class VoyageEnergyAdvisorOptimalVoyageRequestDto
    {
        public long Etd { get; set; }
        public long Eta { get; set; }
        public double SpeedMin { get; set; }
        public double SpeedMax { get; set; }
        public RouteDto Route { get; set; } = null!;
    }
}
