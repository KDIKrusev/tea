using FluentAssertions;
using KSailCalc.Api.Repositories;
using KSailCalc.Api.Repositories.Interfaces;
using KSailCalc.Api.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KSailCalc.Tests.Infrastructure;

/// <summary>
/// Guards the composition root.
///
/// Both other harnesses — <c>GoldenScenarioHost</c> and <c>TestServiceFactory</c> — build the
/// service graph by hand, so a missing or mis-scoped registration in <c>Program.cs</c> reaches
/// production behind a fully green suite. The refactoring epic changed constructor signatures in
/// four stories and added one service; this is the cheap guard that class of mistake needed.
///
/// The registrations are mirrored from Program.cs rather than imported — the API is a top-level
/// program with no reusable composition method. <see cref="EveryServiceTheApiRegisters_IsResolvable"/>
/// therefore also acts as a reminder: if you add a registration to Program.cs, add it here.
/// </summary>
public class DependencyInjectionTests
{
    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(unused);Database=(unused);"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddSingleton<IConfiguration>(configuration);

        // ── mirrors Program.cs ────────────────────────────────────────────────
        services.Configure<CalculatorSettings>(configuration.GetSection("CalculatorSettings"));
        services.Configure<BatterySettings>(configuration.GetSection("BatterySettings"));

        services.AddSingleton<IKSailCalcConfigRepository, HybridConfigRepository>();
        services.AddSingleton<ISailContributionRepository, SailContributionRepository>();

        services.AddScoped<ISfocService, SfocService>();
        services.AddScoped<ISailContributionService, SailContributionService>();
        services.AddScoped<ILevel1OptimizationService, Level1OptimizationService>();
        services.AddScoped<ILevel2OptimizationService, Level2OptimizationService>();
        services.AddScoped<ILevel3DrcService, Level3DrcService>();
        services.AddScoped<IModePipelineRunner, ModePipelineRunner>();
        services.AddScoped<ICalculatorService, CalculatorService>();
        services.AddScoped<IAppDataAggregationService, AppDataAggregationService>();
        services.AddScoped<IVesselResolutionService, VesselResolutionService>();
        services.AddScoped<IValidationService, ValidationService>();
        services.AddSingleton<IBatteryAllocationService, BatteryAllocationService>();
        // ──────────────────────────────────────────────────────────────────────

        // The same validation the runtime would apply with ValidateOnBuild enabled.
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    [Fact]
    public void TheContainerBuilds_WithScopeAndConstructorValidation()
    {
        var act = () => BuildProvider();

        act.Should().NotThrow(
            "ValidateOnBuild resolves every registration's constructor — a service whose dependency " +
            "is not registered fails here instead of at the first request");
    }

    [Theory]
    [InlineData(typeof(ICalculatorService))]
    [InlineData(typeof(IModePipelineRunner))]
    [InlineData(typeof(ILevel1OptimizationService))]
    [InlineData(typeof(ILevel2OptimizationService))]
    [InlineData(typeof(ILevel3DrcService))]
    [InlineData(typeof(ISfocService))]
    [InlineData(typeof(ISailContributionService))]
    [InlineData(typeof(IAppDataAggregationService))]
    [InlineData(typeof(IVesselResolutionService))]
    [InlineData(typeof(IValidationService))]
    [InlineData(typeof(IBatteryAllocationService))]
    public void EveryServiceTheApiRegisters_IsResolvable(Type serviceType)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var resolved = scope.ServiceProvider.GetService(serviceType);

        resolved.Should().NotBeNull($"{serviceType.Name} is registered in Program.cs");
    }

    [Fact]
    public void TheCalculatorResolves_WithItsWholeDependencyGraph()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        // Resolving the top of the graph exercises every constructor beneath it: the pipeline
        // runner, the three optimization levels, the SFOC service and the battery allocator.
        var calculator = scope.ServiceProvider.GetRequiredService<ICalculatorService>();

        calculator.Should().BeOfType<CalculatorService>();
    }

    [Fact]
    public void TheSingletonServicesDoNotCaptureScopedDependencies()
    {
        // ValidateScopes turns a captive dependency into an exception at resolution time.
        // BatteryAllocationService is the singleton most at risk: it sits underneath the scoped
        // pipeline runner, so the direction must stay scoped → singleton, never the reverse.
        using var provider = BuildProvider();

        var act = () => provider.GetRequiredService<IBatteryAllocationService>();

        act.Should().NotThrow("a singleton must not depend on anything scoped");
    }
}
