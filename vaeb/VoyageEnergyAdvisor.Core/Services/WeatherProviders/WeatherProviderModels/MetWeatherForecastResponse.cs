using System.Text.Json.Serialization;

namespace VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models
{
    public record MetWeatherForecastResponse
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("geometry")]
        public required Geometry Geometry { get; init; }

        [JsonPropertyName("properties")]
        public required Properties Properties { get; init; }
    }

    public record Geometry
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("coordinates")]
        public required List<double> Coordinates { get; init; }
    }

    public record Properties
    {
        [JsonPropertyName("meta")]
        public required Meta Meta { get; init; }

        [JsonPropertyName("timeseries")]
        public required List<TimeSeries> Timeseries { get; init; }
    }

    public record Meta
    {
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; init; }

        [JsonPropertyName("units")]
        public required Dictionary<string, string> Units { get; init; }
    }

    public record TimeSeries
    {
        [JsonPropertyName("time")]
        public required DateTime Time { get; init; }

        [JsonPropertyName("data")]
        public required TimeSeriesData Data { get; init; }
    }

    public record TimeSeriesData
    {
        [JsonPropertyName("instant")]
        public required WeatherInstant Instant { get; init; }

        [JsonPropertyName("next_12_hours")]
        public Next12Hours? Next12Hours { get; init; }

        [JsonPropertyName("next_1_hours")]
        public Next1Hours? Next1Hours { get; init; }

        [JsonPropertyName("next_6_hours")]
        public Next6Hours? Next6Hours { get; init; }
    }

    public record WeatherInstant
    {
        [JsonPropertyName("details")]
        public WeatherInstantDetails Details { get; init; } = null!;
    }

    public record WeatherInstantDetails
    {
        [JsonPropertyName("air_pressure_at_sea_level")]
        public double AirPressureAtSeaLevel { get; init; }

        [JsonPropertyName("air_temperature")]
        public double AirTemperature { get; init; }

        [JsonPropertyName("air_temperature_percentile_10")]
        public double AirTemperaturePercentile10 { get; init; }

        [JsonPropertyName("air_temperature_percentile_90")]
        public double AirTemperaturePercentile90 { get; init; }

        [JsonPropertyName("cloud_area_fraction")]
        public double CloudAreaFraction { get; init; }

        [JsonPropertyName("cloud_area_fraction_high")]
        public double CloudAreaFractionHigh { get; init; }

        [JsonPropertyName("cloud_area_fraction_low")]
        public double CloudAreaFractionLow { get; init; }

        [JsonPropertyName("cloud_area_fraction_medium")]
        public double CloudAreaFractionMedium { get; init; }

        [JsonPropertyName("dew_point_temperature")]
        public double DewPointTemperature { get; init; }

        [JsonPropertyName("fog_area_fraction")]
        public double FogAreaFraction { get; init; }

        [JsonPropertyName("relative_humidity")]
        public double RelativeHumidity { get; init; }

        [JsonPropertyName("ultraviolet_index_clear_sky")]
        public double UltravioletIndexClearSky { get; init; }

        [JsonPropertyName("wind_from_direction")]
        public double WindFromDirection { get; init; }

        [JsonPropertyName("wind_speed")]
        public double WindSpeed { get; init; }

        [JsonPropertyName("wind_speed_of_gust")]
        public double WindSpeedOfGust { get; init; }

        [JsonPropertyName("wind_speed_percentile_10")]
        public double WindSpeedPercentile10 { get; init; }

        [JsonPropertyName("wind_speed_percentile_90")]
        public double WindSpeedPercentile90 { get; init; }
    }

    public record Next12Hours
    {
        [JsonPropertyName("summary")]
        public required Next12HoursSummary Summary { get; init; }

        [JsonPropertyName("details")]
        public required Next12HoursDetails Details { get; init; }
    }

    public record Next12HoursSummary
    {
        [JsonPropertyName("symbol_code")]
        public required string SymbolCode { get; init; }

        [JsonPropertyName("symbol_confidence")]
        public required string SymbolConfidence { get; init; }
    }

    public record Next12HoursDetails
    {
        [JsonPropertyName("probability_of_precipitation")]
        public required double ProbabilityOfPrecipitation { get; init; }
    }

    public record Next1Hours
    {
        [JsonPropertyName("summary")]
        public required Next1HoursSummary Summary { get; init; }

        [JsonPropertyName("details")]
        public required Next1HoursDetails Details { get; init; }
    }

    public record Next1HoursSummary
    {
        [JsonPropertyName("symbol_code")]
        public required string SymbolCode { get; init; }
    }

    public record Next1HoursDetails
    {
        [JsonPropertyName("precipitation_amount")]
        public required double PrecipitationAmount { get; init; }

        [JsonPropertyName("precipitation_amount_max")]
        public required double PrecipitationAmountMax { get; init; }

        [JsonPropertyName("precipitation_amount_min")]
        public required double PrecipitationAmountMin { get; init; }

        [JsonPropertyName("probability_of_precipitation")]
        public required double ProbabilityOfPrecipitation { get; init; }

        [JsonPropertyName("probability_of_thunder")]
        public required double ProbabilityOfThunder { get; init; }
    }

    public record Next6Hours
    {
        [JsonPropertyName("summary")]
        public required Next6HoursSummary Summary { get; init; }

        [JsonPropertyName("details")]
        public required Next6HoursDetails Details { get; init; }
    }

    public record Next6HoursSummary
    {
        [JsonPropertyName("symbol_code")]
        public required string SymbolCode { get; init; }
    }

    public record Next6HoursDetails
    {
        [JsonPropertyName("air_temperature_max")]
        public required double AirTemperatureMax { get; init; }

        [JsonPropertyName("air_temperature_min")]
        public required double AirTemperatureMin { get; init; }

        [JsonPropertyName("precipitation_amount")]
        public required double PrecipitationAmount { get; init; }

        [JsonPropertyName("precipitation_amount_max")]
        public required double PrecipitationAmountMax { get; init; }

        [JsonPropertyName("precipitation_amount_min")]
        public required double PrecipitationAmountMin { get; init; }

        [JsonPropertyName("probability_of_precipitation")]
        public required double ProbabilityOfPrecipitation { get; init; }
    }

}

