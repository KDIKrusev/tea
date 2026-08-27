namespace VoyageEnergyAdvisor.Core.Services.WeatherProviders.WeatherProviderModels
{
    public class StormglassWeatherProviderConfiguration : WeatherProviderConfiguration
    {
        public string ApiKey { get; set; } = null!;
    }
}