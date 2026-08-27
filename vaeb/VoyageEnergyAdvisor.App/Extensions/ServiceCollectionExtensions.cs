namespace VoyageEnergyAdvisor.Extensions
{
    using VoyageEnergyAdvisor.Core.Configuration.RouteConfiguration.Models;
    using VoyageEnergyAdvisor.Core.Repositories;
    using VoyageEnergyAdvisor.Core.Services.CalmWaterResistanceService.Models;
    using VoyageEnergyAdvisor.Core.Services.CalmWaterResistanceService;
    using VoyageEnergyAdvisor.Core.Services.CurrentResistanceService.Models;
    using VoyageEnergyAdvisor.Core.Services.CurrentResistanceService;
    using VoyageEnergyAdvisor.Core.Services.ProgressService;
    using VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels;
    using VoyageEnergyAdvisor.Core.Services.RouteProviders;
    using VoyageEnergyAdvisor.Core.Services.RouteService.RouteProviders;
    using VoyageEnergyAdvisor.Core.Services.RouteService;
    using VoyageEnergyAdvisor.Core.Services.SailContributionService.Models;
    using VoyageEnergyAdvisor.Core.Services.SailContributionService;
    using VoyageEnergyAdvisor.Core.Services.WaveResistanceService;
    using VoyageEnergyAdvisor.Core.Services.WeatherProviders;
    using VoyageEnergyAdvisor.Core.Services.WeatherService;
    using VoyageEnergyAdvisor.Core.Services.WindResistanceService.Models;
    using VoyageEnergyAdvisor.Core.Services.WindResistanceService;
    using VoyageEnergyAdvisor.Data.DataRepositories;
    using VoyageEnergyAdvisor.WebApi.Services;
    using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService;
    using VoyageEnergyAdvisor.Core.Services.AisService;
    using VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models;
    using VoyageEnergyAdvisor.Core.Services.WeatherProviders.WeatherProviderModels;
    using VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels;
    using VoyageEnergyAdvisor.Core.Services.AisProviders;
    using VoyageEnergyAdvisor.Core.Services.FuelConsumptionService;
    using VoyageEnergyAdvisor.Core.Services.FuelConsumptionService.Models;
    using VoyageEnergyAdvisor.Core.Services.CostCalculationService;
    using VoyageEnergyAdvisor.Core.Services.CostCalculationService.Models;

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConfigService<TService, TImpl, TConfig>(
            this IServiceCollection services)
            where TService : class
            where TImpl : class, TService
            where TConfig : class
        {
            services.AddScoped<TService>(sp =>
            {
                var repo = sp.GetRequiredService<IConfigurationRepository>();
                var config = repo.GetConfigurationAsync<TConfig>().GetAwaiter().GetResult();
                if (config == null)
                    throw new InvalidOperationException($"{typeof(TConfig).Name} not found.");
                return (TImpl)Activator.CreateInstance(typeof(TImpl), config)!;
            });
            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IVoyageEnergyAdvisorService, VoyageEnergyAdvisorService>();
            services.AddScoped<IVoyageEnergyAdvisorVoyageOptionsBuilder, VoyageEnergyAdvisorVoyageOptionsBuilder>();
            services.AddScoped<IAisService, AisService>();

            return services;
        }

        public static IServiceCollection AddDomainServices(this IServiceCollection services, IConfiguration configuration, string configurationFolder)
        {
            services.AddConfigService<ICalmWaterResistanceService, CalmWaterResistanceService, CalmWaterResistanceServiceConfiguration>();
            services.AddConfigService<IWindResistanceService, WindResistanceService, WindResistanceServiceConfiguration>();
            services.AddConfigService<ICurrentResistanceService, CurrentResistanceService, CurrentResistanceServiceConfiguration>();
            services.AddConfigService<ISailContributionService, SailContributionService, SailContributionServiceConfiguration>();
            services.AddConfigService<IFuelConsumptionService, FuelConsumptionService, FuelConsumptionServiceConfiguration>();
            services.AddConfigService<ICostCalculationService, CostCalculationService, CostCalculationServiceConfiguration>();

            services.AddTransient<IWaveResistanceService, WaveResistanceService>();

            services.Configure<RouteServiceConfiguration>(configuration.GetSection("RouteServiceConfiguration"));
            services.Configure<LocalFilesRouteProviderConfiguration>(configuration.GetSection("LocalFilesRouteProviderConfiguration"));
            services.AddTransient<IRouteProvider, LocalFilesRouteProvider>();
            services.Configure<NavBoxRouteProviderConfiguration>(configuration.GetSection("NavBoxRouteProviderConfiguration"));
            services.AddTransient<IRouteProvider, NavBoxRouteProvider>();
            services.AddTransient<IRouteService, RouteService>();

            services.AddScoped<IWeatherService, WeatherService>();
            services.AddScoped<IProgressService, ProgressService>();
            services.AddSingleton<IWeatherCacheService, WeatherCacheService>();

            return services;
        }

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ICurrentUserRepository, CurrentUserRepository>();
            services.AddScoped<IRouteRepository, RouteRepository>();
            services.AddScoped<IUserVesselRepository, UserVesselRepository>();
            services.AddTransient<IConfigurationRepository, ConfigurationRepository>();
            services.AddScoped<ICancellationTokenService, CancellationTokenService>();
            return services;
        }

        public static IServiceCollection AddWebApiServices(this IServiceCollection services)
        {
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IUserVesselService, UserVesselService>();
            return services;
        }

        public static IServiceCollection AddWeatherServiceAndProviders(
           this IServiceCollection services,
           IConfiguration configuration)
        {
            services.Configure<WeatherServiceConfiguration>(
                configuration.GetSection("WeatherServiceConfiguration"));
            services.Configure<OfflineWeatherProviderConfiguration>(
                configuration.GetSection("OfflineWeatherProviderConfiguration"));
            services.Configure<MeteomaticsWeatherProviderConfiguration>(
                configuration.GetSection("MeteomaticsWeatherServiceProviderConfig"));
            services.Configure<StormglassWeatherProviderConfiguration>(
                configuration.GetSection("StormglassWeatherProviderConfig"));

            services.AddSingleton<IWeatherCacheService, WeatherCacheService>();
            services.AddScoped<IWeatherService, WeatherService>();
            services.AddScoped<IProgressService, ProgressService>();

            // Providers
            services.AddOfflineWeatherForecastProvider(configuration);
            services.AddMetWeatherForecastProvider();
            services.AddMeteomaticsWeatherForecastProvider(configuration);
            services.AddStormglassWeatherForecastProvider(configuration);
            return services;
        }

        private static IServiceCollection AddOfflineWeatherForecastProvider(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddTransient<IWeatherProvider, OfflineWeatherForecastProvider>();
            return services;
        }

        private static IServiceCollection AddMetWeatherForecastProvider(
            this IServiceCollection services)
        {
            services.AddHttpClient<MetWeatherForecastProvider>();
            services.AddTransient<IWeatherProvider, MetWeatherForecastProvider>();
            return services;
        }

        private static IServiceCollection AddMeteomaticsWeatherForecastProvider(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddHttpClient<MeteomaticsWeatherForecastProvider>();
            services.AddTransient<IWeatherProvider, MeteomaticsWeatherForecastProvider>();
            return services;
        }
        
        private static IServiceCollection AddStormglassWeatherForecastProvider(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddHttpClient<StormglassWeatherForecastProvider>();
            services.AddTransient<IWeatherProvider, StormglassWeatherForecastProvider>();
            return services;
        }
        

        public static IServiceCollection AddAisServiceAndProviders(
           this IServiceCollection services,
           IConfiguration configuration)
        {
            services.Configure<AisServiceConfiguration>(configuration.GetSection("AisServiceConfiguration"));

            services.AddOfflineAisProvider(configuration)
                    .AddAisStreamProvider(configuration);

            services.AddScoped<IAisService, AisService>();

            return services;
        }

        private static IServiceCollection AddOfflineAisProvider(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<OfflineAisProviderConfiguration>(
                configuration.GetSection("OfflineAisProviderConfiguration"));

            services.AddTransient<IAisProvider, OfflineAisProvider>();
            return services;
        }

        private static IServiceCollection AddAisStreamProvider(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<AisStreamProviderConfiguration>(
                configuration.GetSection("AisStreamProviderConfiguration"));

            services.AddHostedService<AisStreamBackgroundService>();
            services.AddTransient<IAisProvider, AisStreamProvider>();
            return services;
        }
    }
}
