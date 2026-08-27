using Microsoft.Extensions.DependencyInjection;
using VoyageEnergyAdvisor.Core.Repositories;

namespace VoyageEnergyAdvisor.Core.Services.WeatherService
{
    using VoyageEnergyAdvisor.Core.CommonModels;
    using VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models;

    public class WeatherCacheService(IServiceProvider serviceProvider) : IWeatherCacheService
    {
        private readonly List<WeatherResponseInstance> _cache = new();
        private int? _prevSelectedVesselId;
        
        public IEnumerable<WeatherResponseInstance> GetCachedData(IEnumerable<WeatherRequestInstance> requests)
        {
            CleanupCache();

            return requests
                .SelectMany(request =>
                    _cache
                        .Where(entry => IsValidCacheEntry(entry, request.Location, request.Time))
                        .Select(entry => new WeatherResponseInstance
                        {
                            Location = request.Location,
                            Time = request.Time,
                            Weather = entry.Weather,
                            RadiusMeters = entry.RadiusMeters,
                            ExpirationDateTime = entry.ExpirationDateTime,
                            StartTime = entry.StartTime,
                            //EndTime = entry.EndTime
                        })
                );
        }



        public void AddCacheData(IEnumerable<WeatherResponseInstance> forecasts)
        {
            CleanupCache();

            foreach (var forecast in forecasts)
            {
                // Only add forecasts that are not already in the cache
                if (!_cache.Any(entry => IsValidCacheEntry(entry, forecast.Location, forecast.Time)))
                {
                    _cache.Add(new WeatherResponseInstance
                    {
                        Time = forecast.Time,
                        Location = forecast.Location,
                        Weather = forecast.Weather,
                        ExpirationDateTime = forecast.ExpirationDateTime,
                        RadiusMeters = forecast.RadiusMeters,
                        StartTime = forecast.StartTime,
                        EndTime = forecast.EndTime
                    });
                }
            }
        }

        public IEnumerable<WeatherResponseInstance> GetCacheEntries()
        {
            return _cache.AsReadOnly();
        }

        private void ClearAll()
        {
            _cache.Clear();
        }

        private void CleanupCache()
        {
            ClearCacheIfVesselChanged();
            RemoveExpiredCacheEntries();
        }
        
        void RemoveExpiredCacheEntries()
        {
            _cache.RemoveAll(entry => entry.ExpirationDateTime <= DateTime.UtcNow);
        }

        private bool IsValidCacheEntry(WeatherResponseInstance entry, GeoCoordinate location, DateTime time)
        {
            return time >= entry.StartTime &&
                   time < entry.EndTime &&
                   new GeoCoordinate(entry.Location.Latitude, entry.Location.Longitude)
                       .GetDistanceTo(location) <= entry.RadiusMeters;
        }
        
        private void ClearCacheIfVesselChanged()
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var selectedVessel = scope.ServiceProvider.GetRequiredService<IUserVesselRepository>()
                    .GetCurrentVesselAsync().Result;
                if (_prevSelectedVesselId == null || (selectedVessel != null && (_prevSelectedVesselId != selectedVessel.Id)))
                {
                    ClearAll();
                    _prevSelectedVesselId = selectedVessel?.Id;
                }
            }
        }
    }
}
