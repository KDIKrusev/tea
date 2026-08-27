
namespace VoyageEnergyAdvisor
{
    using VoyageEnergyAdvisor.Core.Services.CacheService;
    using VoyageEnergyAdvisor.Core.Services.ProgressService;
    using VoyageEnergyAdvisor.Data.Extensions;
    using VoyageEnergyAdvisor.Extensions;

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var configurationFolder = GetConfigurationFolder(builder);
            if (configurationFolder == null)
            {
                throw new ArgumentNullException(nameof(configurationFolder), "Configuration folder cannot be null.");
            }

            AddConfigurationFromFiles(builder);

            builder.Services.AddHttpClient();
            builder.Services.AddControllers();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<ICacheService, CacheService>();
            builder.Services.AddMemoryCache();

            builder.Services
                  .AddWeatherServiceAndProviders(builder.Configuration)
                  .AddAisServiceAndProviders(builder.Configuration)
                  .AddDomainServices(builder.Configuration, configurationFolder)
                  .AddInfrastructureServices(builder.Configuration)
                  .AddApplicationServices()
                  .AddWebApiServices()
                  .AddIdentityAndAuthentication(builder.Configuration);       

            var allowedOrigins = builder.Configuration.GetSection("AllowedCorsOrigins").Get<string[]>() ?? [];
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });


            var app = builder.Build();

            app.UseCors("CorsPolicy");
            Task.Run(async () => await app.ApplyMigrations(builder)).GetAwaiter().GetResult();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                c.RoutePrefix = string.Empty;
            });

            app.UseRouting();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<ProgressHub>("/progressHub");

            app.Run();
        }

   
        private static void AddConfigurationFromFiles(WebApplicationBuilder builder)
        {
            var defaultResourcesPath = GetDefaultResourcesPath(builder);

            var configFiles = new[]
            {
                "OfflineWeatherProviderConfiguration.json",
                "RouteServiceConfiguration.json",
                "WeatherServiceConfiguration.json",
                "CalmWaterResistanceServiceConfiguration.json",
                "WindResistanceServiceConfiguration.json",
                "CurrentResistanceServiceConfiguration.json",
                "SailContributionServiceConfiguration.json",
                "FuelConsumptionServiceConfiguration.json",
                "CostCalculationServiceConfiguration.json",
                "AisServiceConfiguration.json",
                "OfflineAisProviderConfiguration.json",
                "AisStreamProviderConfiguration.json"
            };

            foreach (var configFile in configFiles)
            {
                string filePath = Path.Combine(defaultResourcesPath, configFile);
                if (File.Exists(filePath))
                {
                    builder.Configuration.AddJsonFile(filePath, optional: false, reloadOnChange: true);
                }
            }
        }

        private static string GetConfigurationFolder(WebApplicationBuilder builder) =>
            GetDefaultResourcesPath(builder);

        private static string GetDefaultResourcesPath(WebApplicationBuilder builder)
        {
            var isRunningInDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
            var contentRootPath = builder.Environment.ContentRootPath;

            return isRunningInDocker
                ? Path.Combine(contentRootPath, "DefaultResources")
                : Path.Combine(contentRootPath, "..", "VoyageEnergyAdvisor.Core", "DefaultResources");
        }
    }
}
