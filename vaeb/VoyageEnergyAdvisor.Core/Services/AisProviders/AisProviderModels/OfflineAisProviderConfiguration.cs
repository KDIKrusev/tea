namespace VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels
{
    public class OfflineAisProviderConfiguration
    {
        public AisVesselData[]? SampleVessels { get; set; }
    }

    public class AisVesselData
    {
        public DateTime CreatedAt { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime? StaticUpdatedAt { get; set; }
        public DateTime? PositionUpdatedAt { get; set; }
        public long MMSI { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Speed { get; set; }
        public double? Course { get; set; }
        public double? Heading { get; set; }
        public long? IMO { get; set; }
        public string? Name { get; set; }
        public string? CallSign { get; set; }
        public string? Flag { get; set; }
        public double? Draught { get; set; }
        public int? ShipTypeCode { get; set; }
        public string? ShipType { get; set; }
        public double? Length { get; set; }
        public double? Width { get; set; }
        public DateTime? ETA { get; set; }
        public string? Destination { get; set; }
        public int? Status { get; set; }
        public int? Maneuver { get; set; }
        public int? Accuracy { get; set; }
        public double? ROT { get; set; }
        public string? CollectionType { get; set; }
    }
}
