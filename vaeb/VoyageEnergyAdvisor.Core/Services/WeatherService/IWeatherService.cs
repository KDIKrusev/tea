using VoyageEnergyAdvisor.Core.CommonModels;

namespace VoyageEnergyAdvisor.Core.Services.WeatherService
{
    public interface IWeatherService
    {
        Task<IEnumerable<WeatherResponseInstance>> GetWeather(IEnumerable<WeatherRequestInstance> weatherRequest,Func<double, string, Task>? progressCallback = null);
    }
}
