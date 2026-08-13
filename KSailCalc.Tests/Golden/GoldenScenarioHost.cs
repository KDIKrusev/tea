using System.Text.Json;
using System.Text.Json.Serialization;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Repositories.Interfaces;
using KSailCalc.Api.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace KSailCalc.Tests.Golden;

/// <summary>
/// Runs a saved scenario through the REAL service chain, with the two things production
/// reads from outside pinned to committed files:
///   • DB rows (engine SFOC curves, sail lookup, integration levels) → Fixtures/db-fixture.json
///   • configuration (CO2 factors, battery priorities…)              → the repo's appsettings.json
///
/// Everything between input and output is production code, so the snapshots pin the actual
/// calculation pipeline. No SQL Server, no HTTP — the suite runs anywhere.
/// </summary>
internal sealed class GoldenScenarioHost
{
    private static readonly Lazy<GoldenScenarioHost> _instance = new(() => new GoldenScenarioHost());
    public static GoldenScenarioHost Instance => _instance.Value;

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CalculatorService _calculator;
    private readonly ValidationService _validation = new();
    private readonly HashSet<int> _fixtureMainEngineIds;
    private readonly HashSet<int> _fixtureAuxEngineIds;

    private GoldenScenarioHost()
    {
        var fixture = JsonSerializer.Deserialize<DbFixture>(File.ReadAllText(GoldenPaths.FixtureFile), ReadOptions)
            ?? throw new InvalidOperationException($"Empty fixture: {GoldenPaths.FixtureFile}");

        _fixtureMainEngineIds = fixture.MainEngines.Select(e => e.Id).ToHashSet();
        _fixtureAuxEngineIds = fixture.AuxiliaryEngines.Select(e => e.Id).ToHashSet();

        var (calculatorSettings, batterySettings) = LoadProductionSettings();

        var configRepo = new Mock<IKSailCalcConfigRepository>();
        configRepo.Setup(r => r.GetIntegrationLevelConfigsAsync())
            .ReturnsAsync(fixture.IntegrationLevels);

        var appData = new Mock<IAppDataAggregationService>();
        appData.Setup(s => s.GetInitialAppDataAsync()).ReturnsAsync(new AppInitialData
        {
            EngineTypes = new EngineTypesData
            {
                MainEngines = fixture.MainEngines
                    .Select(e => new EngineType { Id = e.Id, Name = e.Name, SfocData = e.SfocData })
                    .ToList(),
                AuxiliaryEngines = fixture.AuxiliaryEngines
                    .Select(e => new AuxiliaryEngineType { Id = e.Id, Name = e.Name, SfocData = e.SfocData })
                    .ToList()
            },
            OperationalProfiles = new List<VesselOperationalProfile>(),
            Metadata = new AppDataMetadata { Version = "golden-fixture" }
        });

        var sailRepo = new Mock<ISailContributionRepository>();
        sailRepo.Setup(r => r.GetSailContributionItemsAsync()).ReturnsAsync(fixture.SailContributions);

        var sfoc = new SfocService(appData.Object, new Mock<ILogger<SfocService>>().Object);
        var calcOptions = Options.Create(calculatorSettings);
        var batteryOptions = Options.Create(batterySettings);

        var pipelineRunner = new ModePipelineRunner(
            sfoc,
            new Level1OptimizationService(batteryOptions, calcOptions),
            new Level2OptimizationService(),
            new Level3DrcService(calcOptions),
            new BatteryAllocationService(batteryOptions),
            NullLogger<ModePipelineRunner>.Instance);

        _calculator = new CalculatorService(
            configRepo.Object,
            new SailContributionService(sailRepo.Object),
            pipelineRunner,
            calcOptions,
            NullLogger<CalculatorService>.Instance);
    }

    /// <summary>
    /// Mirrors CalculatorController.CalculateAllVariants: validate → calculate, with an
    /// infeasible plant answered as a 400 in the ValidationResult shape (QA-C-1).
    /// Kept deliberately explicit so a change in the controller's contract shows up here.
    /// </summary>
    public async Task<GoldenResponse> RunAsync(CalculatorInput input)
    {
        AssertFixtureCovers(input);

        var validation = _validation.ValidateInput(input);
        if (!validation.Valid)
            return new GoldenResponse(400, null, validation.Errors);

        try
        {
            var result = await _calculator.CalculateAllVariantsAsync(input, validation.Warnings);
            return new GoldenResponse(200, result, new List<string>());
        }
        catch (NoValidCombinationException ex)
        {
            return new GoldenResponse(400, null,
                new List<string> { $"No feasible engine configuration: {ex.UserMessage}" });
        }
    }

    /// <summary>
    /// A scenario referencing an engine the fixture never captured would silently fall back to
    /// SfocService's default SFOC and produce plausible-but-meaningless numbers. Fail loudly instead.
    /// </summary>
    private void AssertFixtureCovers(CalculatorInput input)
    {
        // Diesel-electric plant (MeCount == 0, Epic E1): the client parks the ME type field and
        // sends mainEngineTypeId 0, and the main curve is never read (WithFuelConsumption guards
        // on MePowerKw > 0) — so the main-engine coverage rule does not apply.
        if (input.MeCount >= 1 && !_fixtureMainEngineIds.Contains(input.MainEngineTypeId))
            throw new InvalidOperationException(
                $"Main engine id {input.MainEngineTypeId} is not in the golden fixture " +
                $"(has: {string.Join(", ", _fixtureMainEngineIds.OrderBy(i => i))}). " +
                "Add it in Golden/regenerate-fixture.ps1 and regenerate.");

        if (!_fixtureAuxEngineIds.Contains(input.AuxEngineTypeId))
            throw new InvalidOperationException(
                $"Aux engine id {input.AuxEngineTypeId} is not in the golden fixture " +
                $"(has: {string.Join(", ", _fixtureAuxEngineIds.OrderBy(i => i))}). " +
                "Add it in Golden/regenerate-fixture.ps1 and regenerate.");
    }

    /// <summary>Binds the repo's appsettings.json exactly as Program.cs does.</summary>
    private static (CalculatorSettings, BatterySettings) LoadProductionSettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GoldenPaths.AppSettingsFile, optional: false)
            .Build();

        var calculator = new CalculatorSettings();
        configuration.GetSection("CalculatorSettings").Bind(calculator);

        var battery = new BatterySettings();
        configuration.GetSection("BatterySettings").Bind(battery);

        return (calculator, battery);
    }

    // ─── fixture shape ───────────────────────────────────────────────────────────

    private sealed class DbFixture
    {
        public List<IntegrationLevelConfig> IntegrationLevels { get; set; } = new();
        public List<FixtureEngine> MainEngines { get; set; } = new();
        public List<FixtureEngine> AuxiliaryEngines { get; set; } = new();
        public List<SailContributionItem> SailContributions { get; set; } = new();
    }

    /// <summary>
    /// Local shape because EngineType.SfocData carries [JsonIgnore] (it is stripped from API
    /// responses) — the fixture must still round-trip the curves.
    /// </summary>
    private sealed class FixtureEngine
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<SfocDataPoint> SfocData { get; set; } = new();
    }
}

/// <summary>What the API would have put on the wire: status plus body or error list.</summary>
internal sealed record GoldenResponse(int Status, AllVariantsCalculationResult? Result, List<string> Errors);
