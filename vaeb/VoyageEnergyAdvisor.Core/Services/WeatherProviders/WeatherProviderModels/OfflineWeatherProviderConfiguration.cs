using VoyageEnergyAdvisor.Core.CommonModels;

namespace VoyageEnergyAdvisor.Core.Services.WeatherProviders.WeatherProviderModels
{
    public class OfflineWeatherProviderConfiguration : WeatherProviderConfiguration
    {
        public TimeSpan UpdatedAtTimeDelta { get; set; }
        public IList<WeatherResponseInstance>? WeatherForecast { get; set; }
    }
}