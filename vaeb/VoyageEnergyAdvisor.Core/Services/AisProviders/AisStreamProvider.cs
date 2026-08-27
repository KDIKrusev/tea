namespace VoyageEnergyAdvisor.Core.Services.AisProviders
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VoyageEnergyAdvisor.Core.Repositories;
    using VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels;
    using VoyageEnergyAdvisor.Core.Services.CacheService;

    public class AisStreamProvider : IAisProvider
    {
        public AisProviderType AisProviderType => AisProviderType.AisStreamProvider;
        private readonly AisStreamProviderConfiguration _config;
        private readonly ILogger<AisStreamProvider> _logger;
        private readonly IServiceProvider _serviceProvider;
        private static readonly TimeSpan MaxDataAge = TimeSpan.FromHours(1);

        public AisStreamProvider(
            IConfigurationRepository configurationRepository,
            IServiceProvider serviceProvider,
            ILogger<AisStreamProvider> logger)
        {
            _config = configurationRepository.GetConfigurationAsync<AisStreamProviderConfiguration>().Result
                ?? throw new Exception("AISStream Provider Configuration not found.");
            _logger = logger;
            _serviceProvider = serviceProvider;

            _logger.LogInformation($"✅ AISStream Provider initialized for {_config.FilterShipMMSI.Length} vessels");
        }

        public AisResponseInstance? GetAisData(AisRequestInstance request)
        {
            var mmsi = GetVesselMMSI(request);
            var cacheService = _serviceProvider.GetRequiredService<ICacheService>();
            var cacheKey = cacheService.GenerateCacheKey("ais_vessel", mmsi);

            if (cacheService.TryGetCachedItem<AisResponseInstance>(cacheKey, out var vesselData)
                && vesselData != null)
            {
                var dataAge = DateTime.UtcNow - (vesselData.PositionUpdatedAt ?? DateTime.UtcNow);

                if (dataAge > MaxDataAge)
                {
                    _logger.LogWarning($"AIS data for {request.VesselName} (MMSI: {mmsi}) is stale ({dataAge.TotalMinutes:F1} minutes old). Removing from cache.");

                    cacheService.Remove(cacheKey);

                    return null;
                }

                // Data is fresh, return it
                vesselData.VesselId = request.VesselId;
                _logger.LogInformation($"Retrieved cached AISStream data for {request.VesselName} (MMSI: {mmsi}), age: {dataAge.TotalMinutes:F1} minutes");
                return vesselData;
            }

            _logger.LogWarning($"No AISStream data available for vessel {request.VesselName} (MMSI: {mmsi})");
            return null;
        }

        public void ValidateRequest(AisRequestInstance request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            var mmsi = GetVesselMMSI(request);
            if (string.IsNullOrEmpty(mmsi))
                throw new ArgumentException("Vessel MMSI is required for AISStream provider");
        }

        private string GetVesselMMSI(AisRequestInstance request)
        {
            return _config.FilterShipMMSI.FirstOrDefault() ?? request.VesselNumber ?? string.Empty;
        }
    }
}