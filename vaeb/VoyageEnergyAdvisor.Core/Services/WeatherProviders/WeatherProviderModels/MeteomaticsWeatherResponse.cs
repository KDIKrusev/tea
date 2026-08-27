namespace VoyageEnergyAdvisor.Core.Services.WeatherProviders.WeatherProviderModels
{
    public record ParameterDto
    {
        public required string Parameter { get; init; }
        public double Value { get; init; }
    }

    public record DataEntryDto
    {
        public double Lat { get; init; }
        public double Lon { get; init; }
        public DateTime Date { get; init; }
        public required IReadOnlyList<ParameterDto> Parameters { get; init; }
    }

    public record MeteomaticsWeatherResponse()
    {
        public string? Version { get; init; }
        public string? User { get; init; }
        public DateTime DateGenerated { get; init; }
        public string? Status { get; init; }
        public required IReadOnlyList<DataEntryDto> Data { get; init; }
    }
}

