namespace VoyageEnergyAdvisor.Data.DataRepositories
{
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using VoyageEnergyAdvisor.Core.Repositories;
    using VoyageEnergyAdvisor.Data.Entities;

    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUserVesselRepository _userVesselRepository;

        public ConfigurationRepository(ApplicationDbContext dbContext, IUserVesselRepository userVesselRepository)
        {
            _dbContext = dbContext;
            _userVesselRepository = userVesselRepository;
        }

        public async Task<T?> GetConfigurationAsync<T>() where T : class
        {
            var vessel = await _userVesselRepository.GetCurrentVesselAsync();
            if (vessel == null)
                throw new Exception("Vessel not selected.");

            string configName = typeof(T).Name;

            var configEntity = await _dbContext.Configurations
                .FirstOrDefaultAsync(c => c.ConfigName == $"{configName}.json" && c.VesselId == vessel.Id);

            if (configEntity == null) return null;

            var rootObject = JsonConvert.DeserializeObject<Dictionary<string, T>>(configEntity.ConfigJson);

            return rootObject != null && rootObject.TryGetValue(configName, out var config) ? config : null;
        }

        public async Task UpdateConfigurationAsync<T>(T configuration) where T : class
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var vessel = await _userVesselRepository.GetCurrentVesselAsync();
            if (vessel == null)
                throw new Exception("Vessel not selected.");

            string configName = typeof(T).Name;
            var configFileName = $"{configName}.json";

            // Find existing configuration
            var configEntity = await _dbContext.Configurations
                .FirstOrDefaultAsync(c => c.ConfigName == configFileName && c.VesselId == vessel.Id);

            // Serialize configuration with proper structure (wrapped in root object with config name as key)
            var rootObject = new Dictionary<string, T>
            {
                { configName, configuration }
            };
            var jsonString = JsonConvert.SerializeObject(rootObject, Formatting.Indented);

            if (configEntity != null)
            {
                // Update existing configuration
                configEntity.ConfigJson = jsonString;

                _dbContext.Configurations.Update(configEntity);
            }
          
            await _dbContext.SaveChangesAsync();
        }
    }
}
