namespace VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels
{
    using VoyageEnergyAdvisor.Core.CommonModels;

    public class CurrentPosition
    {
        public GeoCoordinate Coordinate { get; set; } = new GeoCoordinate(0, 0);
        public double? Heading { get; set; }
        public double? Course { get; set; }
        public string? Status { get; set; }
        public string? VesselName { get; set; }
        public DateTime? PositionUpdatedAt { get; set; }
    }
}
