namespace VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels
{
    public class AisResponseInstance
    {
        public int VesselId { get; set; }
        public long MMSI { get; set; }
        public long? IMO { get; set; }
        public string VesselName { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Speed { get; set; } 
        public double? Course { get; set; } 
        public double? Heading { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? PositionUpdatedAt { get; set; } 
    }
}
