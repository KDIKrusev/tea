namespace VoyageEnergyAdvisor.Core.Services.WeatherService
{
    using Microsoft.Extensions.Logging;
    using VoyageEnergyAdvisor.Core.CommonModels;
    using VoyageEnergyAdvisor.Core.Repositories;
    using VoyageEnergyAdvisor.Core.Services.WeatherProviders;

    public class WeatherService : IWeatherService
    {
        private readonly IWeatherProvider _selectedWeatherProvider;
        private readonly IWeatherCacheService _cacheService;

        public WeatherService(
            IEnumerable<IWeatherProvider> providers,
            IConfigurationRepository configurationRepository,
            IWeatherCacheService cacheService,
            ILogger<WeatherService> logger)
        {
            _cacheService = cacheService;

            var config = configurationRepository.GetConfigurationAsync<WeatherServiceConfiguration>().Result;
            if (config == null)
                throw new Exception("Weather Service Configuration not found.");

            _selectedWeatherProvider = providers.FirstOrDefault(p => p.WeatherProviderType == config.SelectedWeatherProvider)
                          ?? throw new ArgumentException($"Provider '{config.SelectedWeatherProvider}' is not available.");

            logger.LogInformation($"✅ Selected Weather Provider: {config.SelectedWeatherProvider}");
        }

        public TimeSpan MaxForecastRange => _selectedWeatherProvider.MaxForecastRange;

        public async Task<IEnumerable<WeatherResponseInstance>> GetWeather(
            IEnumerable<WeatherRequestInstance> weatherRequest,
            Func<double, string, Task>? progressCallback = null)
        {
            var cachedForecasts = _cacheService.GetCachedData(weatherRequest).ToList();

            var cachedRequestKeys = cachedForecasts.Select(cached => (cached.Location, cached.Time)).ToHashSet();
            var nonCachedRequests = weatherRequest
                .Where(request => !cachedRequestKeys.Contains((request.Location, request.Time)))
                .ToList();


            if (nonCachedRequests.Any())
            {
                var fetchedForecasts = (await FetchWeatherInBatches(nonCachedRequests, progressCallback)).ToList();

                _cacheService.AddCacheData(fetchedForecasts);

                return cachedForecasts.Concat(fetchedForecasts);
            }

            return cachedForecasts;

        }


        private async Task<IList<WeatherResponseInstance>> FetchWeatherInBatches(
            IEnumerable<WeatherRequestInstance> requests,
            Func<double, string, Task>? progressCallback = null)
        {
            var requestList = requests.ToList();
            List<WeatherResponseInstance> results = new();

            int totalRequests = requestList.Count;
            int processedRequests = 0;

            double startPercent = 5;
            double endPercent = 85;

            var groupedByLocation = requestList.GroupBy(r => r.Location).ToList();

            foreach (var locationGroup in groupedByLocation)
            {
                var batch = locationGroup.ToList();

                var batchResults = await _selectedWeatherProvider.GetMultiPointWeatherForecast(batch);

                processedRequests += batch.Count;

                double localProgress = processedRequests / (double)totalRequests; // 0.0 to 1.0
                double globalProgress = startPercent + (localProgress * (endPercent - startPercent));

                results.AddRange(batchResults.Where(r => r != null));

                if (progressCallback != null)
                {
                    await progressCallback(globalProgress, "Fetching weather data");
                }
            }

            return results;
        }
    }
}
