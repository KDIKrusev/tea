using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;
using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models;
using VoyageEnergyAdvisor.Core.Services.WeatherService.Exceptions;

namespace VoyageEnergyAdvisor.Core.Services.WeatherProviders
{
    public class MetWeatherForecastProvider : IWeatherProvider
    {
        public WeatherProviderType WeatherProviderType => WeatherProviderType.MetWeatherProvider;

        public TimeSpan MaxForecastRange => TimeSpan.FromDays(9);

        private readonly ICancellationTokenService _cancellationService;
        private readonly IOptionsMonitor<MetWeatherForecastProviderConfiguration> _weatherForecastOptions;
        private readonly TimeSpan _maxForecastPeriod = TimeSpan.FromDays(9);
        private readonly double _requestRadiusMeters = 1000;
        private readonly TimeSpan _updatePeriod = TimeSpan.FromHours(1);
        private readonly TimeSpan _expirationPeriod = TimeSpan.FromHours(1);
        private readonly HttpClient _httpClient;

        public MetWeatherForecastProvider(
                HttpClient httpClient,
                IOptionsMonitor<MetWeatherForecastProviderConfiguration> weatherForecastOptions,
                ICancellationTokenService cancellationService)
        {
            _httpClient = httpClient;
            _weatherForecastOptions = weatherForecastOptions;
            _cancellationService = cancellationService;
        }

        public async Task<IList<WeatherResponseInstance>> GetMultiPointWeatherForecast(
            IEnumerable<WeatherRequestInstance> requests)
        {
            List<Task<WeatherResponseInstance?>> tasks = new();
            foreach (var request in requests)
            {
                tasks.Add(GetSinglePointWeatherForecast(request));
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            // Extract the results from completed tasks
            var results = tasks
                .Where(t => t.Result != null)
                .Select(t => t.Result!)
                .ToList();
            return results;
        }

        private async Task<WeatherResponseInstance?> GetSinglePointWeatherForecast(
            WeatherRequestInstance request)
        {
            var oceanForecastTask = RequestOceanForecast(request.Location.Latitude, request.Location.Longitude);
            var weatherForecastTask = RequestWeatherForecast(request.Location.Latitude, request.Location.Longitude);

            var oceanForecastResult = await oceanForecastTask;
            var weatherForecastResult = await weatherForecastTask;

            var closestOceanForecastTime = FindClosestDateTime(oceanForecastResult.Properties.Timeseries.Select(e => e.Time), request.Time);
            var closestWeatherForecastTime = FindClosestDateTime(weatherForecastResult.Properties.Timeseries.Select(e => e.Time), request.Time);

            var oceanForecast = oceanForecastResult.Properties.Timeseries.First(e => e.Time == closestOceanForecastTime);
            var weatherForecast = weatherForecastResult.Properties.Timeseries.First(e => e.Time == closestWeatherForecastTime);

            var configuration = _weatherForecastOptions.CurrentValue;

            return BuildSinglePointWeatherForecast(request.Location, request.Time, weatherForecastResult.Properties.Meta.UpdatedAt,
                oceanForecast.Data.Instant.Details, weatherForecast.Data.Instant.Details, configuration);
        }
        public (DateTime MinTimestamp, DateTime MaxTimestamp) GetValidForecastRange()
        {
            var now = DateTime.UtcNow;
            return (now.Add(MaxForecastRange), now.Add(_maxForecastPeriod));
        }

        private async Task<MetOceanForecastResponse> RequestOceanForecast(double lat, double lon)
        {
            var oceanForecastRequest = new HttpRequestMessage(HttpMethod.Get, GetOceanForecastUrl(lat, lon));
            oceanForecastRequest.Headers.Add("User-Agent", "Source");

            var oceanForecastResponseMessage = await _httpClient.SendAsync(oceanForecastRequest, _cancellationService.Token);

            // Check if the request was successful
            if (oceanForecastResponseMessage.IsSuccessStatusCode)
            {
                var returnString = await oceanForecastResponseMessage.Content.ReadAsStringAsync();

                var response = JsonSerializer.Deserialize<MetOceanForecastResponse>(returnString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });

                if (response == null)
                {
                    throw new InvalidOperationException("Deserialization resulted in a null response.");
                }

                return response;
            }

            throw new WeatherForecastProviderException(await oceanForecastResponseMessage.Content.ReadAsStringAsync());
        }

        private async Task<MetWeatherForecastResponse> RequestWeatherForecast(double lat, double lon)
        {
            var weatherForecastRequest = new HttpRequestMessage(HttpMethod.Get, GetWeatherForecastUrl(lat, lon));
            weatherForecastRequest.Headers.Add("User-Agent", "Source");
            var weatherForecastResponseMessage = await _httpClient.SendAsync(weatherForecastRequest, _cancellationService.Token);

            // Check if the request was successful
            if (weatherForecastResponseMessage.IsSuccessStatusCode)
            {
                var returnString = await weatherForecastResponseMessage.Content.ReadAsStringAsync();

                var response = JsonSerializer.Deserialize<MetWeatherForecastResponse>(
                    returnString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    });

                if (response == null)
                {
                    throw new InvalidOperationException("Deserialization resulted in a null response.");
                }

                return response;
            }

            throw new WeatherForecastProviderException(await weatherForecastResponseMessage.Content.ReadAsStringAsync());
        }

        private string GetOceanForecastUrl(double lat, double lon)
        {
            string baseUrl = "https://api.met.no/weatherapi/oceanforecast/2.0/complete";
            var uriBuilder = new UriBuilder(baseUrl)
            {
                Query = $"lat={lat.ToString(CultureInfo.InvariantCulture)}&lon={lon.ToString(CultureInfo.InvariantCulture)}"
            };
            return uriBuilder.Uri.ToString();
        }

        private string GetWeatherForecastUrl(double lat, double lon)
        {
            string baseUrl = "https://api.met.no/weatherapi/locationforecast/2.0/complete";
            var uriBuilder = new UriBuilder(baseUrl)
            {
                Query = $"lat={lat.ToString(CultureInfo.InvariantCulture)}&lon={lon.ToString(CultureInfo.InvariantCulture)}"
            };
            return uriBuilder.Uri.ToString();
        }

        private static DateTime FindClosestDateTime(IEnumerable<DateTime> dateTimes, DateTime targetDateTime)
        {
            var dateTimeOffsets = dateTimes.ToList();
            if (dateTimes == null || !dateTimeOffsets.Any())
                throw new ArgumentException("Sequence cannot be null or empty");

            // Initialize with the first element
            var closestDateTime = dateTimeOffsets.First();
            TimeSpan closestDifference = TimeSpan.MaxValue;

            // Iterate through the sequence
            foreach (var dateTime in dateTimeOffsets)
            {
                // Calculate the time difference using Duration
                TimeSpan difference = (dateTime - targetDateTime).Duration();

                // Update the closest DateTime if a closer one is found
                if (difference < closestDifference)
                {
                    closestDateTime = dateTime;
                    closestDifference = difference;
                }
            }

            return closestDateTime;
        }

        private WeatherResponseInstance? BuildSinglePointWeatherForecast(
            GeoCoordinate location,
            DateTime time,
            DateTime updatedAt,
            OceanInstantDetails oceanInstantDetails,
            WeatherInstantDetails weatherInstantDetails,
            MetWeatherForecastProviderConfiguration configuration
          )
        {
            return new WeatherResponseInstance()
            {
                //UpdatedAt = updatedAt,
                Location = location,
                Time = time,
                Weather = new()
                {
                    CurrentFromDirection = oceanInstantDetails.SeaWaterToDirection,
                    CurrentSpeed = oceanInstantDetails.SeaWaterSpeed,
                    WaveHeight = oceanInstantDetails.SeaSurfaceWaveHeight, 
                    WaveFromDirection = oceanInstantDetails.SeaSurfaceWaveFromDirection, 
                    WindSpeed = weatherInstantDetails.WindSpeed,
                    WindFromDirection = weatherInstantDetails.WindFromDirection,
                    WavePeakPeriod = 0.000001 // Wave period not available,
                },
                RadiusMeters = _requestRadiusMeters,
                ExpirationDateTime = DateTime.UtcNow.Add(_expirationPeriod),
                StartTime = time,
                EndTime = time.Add(_updatePeriod)
            };
        }

    }
}


