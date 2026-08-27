namespace VoyageEnergyAdvisor.Core.Services.CostCalculationService
{
    using VoyageEnergyAdvisor.Core.Services.CostCalculationService.Models;

    public class CostCalculationService : ICostCalculationService
    {
        private CostCalculationServiceConfiguration _config; 

        public CostCalculationService(CostCalculationServiceConfiguration config)
        {
            _config = config;
        }

        public double GetFuelPricePerKg()
        {
            return _config.FuelPricePerKg;
        }
    }
}
