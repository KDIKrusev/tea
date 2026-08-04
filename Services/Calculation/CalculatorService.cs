using System.Globalization;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Repositories.Interfaces;
using KSailCalc.Api.Services.Battery;
using KSailCalc.Api.Services.Interfaces;
using KSailCalc.Api.Services.Results;
using Microsoft.Extensions.Options;

namespace KSailCalc.Api.Services.Calculation;

/// <summary>
/// Orchestrates iEMS savings calculations: resolve the sail contribution, run every active
/// operational mode through the optimization pipeline, then turn the per-mode results into the
/// response's three product tiers (Advanced, Pro, Premium).
///
/// Deliberately thin. The pipeline itself lives in <see cref="ModePipelineRunner"/>, the battery
/// translation in <see cref="BatteryModeAdapter"/>, and the aggregation, tier assembly and
/// power-demands panel are pure functions in <see cref="Services.Results"/>.
/// </summary>
public class CalculatorService : ICalculatorService
{
    private readonly IKSailCalcConfigRepository _configRepository;
    private readonly ISailContributionService _sailContributionService;
    private readonly IModePipelineRunner _pipelineRunner;
    private readonly CalculatorSettings _settings;
    private readonly ILogger<CalculatorService> _logger;

    public CalculatorService(
        IKSailCalcConfigRepository configRepository,
        ISailContributionService sailContributionService,
        IModePipelineRunner pipelineRunner,
        IOptions<CalculatorSettings> settings,
        ILogger<CalculatorService> logger)
    {
        _configRepository = configRepository;
        _sailContributionService = sailContributionService;
        _pipelineRunner = pipelineRunner;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Calculate results for all integration levels in one call.
    /// Runs L1→L2→L3 pipeline for Transit (and optionally DP),
    /// then builds three product tiers with cumulative savings.
    /// </summary>
    public async Task<AllVariantsCalculationResult> CalculateAllVariantsAsync(CalculatorInput input, List<ValidationWarning>? warnings = null)
    {
        var pricingConfigs = await _configRepository.GetIntegrationLevelConfigsAsync();
        // Sail contribution reduces propulsion demand for Transit mode optimization
        var sailResult = await CalculateSailIfEnabledAsync(input);

        var modeResults = await _pipelineRunner.RunAllModesAsync(input, sailResult?.TransitPropulsionAfterKw);

        // Aggregate savings across modes
        var foc = SavingsAggregator.CalculateFocBreakdown(modeResults);
        var savings = SavingsAggregator.CalculateSavings(modeResults);

        // Everything the client shows comes from Transit — the primary mode (D4/Q5).
        var transitResult = modeResults.First(m => m.Mode == OperationalMode.Transit);

        // Invariant culture: the tier keys are compared against the literals "1"/"2"/"3"
        var configMap = pricingConfigs.ToDictionary(
            c => c.IntegrationLevelId.ToString(CultureInfo.InvariantCulture));
        var tierInputs = new TierInputs(
            input, _settings, foc, savings,
            transitResult.L1.OptimalCombination,
            Level2DetailsBuilder.Build(transitResult.L2),
            Level3DetailsBuilder.Build(transitResult.L3));

        // Assembled in the order the client renders them, one builder per panel.
        var result = new AllVariantsCalculationResult
        {
            Warnings = warnings is { Count: > 0 } ? new List<ValidationWarning>(warnings) : new(),

            // 2 · power-demands-panel
            PowerDemands = PowerDemandsBuilder.Build(modeResults, input, sailResult),

            // 3 · baseline-panel (plus the sail badge it carries)
            Level1Details = Level1DetailsBuilder.Build(transitResult.L1),
            SailContribution = sailResult,

            // 4 · battery-contribution-panel — null when no mode used the battery
            BatteryDetails = BatteryDetailsBuilder.Build(input, modeResults),

            // 5 · variant-detail-panel ×3 — each tier adds one optimization level to the one below
            Advanced = TierResultBuilder.Build(tierInputs, IntegrationTier.Advanced, PricingFor(configMap, IntegrationTier.Advanced)),
            Pro = TierResultBuilder.Build(tierInputs, IntegrationTier.Pro, PricingFor(configMap, IntegrationTier.Pro)),
            Premium = TierResultBuilder.Build(tierInputs, IntegrationTier.Premium, PricingFor(configMap, IntegrationTier.Premium))
        };

        // The "before iEMS" figures every panel above is measured against.
        BaselineFiguresBuilder.ApplyTo(result, foc, input, _settings);

        // The one line that answers "what did this request actually produce" without a debugger.
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Calculated {Modes} mode(s): baseline {Baseline:F1} t/yr · L1 {L1:F1} · L2 {L2:F1} · L3 {L3:F1} t/yr " +
                "⇒ Advanced {Adv:F1} · Pro {Pro:F1} · Premium {Prem:F1} t/yr",
                modeResults.Count, foc.BaselineFoc, savings.L1, savings.L2, savings.L3,
                result.Advanced.FuelSavings, result.Pro.FuelSavings, result.Premium.FuelSavings);
        }

        return result;
    }

    /// <summary>
    /// The pricing row for a tier. A row deactivated in the database used to surface as a bare
    /// <see cref="KeyNotFoundException"/> and an HTTP 500; name the missing level instead so the
    /// cause is diagnosable from the log alone.
    /// </summary>
    private static IntegrationLevelConfig PricingFor(
        IReadOnlyDictionary<string, IntegrationLevelConfig> configMap, IntegrationTier tier)
        => configMap.TryGetValue(tier.ConfigKey, out var config)
            ? config
            : throw new InvalidOperationException(
                $"No active IntegrationLevel row with id {tier.ConfigKey} — the {nameof(AllVariantsCalculationResult)} " +
                "cannot be built without pricing for every tier. Check the IntegrationLevel table's IsActive flags.");

    // SailEnabled controls whether sail is included in calculations (the UI-only "installed" flag
    // is not part of the backend contract)
    private async Task<SailContributionResult?> CalculateSailIfEnabledAsync(CalculatorInput input)
    {
        if (!input.SailEnabled
            || !input.TrueWindSpeed.HasValue || !input.WindAngleRelVessel.HasValue
            || input.TrueWindSpeed.Value <= 0 || input.VesselSpeedKnots <= 0)
            return null;

        return await _sailContributionService.CalculateSailContributionAsync(
            input.TrueWindSpeed.Value,
            input.WindAngleRelVessel.Value,
            input.VesselSpeedKnots,
            input.EffectivePropulsionPower);
    }
}
