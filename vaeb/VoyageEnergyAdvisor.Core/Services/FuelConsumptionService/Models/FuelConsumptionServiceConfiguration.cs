namespace VoyageEnergyAdvisor.Core.Services.FuelConsumptionService.Models
{
    public record PropulsionEfficiencyItem(double SpeedMetersPerSecond, double OverallPropulsionEfficiency);
    public record FuelConversionFactorItem(int EngineLoadPercent, double FuelConsumptionKgPerKWh);
    public record FuelConsumptionServiceConfiguration
    {
        public required IEnumerable<PropulsionEfficiencyItem> PropulsionEfficiencyItems { get; init; }
        public required IEnumerable<FuelConversionFactorItem> FuelConversionFactorItems { get; init; }
        public int AssumedEngineLoadPercent { get; init; }
    }
}
