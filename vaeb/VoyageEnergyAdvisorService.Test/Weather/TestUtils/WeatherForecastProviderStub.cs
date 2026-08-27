using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models;
using VoyageEnergyAdvisor.Core.Services.WeatherProviders;

namespace VoyageEnergyAdvisorService.Test.Weather.TestUtils
{
    using VoyageEnergyAdvisor.Core.Services.WeatherProviders.WeatherProviderModels;
    public class WeatherForecastProviderStub : IWeatherProvider
    {
        public WeatherProviderType WeatherProviderType => WeatherProviderType.OfflineWeatherProvider;
        public TimeSpan MaxForecastRange => TimeSpan.FromDays(9);

        public async Task<WeatherResponseInstance?> GetSinglePointWeatherForecast(WeatherRequestInstance geoCoordinate)
        {
            return (await GetMultiPointWeatherForecast(new[] { geoCoordinate })).FirstOrDefault();
        }

        public async Task<IList<WeatherResponseInstance>> GetMultiPointWeatherForecast(
            IEnumerable<WeatherRequestInstance> geoCoordinates)
        {
            if (geoCoordinates == null) throw new ArgumentNullException(nameof(geoCoordinates));
            IList<WeatherResponseInstance> weatherForecast = geoCoordinates.Select(wp => new WeatherResponseInstance
                {
                    Location = wp.Location,
                    Time = wp.Time,
                    //UpdatedAt = DateTime.UtcNow,
                    Weather = new WeatherData()
                    {
                        CurrentFromDirection = 2, //"d",
                        CurrentSpeed = 3, // "m/s"
                        WaveFromDirection = 4, // "d"
                        WaveHeight = 5, // "m" //,
                        WavePeakPeriod = 6, // "s"),
                        WindFromDirection = 7, // "d"),
                        WindSpeed = 8, // "m/s")
                    }
                })
                .ToList();

            return await Task.FromResult(weatherForecast);
        }

        public WeatherProviderConfiguration GetProviderOptions()
        {
            return new WeatherProviderConfiguration
            {
                Radius = 1,
                ExpirationPeriod = TimeSpan.FromHours(1)
             };
        }

        public (DateTime MinTimestamp, DateTime MaxTimestamp) GetValidForecastRange()
        {
            throw new NotImplementedException();
        }

        public void ValidateRequest(IEnumerable<WeatherRequestInstance> requests)
        {
            throw new NotImplementedException();
        }
    }
}