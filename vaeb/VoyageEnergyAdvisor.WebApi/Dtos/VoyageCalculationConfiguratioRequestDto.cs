namespace VoyageEnergyAdvisor.WebApi.Dtos
{
    public class VoyageCalculationConfigurationRequestDto
    {
        public double? FuelPricePerKg { get; set; }
        public double? EmissionFactorCO2PerKg { get; set; }
    }
}
