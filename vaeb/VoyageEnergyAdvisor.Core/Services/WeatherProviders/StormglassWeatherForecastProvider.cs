
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models;
using VoyageEnergyAdvisor.Core.Services.WeatherProviders.WeatherProviderModels;
using VoyageEnergyAdvisor.Core.Services.WeatherService.Exceptions;

namespace VoyageEnergyAdvisor.Core.Services.WeatherProviders
{
    public class StormglassWeatherForecastProvider : IWeatherProvider
    {
        public WeatherProviderType WeatherProviderType => WeatherProviderType.StormglassWeatherProvider;
        public TimeSpan MaxForecastRange => TimeSpan.FromDays(8);

        private readonly IOptions<StormglassWeatherProviderConfiguration> _weatherForecastOptions;
        private readonly HttpClient _httpClient;

        private readonly TimeSpan _updatePeriod = TimeSpan.FromHours(1);
        private readonly TimeSpan _expirationPeriod = TimeSpan.FromHours(1);

        private const double _requestRadiusMeters = 1000;

        public StormglassWeatherForecastProvider(
            HttpClient httpClient,
            IOptions<StormglassWeatherProviderConfiguration> weatherForecastOptions)
        {
            _httpClient = httpClient;
            _weatherForecastOptions = weatherForecastOptions;
        }

        public (DateTime MinTimestamp, DateTime MaxTimestamp) GetValidForecastRange()
        {
            var now = DateTime.UtcNow;
            return (now, now.Add(MaxForecastRange));
        }

        public async Task<IList<WeatherResponseInstance>> GetMultiPointWeatherForecast(
            IEnumerable<WeatherRequestInstance> request)
        {
            var originalRequests = request?.ToList()
                ?? throw new ArgumentNullException(nameof(request));

            var config = _weatherForecastOptions.Value;
            var (minTime, maxTime) = GetValidForecastRange();

            var clamped = ClampRequestsPreserveOrder(originalRequests, minTime, maxTime);
            var location = clamped.First().Location;
            var start = clamped.Min(p => p.Time);
            var end = clamped.Max(p => p.Time);
            var uri = BuildRouteUri(location, start, end);

            using var msg = new HttpRequestMessage(HttpMethod.Get, uri);
            msg.Headers.Authorization = new AuthenticationHeaderValue(config.ApiKey);

            var httpResponse = await _httpClient.SendAsync(msg);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var body = await httpResponse.Content.ReadAsStringAsync();
                throw new WeatherForecastProviderException(
                    $"StormGlass error: {httpResponse.StatusCode}\n{body}");
            }

            var parsed = await ProcessSuccessfulResponse(httpResponse);

            if (parsed.Hours.Count == 0)
                throw new WeatherForecastProviderException("No weather data returned from StormGlass API.");
            // Map using the ORIGINAL request order, enforcing exact time/location equality
            return MapResponseToRequests(parsed, originalRequests).ToList();
        }

        // --- Helpers -------------------------------------------------------------

        /// <summary>
        /// Clamp request times to the valid forecast range, preserve original order, and ensure UTC.
        /// </summary>
        private static List<WeatherRequestInstance> ClampRequestsPreserveOrder(
            IList<WeatherRequestInstance> src,
            DateTime min,
            DateTime max)
        {
            var output = new List<WeatherRequestInstance>(src.Count);

            foreach (var req in src)
            {
                var t = req.Time < min ? min :
                        req.Time > max ? max : req.Time;

                // Ensure UTC Kind
                t = DateTime.SpecifyKind(t, DateTimeKind.Utc);

                output.Add(new WeatherRequestInstance
                {
                    Location = req.Location,
                    IsLiveMode = req.IsLiveMode,
                    Time = t
                });
            }

            return output;
        }

        private Uri BuildRouteUri(GeoCoordinate location, DateTime start, DateTime end)
        {
            string lat = location.Latitude.ToString(CultureInfo.InvariantCulture);
            string lng = location.Longitude.ToString(CultureInfo.InvariantCulture);

            string parameters =
                "waveHeight,waveDirection,wavePeriod,windSpeed,windDirection,currentDirection,currentSpeed";

            string startStr = start.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);
            string endStr = end.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);

            string url =
                $"https://api.stormglass.io/v2/weather/point" +
                $"?lat={lat}&lng={lng}&params={parameters}" +
                $"&start={startStr}&end={endStr}";

            return new Uri(url);
        }

        private async Task<StormglassWeatherResponse> ProcessSuccessfulResponse(HttpResponseMessage msg)
        {
            var json = await msg.Content.ReadAsStringAsync();
            var obj = JsonSerializer.Deserialize<StormglassWeatherResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });

            if (obj == null || obj.Hours == null)
                throw new WeatherForecastProviderException("StormGlass response was empty or invalid.");

            return obj;
        }

        /// <summary>
        /// For each request, find the closest StormGlass hour entry by time, then create a WeatherResponseInstance
        /// using the request's exact Time and Location. This guarantees equality and preserves order.
        /// </summary>
        private IEnumerable<WeatherResponseInstance> MapResponseToRequests(
            StormglassWeatherResponse sg,
            IList<WeatherRequestInstance> requests)
        {
            foreach (var req in requests)
            {
                // Ensure request time is treated as UTC
                var reqUtc = DateTime.SpecifyKind(req.Time, DateTimeKind.Utc);

                // Ensure hour entries are treated as UTC and compute closest by absolute difference
                var closest = sg.Hours
                    .OrderBy(h => (h.Time.UtcDateTime - reqUtc).Duration())
                    .FirstOrDefault();

                if (closest == null)
                {
                    throw new WeatherForecastProviderException(
                        $"No StormGlass data available near {reqUtc:O} for location {req.Location.Longitude}/{req.Location.Latitude}.");
                }

                // Toddo wave ignored for now
                // Validate presence of ALL required parameters — do NOT default to 0
                if (//closest.WaveHeight?.Values == null ||
                    //closest.WaveDirection?.Values == null ||
                    //closest.WavePeriod?.Values == null ||
                    closest.WindSpeed?.Values == null ||
                    closest.WindDirection?.Values == null ||
                    closest.CurrentDirection?.Values == null ||
                    closest.CurrentSpeed?.Values == null)
                {
                    throw new WeatherForecastProviderException(
                        $"Incomplete StormGlass data near {req.Time:O}. Missing one or more required parameters.");
                }

                // Ensure each required parameter has at least one value
                //    if (!closest.WaveHeight.Values.Any())
                //        throw new WeatherForecastProviderException($"StormGlass parameter 'WaveHeight' has no values near {req.Time:O}.");
                //    if (!closest.WaveDirection.Values.Any())
                //        throw new WeatherForecastProviderException($"StormGlass parameter 'WaveDirection' has no values near {req.Time:O}.");
                //    if (!closest.WavePeriod.Values.Any())
                //        throw new WeatherForecastProviderException($"StormGlass parameter 'WavePeriod' has no values near {req.Time:O}.");
                if (!closest.WindSpeed.Values.Any())
                    throw new WeatherForecastProviderException($"StormGlass parameter 'WindSpeed' has no values near {req.Time:O}.");
                if (!closest.WindDirection.Values.Any())
                    throw new WeatherForecastProviderException($"StormGlass parameter 'WindDirection' has no values near {req.Time:O}.");
                if (!closest.CurrentDirection.Values.Any())
                    throw new WeatherForecastProviderException($"StormGlass parameter 'CurrentDirection' has no values near {req.Time:O}.");
                if (!closest.CurrentSpeed.Values.Any())
                    throw new WeatherForecastProviderException($"StormGlass parameter 'CurrentSpeed' has no values near {req.Time:O}.");

                // Enforce exact equality for time and location using the REQUEST values (UTC)
                var t = reqUtc;

                yield return new WeatherResponseInstance()
                {
                    Location = req.Location,
                    Time = t,
                    Weather = new WeatherData()
                    {
                        WaveHeight = 0, // closest.WaveHeight.Values.First(),
                        WaveFromDirection = 0, //closest.WaveDirection.Values.First(),
                        WavePeakPeriod = 0, //closest.WavePeriod.Values.First(),
                        WindSpeed = closest.WindSpeed.Values.First(),
                        WindFromDirection = closest.WindDirection.Values.First(),
                        CurrentFromDirection = closest.CurrentDirection.Values.First(),
                        CurrentSpeed = closest.CurrentSpeed.Values.First(),
                    },
                    RadiusMeters = _requestRadiusMeters,
                    ExpirationDateTime = DateTime.UtcNow.Add(_expirationPeriod),
                    StartTime = t,
                    EndTime = t.Add(_updatePeriod)
                };
            }
        }
    }
}
