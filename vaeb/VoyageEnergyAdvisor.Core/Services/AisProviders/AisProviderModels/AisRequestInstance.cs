namespace VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels
{
    public class AisRequestInstance
    {
        public int VesselId { get; set; } 
        public string? VesselName { get; set; } 
        public string? VesselNumber { get; set; } 
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }
}
