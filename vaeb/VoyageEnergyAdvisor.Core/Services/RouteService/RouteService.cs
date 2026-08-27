namespace VoyageEnergyAdvisor.Core.Services.RouteService
{
    using VoyageEnergyAdvisor.Core.Services.RouteService.RouteProviders;
    using Microsoft.Extensions.Logging;
    using VoyageEnergyAdvisor.Core.CommonModels;
    using VoyageEnergyAdvisor.Core.Repositories;

    public class RouteService : IRouteService
    {
        private readonly ILogger<RouteService> _logger;
        private readonly IRouteProvider _selectedRouteProvider;

        public RouteService(
              IEnumerable<IRouteProvider> providers,
              IConfigurationRepository configurationRepository,
              ILogger<RouteService> logger)
        {
            _logger = logger;

            var config = configurationRepository.GetConfigurationAsync<RouteServiceConfiguration>().Result;
            if (config == null) throw new Exception("Configuration not found.");

            _selectedRouteProvider = providers.FirstOrDefault(p => p.RouteProviderType == config.SelectedRouteProvider)
                ?? throw new ArgumentException($"Provider '{config.SelectedRouteProvider}' is not available.");

            _logger.LogInformation($"Selected Route Provider: {config.SelectedRouteProvider}");
        }

        public List<string> GetRoutesList()
        {
            return _selectedRouteProvider.GetRoutesList();
        }

        public Route? GetRoute(string id)
        {
            return _selectedRouteProvider.GetRoute(id);
        }
    }
}
