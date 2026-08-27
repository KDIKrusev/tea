namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models
{
    public class VoyageEnergyAdvisorResponse
    {
        public double VoyageDistance { get; set; }
        public List<VoyageEnergyAdvisorVoyageOption> VoyageOptions { get; set; } = new();

        public string ValidationMessage { get; set; } = null!;
    }
}
