using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Configuration.RouteConfiguration.Models;
using VoyageEnergyAdvisor.Core.Models;

namespace VoyageEnergyAdvisor.Core.Services.RouteService.RouteProviders
{
    public interface IRouteProvider
    {
        RouteProviderType RouteProviderType { get; }
        List<string> GetRoutesList();
        Route? GetRoute(string id);
    }
}
