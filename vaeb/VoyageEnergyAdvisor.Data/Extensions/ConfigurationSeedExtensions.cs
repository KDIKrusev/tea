namespace VoyageEnergyAdvisor.Data.Extensions
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using VoyageEnergyAdvisor.Data.Entities;

    public static class ConfigurationSeedExtensions
    {
        public static async Task SeedConfigurations(this IServiceProvider serviceProvider, string defaultResourcesPath)
        {
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

            var vessels = await dbContext.Vessels.ToListAsync();
            if (!vessels.Any())
            {
                Console.WriteLine("⚠️ No vessels found. Skipping configuration seeding.");
                return;
            }

            foreach (var vessel in vessels)
            {
                await SeedConfigurationsForVessel(dbContext, vessel.Id, defaultResourcesPath);
            }
        }

        private static async Task SeedConfigurationsForVessel(ApplicationDbContext dbContext, int vesselId, string defaultResourcesPath)
        {
            var configFiles = new[]
            {
                "CalmWaterResistanceServiceConfiguration.json",
                "CurrentResistanceServiceConfiguration.json",
                "WindResistanceServiceConfiguration.json",
                "OfflineWeatherProviderConfiguration.json",
                "RouteServiceConfiguration.json",
                "VoyageEnergyAdvisorConfiguration.json",
                "WeatherServiceConfiguration.json",
                "SailContributionServiceConfiguration.json",
                "FuelConsumptionServiceConfiguration.json",
                "CostCalculationServiceConfiguration.json",
                "AisServiceConfiguration.json",
                "OfflineAisProviderConfiguration.json",
                "AisStreamProviderConfiguration.json"
            };

            foreach (var fileName in configFiles)
            {
                var filePath = Path.Combine(defaultResourcesPath, fileName);
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"⚠️ Config file {fileName} not found in {defaultResourcesPath}. Skipping.");
                    continue;
                }

                var configContent = await File.ReadAllTextAsync(filePath);

                var exists = await dbContext.Configurations.AnyAsync(c => c.ConfigName == fileName && c.VesselId == vesselId);
                if (!exists)

                    dbContext.Configurations.Add(new Configuration
                    {
                        ConfigName = fileName,
                        ConfigJson = configContent,
                        VesselId = vesselId
                    });
                Console.WriteLine($"✅ Added configuration {fileName} for Vessel ID: {vesselId}");
            }

            await dbContext.SaveChangesAsync();
        }

    }
}

