namespace VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models
{
    using VoyageEnergyAdvisor.Core.Services.WeatherProviders.WeatherProviderModels;
    public class MeteomaticsWeatherProviderConfiguration : WeatherProviderConfiguration
    {
        public string User { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}