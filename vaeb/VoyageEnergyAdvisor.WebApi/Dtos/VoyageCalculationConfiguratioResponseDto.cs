namespace VoyageEnergyAdvisor.WebApi.Dtos
{
    public class VoyageCalculationConfigurationResponseDto
    {
        public bool Success { get; set; }
        public double FuelPricePerKg { get; set; }
        public double EmissionFactorCO2PerKg { get; set; }
        public string? Message { get; set; }
    }
}
