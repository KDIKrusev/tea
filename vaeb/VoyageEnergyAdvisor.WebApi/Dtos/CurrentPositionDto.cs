namespace VoyageEnergyAdvisor.WebApi.Dtos
{
    public class CurrentPositionDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Heading { get; set; }
        public double? Course { get; set; }
        public string? Status { get; set; }
        public string? VesselName { get; set; }
        public DateTime? PositionUpdatedAt { get; set; }

    }
}
