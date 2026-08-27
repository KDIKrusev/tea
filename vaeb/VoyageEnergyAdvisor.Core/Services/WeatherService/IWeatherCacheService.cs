namespace VoyageEnergyAdvisor.Core.Services.WeatherService
{
    using VoyageEnergyAdvisor.Core.CommonModels;
    using VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models;

    public interface IWeatherCacheService
    {
        IEnumerable<WeatherResponseInstance> GetCachedData(IEnumerable<WeatherRequestInstance> requests);
        void AddCacheData(IEnumerable<WeatherResponseInstance> forecasts);
    }
}
