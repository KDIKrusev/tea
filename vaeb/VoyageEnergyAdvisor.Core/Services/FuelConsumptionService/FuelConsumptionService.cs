namespace VoyageEnergyAdvisor.Core.Services.FuelConsumptionService
{
    using VoyageEnergyAdvisor.Core.Services.FuelConsumptionService.Models;

    public class FuelConsumptionService : IFuelConsumptionService
    {
        private FuelConsumptionServiceConfiguration _config;

        public FuelConsumptionService(FuelConsumptionServiceConfiguration config)
        {
            _config = config;
        }

        public double GetFuelConsumption(double resistancePower)
        {
            var propulsionEfficiencyItem = _config.PropulsionEfficiencyItems.First();

            var fuelConversionFactorItem = _config.FuelConversionFactorItems
                .First(x => x.EngineLoadPercent == _config.AssumedEngineLoadPercent);

            var fuelConsumptionRate = (fuelConversionFactorItem.FuelConsumptionKgPerKWh * resistancePower)
                            / propulsionEfficiencyItem.OverallPropulsionEfficiency;

            return fuelConsumptionRate;
        }

    }
}
