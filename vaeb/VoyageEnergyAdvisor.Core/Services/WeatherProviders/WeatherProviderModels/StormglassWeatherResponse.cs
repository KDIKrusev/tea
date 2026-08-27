namespace VoyageEnergyAdvisor.Core.Services.WeatherProviders.WeatherProviderModels
{
    public record StormglassWeatherResponse(
        List<StormglassHourEntry> Hours,
        StormglassMeta Meta
    );

    public record StormglassMeta(
        double Lat,
        double Lng,
        int DailyQuota,
        int RequestCount
    );

    public record StormglassHourEntry
    {
        public DateTimeOffset Time { get; set; }
        public Dictionary<string, double>? WaveHeight { get; set; }
        public Dictionary<string, double>? WaveDirection { get; set; }
        public Dictionary<string, double>? WavePeriod { get; set; }
        public Dictionary<string, double>? WindSpeed { get; set; }
        public Dictionary<string, double>? WindDirection { get; set; }
        public Dictionary<string, double>? CurrentDirection { get; set; }
        public Dictionary<string, double>? CurrentSpeed { get; set; }
    }
}

