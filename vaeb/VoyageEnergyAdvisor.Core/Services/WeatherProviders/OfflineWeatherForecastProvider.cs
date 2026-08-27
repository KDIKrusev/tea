namespace VoyageEnergyAdvisor.Core.Services.WeatherProviders
{
    using Microsoft.Extensions.Logging;
    using VoyageEnergyAdvisor.Core.CommonModels;
    using VoyageEnergyAdvisor.Core.Repositories;
    using VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models;
    using VoyageEnergyAdvisor.Core.Services.WeatherProviders.WeatherProviderModels;
    using VoyageEnergyAdvisor.Core.Services.WeatherService.Exceptions;

    public class OfflineWeatherForecastProvider : IWeatherProvider
    {
        public WeatherProviderType WeatherProviderType => WeatherProviderType.OfflineWeatherProvider;
        public TimeSpan MaxForecastRange => TimeSpan.FromDays(30);
        private readonly OfflineWeatherProviderConfiguration _config;
        private readonly int _minForecastDays = -10;
        private readonly int _maxForecastDays = 10;

        public OfflineWeatherForecastProvider(
            IConfigurationRepository configurationRepository,
            ILogger<OfflineWeatherForecastProvider> logger)
        {
            _config = configurationRepository.GetConfigurationAsync<OfflineWeatherProviderConfiguration>().Result
                ?? throw new Exception("Offline Weather Provider Configuration not found.");

            logger.LogInformation("✅ Loaded Offline Weather Provider Configuration from DB.");
        }

        public async Task<IList<WeatherResponseInstance>> GetMultiPointWeatherForecast(
            IEnumerable<WeatherRequestInstance> request)
        {
            var weatherForecasts = _config.WeatherForecast;
            var configuration = _config;

            if (weatherForecasts == null)
            {
                throw new InvalidOperationException("Weather forecasts cannot be null.");
            }

            var weatherForecastList = weatherForecasts.ToList();
            var responseList = new List<WeatherResponseInstance>();
            var requestList = request.ToList();

            int forecastCount = weatherForecastList.Count;
            int requestCount = requestList.Count();

            for (int i = 0; i < requestCount; i++)
            {
                var weatherRequest = requestList.ElementAt(i);
                var weatherData = weatherForecastList[i % forecastCount];

                var response = new WeatherResponseInstance
                {
                    Time = weatherRequest.Time,
                    Location = weatherRequest.Location,
                    Weather = weatherData.Weather,
                    RadiusMeters = 1.0, 
                    ExpirationDateTime = DateTime.UtcNow.Add(TimeSpan.FromHours(1)),
                    StartTime = weatherRequest.Time,
                    EndTime = weatherRequest.Time.Add(TimeSpan.FromHours(1))
                };

                responseList.Add(response);
            }

            return await Task.FromResult(responseList);
        }

        public (DateTime MinTimestamp, DateTime MaxTimestamp) GetValidForecastRange()
        {
            var now = DateTime.UtcNow;
            return (now.AddDays(_minForecastDays), now.AddDays(_maxForecastDays));
        }

    }
}