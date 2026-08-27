namespace VoyageEnergyAdvisor.Core.Services.WeatherProviders.WeatherProviderModels
{
    public class WeatherProviderConfiguration
    {
        public double Radius { get; set; } 
        public TimeSpan ExpirationPeriod { get; set; } 
        public DateTime EndTime { get; set; } 

        public TimeSpan ForecastDuration { get; set; }
    }
}
