using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models;

namespace VoyageEnergyAdvisor.Core.Services.WeatherProviders
{
    public interface IWeatherProvider
    {
        WeatherProviderType WeatherProviderType { get; }
        TimeSpan MaxForecastRange { get; }
        Task<IList<WeatherResponseInstance>> GetMultiPointWeatherForecast(
            IEnumerable<WeatherRequestInstance> request);

        (DateTime MinTimestamp, DateTime MaxTimestamp) GetValidForecastRange();
    }
}
