using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models;
using VoyageEnergyAdvisor.Core.Services.WeatherProviders.WeatherProviderModels;
using VoyageEnergyAdvisor.Core.Services.WeatherService.Exceptions;

namespace VoyageEnergyAdvisor.Core.Services.WeatherProviders
{
    public class MeteomaticsWeatherForecastProvider : IWeatherProvider
    {
        public WeatherProviderType WeatherProviderType => WeatherProviderType.MeteomaticsWeatherProvider;
        public TimeSpan MaxForecastRange => TimeSpan.FromDays(9);
        private readonly IOptions<MeteomaticsWeatherProviderConfiguration> _weatherForecastOptions;
        private readonly HttpClient _httpClient;
        private readonly double _requestRadiusMeters = 1000;
        private readonly TimeSpan _updatePeriod = TimeSpan.FromHours(1);
        private readonly TimeSpan _expirationPeriod = TimeSpan.FromHours(1);

        public MeteomaticsWeatherForecastProvider(
            HttpClient httpClient,
            IOptions<MeteomaticsWeatherProviderConfiguration> weatherForecastOptions)
        {
            _httpClient = httpClient;
            _weatherForecastOptions = weatherForecastOptions;
        }

        public async Task<IList<WeatherResponseInstance>> GetMultiPointWeatherForecast(
            IEnumerable<WeatherRequestInstance> request)
        {
            var geoCoordinatesList = request?.ToList()
                ?? throw new ArgumentNullException(nameof(request), "Request list cannot be null.");

            var configuration = _weatherForecastOptions.Value;
            var (minDateTime, maxDateTime) = GetValidForecastRange();

            var apiRequests = PrepareRequests(geoCoordinatesList, minDateTime, maxDateTime);

            var weatherForecastRequest = new HttpRequestMessage(HttpMethod.Get, BuildRouteUri(apiRequests));
            AddAuthenticationHeader(weatherForecastRequest);

            var weatherForecastResponseMessage = await _httpClient.SendAsync(weatherForecastRequest);
            if (!weatherForecastResponseMessage.IsSuccessStatusCode)
                throw new WeatherForecastProviderException(await weatherForecastResponseMessage.Content.ReadAsStringAsync());

            var weatherForecastResponse = await ProcessSuccessfulResponse(weatherForecastResponseMessage);
            if (weatherForecastResponse.Data.Count == 0)
                throw new WeatherForecastProviderException("No data returned from Meteomatics API.");

            var adjustedResponse = MatchClosestTimes(weatherForecastResponse, geoCoordinatesList);

            return GetSinglePointWeatherForecastList(adjustedResponse, configuration).ToList();
        }

        private static List<WeatherRequestInstance> PrepareRequests(
                IEnumerable<WeatherRequestInstance> source,
                DateTime minDateTime,
                DateTime maxDateTime)
        {
            var list = new List<WeatherRequestInstance>();
            DateTime? lastTime = null;

            foreach (var req in source.OrderBy(r => r.Time))
            {
                // Clamp to valid range
                var time = req.Time < minDateTime ? minDateTime :
                           req.Time > maxDateTime ? maxDateTime :
                           req.Time;

                // Ensure UTC
                time = DateTime.SpecifyKind(time, DateTimeKind.Utc);

                // Add +1 sec if same or earlier than previous
                if (lastTime.HasValue && time <= lastTime.Value)
                    time = lastTime.Value.AddSeconds(1);

                list.Add(new WeatherRequestInstance
                {
                    Location = req.Location,
                    IsLiveMode = req.IsLiveMode,
                    Time = time
                });

                lastTime = time;
            }

            return list;
        }

        private static MeteomaticsWeatherResponse MatchClosestTimes(
            MeteomaticsWeatherResponse response,
            IList<WeatherRequestInstance> requests)
        {
            var mapped = new List<DataEntryDto>();

            foreach (var req in requests)
            {
                var closest = response.Data
                    .OrderBy(d => (d.Date - req.Time).Duration())
                    .ThenBy(d => Math.Abs(d.Lat - req.Location.Latitude) + Math.Abs(d.Lon - req.Location.Longitude))
                    .FirstOrDefault();

                if (closest != null)
                {
                    mapped.Add(closest with
                    {
                        Date = req.Time,
                        Lat = req.Location.Latitude,
                        Lon = req.Location.Longitude
                    });
                }
            }

            return response with { Data = mapped };
        }

        private async Task<MeteomaticsWeatherResponse> ProcessSuccessfulResponse(HttpResponseMessage response)
        {
            var returnString = await response.Content.ReadAsStringAsync();
            var responseDto = JsonSerializer.Deserialize<MeteomaticsWeatherResponse>(
                returnString,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });

            if (responseDto?.Data == null)
                throw new InvalidOperationException("Response data is null.");

            var invalidLocations = responseDto.Data
                .Where(e => e.Parameters.Any(p => (int)p.Value == -666))
                .ToList();

            if (invalidLocations.Any())
                throw CreateInvalidLocationsException(invalidLocations);

            return responseDto;
        }

        private WeatherForecastProviderException CreateInvalidLocationsException(List<DataEntryDto> invalidLocations)
        {
            var exceptionMessage = new StringBuilder("Weather data not applicable for given locations (lon/lat):\n");
            foreach (var loc in invalidLocations)
                exceptionMessage.AppendLine($"{loc.Lon}/{loc.Lat}");
            return new WeatherForecastProviderException(exceptionMessage.ToString());
        }

        private void AddAuthenticationHeader(HttpRequestMessage request)
        {
            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(
                $"{_weatherForecastOptions.Value.User}:{_weatherForecastOptions.Value.Password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        private IEnumerable<WeatherResponseInstance> GetSinglePointWeatherForecastList(
            MeteomaticsWeatherResponse data,
            MeteomaticsWeatherProviderConfiguration configuration )
        {
            foreach (var point in data.Data)
            {
                yield return new WeatherResponseInstance()
                {
                    Location = new GeoCoordinate(point.Lat, point.Lon),
                    Time = point.Date,
                    Weather = new WeatherData()
                    {
                        WaveHeight = point.Parameters.First(e => e.Parameter.Equals(_parameterStrings[Parameter.WaveHeight])).Value,
                        WaveFromDirection = point.Parameters.First(e => e.Parameter.Equals(_parameterStrings[Parameter.WaveDirection])).Value,
                        WavePeakPeriod = point.Parameters.First(e => e.Parameter.Equals(_parameterStrings[Parameter.WavePeriod])).Value,
                        WindSpeed = point.Parameters.First(e => e.Parameter.Equals(_parameterStrings[Parameter.WindSpeed])).Value,
                        WindFromDirection = point.Parameters.First(e => e.Parameter.Equals(_parameterStrings[Parameter.WindDirection])).Value,
                        CurrentFromDirection = point.Parameters.First(e => e.Parameter.Equals(_parameterStrings[Parameter.CurrentDirection])).Value,
                        CurrentSpeed = point.Parameters.First(e => e.Parameter.Equals(_parameterStrings[Parameter.CurrentSpeed])).Value,
                    },
                    RadiusMeters = _requestRadiusMeters,
                    ExpirationDateTime = DateTime.UtcNow.Add(_expirationPeriod),
                    StartTime = point.Date,
                    EndTime = point.Date.Add(_updatePeriod)            
                };
            }
        }

        private Uri BuildRouteUri(IList<WeatherRequestInstance> route)
        {
            var timestamps = string.Join(",",
                route.Select(r => r.Time.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture)));

            var coords = string.Join("+",
                route.Select(r =>
                    $"{r.Location.Latitude.ToString(CultureInfo.InvariantCulture)}," +
                    $"{r.Location.Longitude.ToString(CultureInfo.InvariantCulture)}"));

            var parameters =
                "max_individual_wave_height:m,mean_wave_direction:d,mean_period_total_swell:s," +
                "wind_speed_10m:ms,wind_dir_10m:d,ocean_current_direction:d,ocean_current_speed:ms";

            var url = $"https://api.meteomatics.com/{timestamps}/{parameters}/{coords}/json?route=true";
            return new Uri(url);
        }

        public (DateTime MinTimestamp, DateTime MaxTimestamp) GetValidForecastRange()
        {
            var now = DateTime.UtcNow;
            return (now, now.Add(MaxForecastRange));
        }


        private enum Parameter
        {
            WaveHeight,
            WaveDirection,
            WavePeriod,
            WindSpeed,
            WindDirection,
            CurrentSpeed,
            CurrentDirection,
        }

        private readonly Dictionary<Parameter, string> _parameterStrings = new()
        {
            {Parameter.WaveHeight, "max_individual_wave_height:m"},
            {Parameter.WaveDirection, "mean_wave_direction:d"},
            {Parameter.WavePeriod, "mean_period_total_swell:s"},
            {Parameter.WindSpeed, "wind_speed_10m:ms"},
            {Parameter.WindDirection, "wind_dir_10m:d"},
            {Parameter.CurrentDirection, "ocean_current_direction:d"},
            {Parameter.CurrentSpeed, "ocean_current_speed:ms"},
        };
    }
}
