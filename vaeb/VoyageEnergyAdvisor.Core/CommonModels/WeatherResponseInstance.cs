namespace VoyageEnergyAdvisor.Core.CommonModels
{
    public record WeatherResponseInstance
    {
        public DateTime Time { get; set; }
        public GeoCoordinate Location { get; set; } = null!;
        public WeatherData Weather { get; set; } = null!;

        public double RadiusMeters { get; set; }
        public DateTime ExpirationDateTime { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

    }
}
