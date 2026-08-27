namespace VoyageEnergyAdvisor.WebApi.Dtos
{
    public class VoyageEnergyAdvisorRequestDto
    {
        public long EtdMin { get; set; }
        public long EtdMax { get; set; }
        public long EtaMin { get; set; }
        public long EtaMax { get; set; }
        public double SpeedMin { get; set; }
        public double SpeedMax { get; set; }
        public RouteDto Route { get; set; } = null!;
    }
}



