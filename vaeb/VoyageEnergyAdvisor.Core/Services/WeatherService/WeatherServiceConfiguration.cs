using VoyageEnergyAdvisor.Core.Configuration.RouteConfiguration.Models;
using VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models;

namespace VoyageEnergyAdvisor.Core.Services.WeatherService;

public record WeatherServiceConfiguration()
{
    public WeatherProviderType SelectedWeatherProvider { get; init; }
}