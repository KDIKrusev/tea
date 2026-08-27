namespace VoyageEnergyAdvisor.Core.Services.AisProviders
{
    using Microsoft.Extensions.Logging;
    using VoyageEnergyAdvisor.Core.Repositories;
    using VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels;

    public class OfflineAisProvider : IAisProvider
    {
        public AisProviderType AisProviderType => AisProviderType.OfflineAisProvider;

        private readonly OfflineAisProviderConfiguration _config;
        private readonly ILogger<OfflineAisProvider> _logger;

        // Simple: Track current position in sample data
        private static int _currentIndex = 0;

        public OfflineAisProvider(
            IConfigurationRepository configurationRepository,
            ILogger<OfflineAisProvider> logger)
        {
            _config = configurationRepository.GetConfigurationAsync<OfflineAisProviderConfiguration>().Result
                ?? throw new Exception("Offline AIS Provider Configuration not found.");
            _logger = logger;

            logger.LogInformation($"✅ Loaded Offline AIS Provider with {_config.SampleVessels?.Length ?? 0} waypoints");
        }

        public AisResponseInstance GetAisData(AisRequestInstance request)
        {
            if (_config.SampleVessels == null || _config.SampleVessels.Length == 0)
            {
                _logger.LogWarning("⚠️ No sample vessel data available");
                throw new InvalidOperationException("No sample vessel data available");
            }

            // Get current waypoint from sample data
            var currentWaypoint = _config.SampleVessels[_currentIndex];

            // Use vessel name from request (your database) + position from sample data
            var vesselName = request.VesselName ?? currentWaypoint.Name ?? "Unknown Vessel";
            var vesselNumber = request.VesselNumber ?? "Unknown";

            _logger.LogInformation($"📍 {vesselName} ({vesselNumber}) waypoint {_currentIndex + 1}/{_config.SampleVessels.Length}: " +
                                 $"{currentWaypoint.Latitude:F6}, {currentWaypoint.Longitude:F6} → {currentWaypoint.Destination}");

            // Create AIS response using your new simplified model
            var aisResponse = new AisResponseInstance
            {
                VesselId = request.VesselId,
                MMSI = currentWaypoint.MMSI,
                IMO = currentWaypoint.IMO,
                VesselName = vesselName,
                Latitude = currentWaypoint.Latitude,
                Longitude = currentWaypoint.Longitude,
                Speed = currentWaypoint.Speed,
                Course = currentWaypoint.Course,
                Heading = currentWaypoint.Heading,
                PositionUpdatedAt = DateTime.UtcNow
            };

            // Move to next waypoint for next request
            _currentIndex = (_currentIndex + 1) % _config.SampleVessels.Length;

            // Log when route completes and restarts
            if (_currentIndex == 0)
            {
                _logger.LogInformation($"🏁 {vesselName} completed voyage, restarting from beginning");
            }

            return aisResponse;
        }

        public void ValidateRequest(AisRequestInstance request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
        }
    }
}
