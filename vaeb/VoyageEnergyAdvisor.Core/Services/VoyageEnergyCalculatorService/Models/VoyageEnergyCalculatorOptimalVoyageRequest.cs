using VoyageEnergyAdvisor.Core.CommonModels;

namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models
{
    public class VoyageEnergyAdvisorOptimalVoyageRequest
    {
        public DateTime Etd { get; set; }
        public DateTime Eta { get; set; }
        public double SpeedMin { get; set; }
        public double SpeedMax { get; set; }
        public Route Route { get; set; } = null!;
    }
}
