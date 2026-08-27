namespace VoyageEnergyAdvisor.WebApi.Dtos
{
    [Serializable]
    public class VoyageEnergyAdvisorVoyageOptionRouteSegmentDto
    {
        public long StartTime { get; set; }
        public long EndTime { get; set; }
        public GeoCoordinateDto? StartPosition { get; set; }    // Todo Shall not be nullable
        public GeoCoordinateDto? EndPosition { get; set; }  // Todo Shall not be nullable
        public double? Course { get; set; }    // Todo Shall not be nullable
        public double? AverageSpeed { get; set; }   // Todo Shall not be nullable
        public double? DurationInSeconds;   // Todo Shall not be nullable
        public WeatherDataDto? TrueWeather { get; set; }  // Todo Shall not be nullable
        public WeatherDataDto? ApparentWeather;   // Todo Shall not be nullable

        // Power
        public double? AvgTotalPower { get; set; }  // Todo Shall not be nullable?
        public double? AvgCalmWaterPower { get; set; }
        public double? AvgWindPower { get; set; }
        public double? AvgWavePower { get; set; }
        public double? AvgCurrentPower { get; set; }
        public double? AvgSailPower { get; set; }
        public double? AvgNetWeatherResistancePower { get; set; } 
        public double? FavorableWeatherIndex { get; set; }

        // Fuel
        public double? AvgTotalResistanceFuelConsumption { get; set; }
        public double? AvgCalmWaterResistanceFuelConsumption { get; set; }
        public double? AvgWindResistanceFuelConsumption { get; set; }
        public double? AvgWaveResistanceFuelConsumption { get; set; }
        public double? AvgCurrentResistanceFuelConsumption { get; set; }
        public double? AvgSailResistanceFuelConsumption { get; set; }
        public double? AvgNetWeatherResistanceFuelConsumption { get; set; }

        // Cost
        public double? AvgTotalResistanceCost { get; set; }
        public double? AvgCalmWaterResistanceCost { get; set; }
        public double? AvgWindResistanceCost { get; set; }
        public double? AvgWaveResistanceCost { get; set; }
        public double? AvgCurrentResistanceCost { get; set; }
        public double? AvgSailResistanceCost { get; set; }
        public double? AvgNetWeatherResistanceCost { get; set; }
    }
}
