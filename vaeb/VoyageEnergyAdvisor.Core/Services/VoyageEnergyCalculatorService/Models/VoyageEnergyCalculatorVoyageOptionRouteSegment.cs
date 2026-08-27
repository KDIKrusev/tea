
using VoyageEnergyAdvisor.Core.CommonModels;

namespace VoyageEnergyAdvisor.Core.Models.VoyageEnergyAdvisor
{
    public record VoyageEnergyAdvisorVoyageOptionRouteSegment
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public GeoCoordinate? StartPosition { get; set; }
        public GeoCoordinate? EndPosition { get; set; }
        public double? Course { get; set; }
        public double? AverageSpeed { get; set; }
        public double? DurationInSeconds;
        public WeatherData? TrueWeather { get; set; }
        public WeatherData? ApparentWeather { get; set; }

        // Power
        public double? AvgTotalResistancePower { get; set; }
        public double? AvgCalmWaterResistancePower { get; set; }
        public double? AvgWindResistancePower;
        public double? AvgWaveResistancePower;
        public double? AvgCurrentResistancePower;
        public double? AvgSailResistancePower;
        public double? AvgNetWeatherResistancePower { get; set; }

        // Fuel Consumption 
        public double? AvgTotalResistanceFuelConsumption { get; set; }
        public double? AvgCalmWaterResistanceFuelConsumption { get; set; }
        public double? AvgWindResistanceFuelConsumption { get; set; }
        public double? AvgWaveResistanceFuelConsumption { get; set; }
        public double? AvgCurrentResistanceFuelConsumption { get; set; }
        public double? AvgSailResistanceFuelConsumption { get; set; }
        public double? AvgNetWeatherResistanceFuelConsumption { get; set; }

        // Cost
        public double? AvgCalmWaterResistanceCost { get; set; }
        public double? AvgWindResistanceCost { get; set; }
        public double? AvgWaveResistanceCost { get; set; }
        public double? AvgCurrentResistanceCost { get; set; }
        public double? AvgSailResistanceCost { get; set; }
        public double? AvgTotalResistanceCost { get; set; }
        public double? AvgNetWeatherResistanceCost { get; set; }

        // Index
        public double? FavorableWeatherIndex;
    }
}
