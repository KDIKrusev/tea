namespace VoyageEnergyAdvisor.Core.Services.AisService
{
    using Microsoft.Extensions.Logging;
    using System.Threading.Tasks;
    using VoyageEnergyAdvisor.Core.Repositories;
    using VoyageEnergyAdvisor.Core.Services.AisProviders;
    using VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels;

    public class AisService : IAisService
    {
        private readonly ILogger<AisService> _logger;
        private readonly IAisProvider _selectedAisProvider;
        private readonly IUserVesselRepository _userVesselRepository;

        public AisService(
            IEnumerable<IAisProvider> providers,
            IConfigurationRepository configurationRepository,
            IUserVesselRepository userVesselRepository,
            ILogger<AisService> logger)
        {
            _logger = logger;
            _userVesselRepository = userVesselRepository;

            var config = configurationRepository.GetConfigurationAsync<AisServiceConfiguration>().Result;
            if (config == null)
                throw new Exception("AIS Service Configuration not found.");

            _selectedAisProvider = providers.FirstOrDefault(p => p.AisProviderType == config.SelectedAisProvider)
                          ?? throw new ArgumentException($"Provider '{config.SelectedAisProvider}' is not available.");

            logger.LogInformation($"✅ Selected AIS Provider: {config.SelectedAisProvider}");
        }

        public async Task<AisResponseInstance?> GetCurrentVesselDataAsync()
        {
            try
            {
                var vesselInfo = await _userVesselRepository.GetCurrentVesselAsync();
                if (vesselInfo == null)
                {
                    _logger.LogWarning($"⚠️ Vessel is not found in database");
                    return null;
                }

                var aisRequest = new AisRequestInstance
                {
                    VesselId = vesselInfo.Id,
                    VesselName = vesselInfo.Name,
                    VesselNumber = vesselInfo.VesselNumber,
                    RequestedAt = DateTime.UtcNow
                };

                _selectedAisProvider.ValidateRequest(aisRequest);
                var aisVesselData = _selectedAisProvider.GetAisData(aisRequest);

                if (aisVesselData != null)
                {
                    _logger.LogInformation($"📡 Retrieved vessel data: {aisVesselData.VesselName} at {aisVesselData.Latitude:F6}, {aisVesselData.Longitude:F6}, speed: {aisVesselData.Speed} knots");
                    return aisVesselData;
                }
                else
                {
                    _logger.LogWarning("⚠️ No vessel data received from provider");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vessel data from provider");
                return null;
            }
        }

    }
}
