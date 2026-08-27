namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models
{
    public class VoyageEnergyAdvisorResponse
    {
        public double VoyageDistance { get; set; }

        // Each ETD/ETA slot with both ways of sailing it. See VoyageEnergyAdvisorVoyageOptionSet.
        public List<VoyageEnergyAdvisorVoyageOptionSet> VoyageOptionSets { get; set; } = new();

        public string ValidationMessage { get; set; } = null!;
    }
}
