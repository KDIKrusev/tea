namespace VoyageEnergyAdvisor.Core.Services.RouteProviders
{
    using Microsoft.Extensions.Logging;
    using VoyageEnergyAdvisor.Core.CommonModels;
    using VoyageEnergyAdvisor.Core.Configuration.RouteConfiguration.Models;
    using VoyageEnergyAdvisor.Core.Repositories;
    using VoyageEnergyAdvisor.Core.Services.RouteService.RouteProviders;

    public class LocalFilesRouteProvider : IRouteProvider
    {
        public RouteProviderType RouteProviderType => RouteProviderType.LocalFilesRouteProvider;
        private readonly IRouteRepository _routeRepository;
        private readonly ILogger<LocalFilesRouteProvider> _logger;

        public LocalFilesRouteProvider(
                ILogger<LocalFilesRouteProvider> logger,
                IRouteRepository routeRepository)
        {
            _logger = logger;
            _routeRepository = routeRepository;
        }

        public Route? GetRoute(string id)
        {
            try
            {
                var route = _routeRepository.GetRouteAsync(id).Result;
                if (route == null)
                {
                    _logger.LogWarning($"Route with ID {id} not found.");
                    return null;
                }

                return route;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving route {id}");
                return null;
            }
        }

        public List<string> GetRoutesList()
        {
            try
            {
                var routeNames = _routeRepository.GetRoutesListAsync().Result;
                if (!routeNames.Any())
                {
                    _logger.LogWarning("No routes found.");
                }
                else
                {
                    _logger.LogInformation($"Retrieved {routeNames.Count} routes.");
                }

                return routeNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving route list.");
                return new List<string>();
            }
        }
    }
}
