using VoyageEnergyAdvisor.Core.CommonModels;

namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models
{
    public class VoyageEnergyAdvisorRequest
    {
        public DateTime? EtdMin { get; set; }
        public DateTime? EtdMax { get; set; }
        public DateTime? EtaMin { get; set; }
        public DateTime? EtaMax { get; set; }
        public double SpeedMin { get; set; }
        public double SpeedMax { get; set; }
        public Route Route { get; set; } = null!;
        public int ReturnArrayDimension;
        public TimeSelectionMode TimeSelectionMode { get; set; }
    }

    public enum TimeSelectionMode
    {
        ETD,
        ETA
    }
}
