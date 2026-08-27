using Newtonsoft.Json;
using VoyageEnergyAdvisor.Core.Configuration.RouteConfiguration.Models;
using VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels;

namespace VoyageEnergyAdvisor.Core.Services.RouteService
{
    public class RouteServiceConfiguration
    {
        public RouteProviderType SelectedRouteProvider { get; init; }

        public LocalFilesRouteProviderConfiguration? LocalFilesProviderConfig { get; init; }

        public NavBoxRouteProviderConfiguration? NavBoxProviderConfig { get; set; }
    }
}
