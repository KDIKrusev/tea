namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyCalculatorService.Models
{
    using VoyageEnergyAdvisor.Core.Models.VoyageEnergyAdvisor;
    using VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels;

    public class VoyageEnergyAdvisorLiveResponse
    {
        public DateTime Eta { get; set; }
        public double CurrentSpeed { get; set; }
        public double RemainingTimeInSeconds { get; set; }
        public IList<VoyageEnergyAdvisorVoyageOptionRouteSegment> RemainingRouteSegments { get; set; } = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>();
        public CurrentPosition? CurrentPosition { get; set; }
    }
}
