using System.Text.Json.Serialization;

namespace VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models
{
    public record OceanMetaData
    {
        [JsonPropertyName("updated_at")]
        public required DateTime UpdatedAt { get; init; }

        public required Dictionary<string, string> Units { get; init; }
    }

    public record OceanInstantDetails
    {
        [JsonPropertyName("sea_surface_wave_from_direction")]
        public required double SeaSurfaceWaveFromDirection { get; init; }

        [JsonPropertyName("sea_surface_wave_height")]
        public required double SeaSurfaceWaveHeight { get; init; }

        [JsonPropertyName("sea_water_speed")]
        public required double SeaWaterSpeed { get; init; }

        [JsonPropertyName("sea_water_temperature")]
        public required double SeaWaterTemperature { get; init; }

        [JsonPropertyName("sea_water_to_direction")]
        public required double SeaWaterToDirection { get; init; }
    }

    public record OceanInstantSub
    {
        public required OceanInstantData Instant { get; set; }
    }

    public record OceanInstantData
    {
        public required OceanInstantDetails Details { get; init; }
    }

    public record OceanTimeSeriesData
    {
        public required DateTime Time { get; init; }
        public required OceanInstantSub Data { get; init; }
    }

    public record OceanPropertiesData
    {
        public required OceanMetaData Meta { get; init; }
        public required List<OceanTimeSeriesData> Timeseries { get; init; }
    }

    public record OceanGeometryData
    {
        public required string Type { get; init; }
        public required List<double> Coordinates { get; init; }
    }

    public record MetOceanForecastResponse
    {
        public required string Type { get; init; }
        public required OceanGeometryData Geometry { get; init; }
        public required OceanPropertiesData Properties { get; init; }
    }
}