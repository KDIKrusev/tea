
using VoyageEnergyAdvisor.Core.CommonModels;

namespace VoyageEnergyAdvisor.Core.Services.RouteService
{
    public interface IRouteService
    {
        List<string>? GetRoutesList();

        Route? GetRoute(string id);
    }
}
