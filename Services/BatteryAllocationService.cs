using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace KSailCalc.Api.Services;

/// <summary>
/// Port of the Excel "Load Demands" sheet (PowerPlantSetupAdvisesIncludingPTIOAndbatteries_test.xlsx):
/// the battery power budget cascades over the mode's load demands in strict priority order.
/// Per row: H = variation, I = min(remaining, H), J = I × coverage, L = H − J, remaining −= I.
/// Pure and synchronous — no I/O. See docs/battery-feature-analysis/02-excel-model-analysis.md §1.3.
/// </summary>
public class BatteryAllocationService : IBatteryAllocationService
{
    private readonly BatterySettings _settings;
    private readonly List<BatteryLoadPriority> _loadPriorities;

    public BatteryAllocationService(IOptions<BatterySettings> settings)
    {
        _settings = settings.Value;
        // Empty when the appsettings section is absent — fall back to the Excel defaults
        // (LoadPriorities defaults to empty to avoid ConfigurationBinder list-append duplication)
        _loadPriorities = _settings.LoadPriorities is { Count: > 0 }
            ? _settings.LoadPriorities
            : BatterySettings.CreateDefaultLoadPriorities();
    }

    public BatteryModeAllocation Allocate(OperationalMode mode, CalculatorInput input,
        double? budgetOverrideKw = null, double? propulsionOverrideKw = null)
    {
        var budget = budgetOverrideKw.HasValue
            ? Math.Max(budgetOverrideKw.Value, 0)
            : input.Battery?.AppliesTo(mode) == true ? Math.Max(input.Battery.PowerKw, 0) : 0;

        var result = new BatteryModeAllocation { Mode = mode };
        var remaining = budget;

        foreach (var priority in _loadPriorities)
        {
            var loadInputs = GetLoadInputs(priority.Load, mode, input);
            if (loadInputs is null)
                continue; // load type not applicable in this mode

            var (averageLoad, variationOverride) = loadInputs.Value;
            if (priority.Load == BatteryLoadType.Propulsion && propulsionOverrideKw.HasValue)
                averageLoad = propulsionOverrideKw.Value;

            var row = BuildRow(priority, Math.Max(averageLoad, 0), variationOverride, ref remaining);
            result.Loads.Add(row);
        }

        result.PeakShavingBandKw = result.Loads
            .Where(l => l.Function == BatteryFunction.PeakShaving)
            .Sum(l => l.CoveredBandKw);
        result.AdditionalSpinningReserveKw = result.Loads.Sum(l => l.UncoveredReserveKw);
        result.CommittedBatteryKw = result.Loads.Sum(l => l.BatteryUsedKw);
        result.RemainingBatteryKw = remaining;

        return result;
    }

    private static BatteryLoadAllocation BuildRow(
        BatteryLoadPriority priority, double averageLoad, double? variationOverrideKw, ref double remaining)
    {
        // Reserve rows must cover the full requirement (Excel: H = G);
        // peak-shaving rows only the variation band (H = G − E).
        // Mission uses an explicit override: H = heavy-consumer max as-is (Excel G7 = IF(E7>0, I3, 0)).
        var variation = variationOverrideKw ?? (priority.Function == BatteryFunction.Reserve
            ? averageLoad * (1 + priority.VariationFactor)
            : averageLoad * priority.VariationFactor);
        variation = Math.Max(variation, 0);

        var batteryUsed = Math.Min(remaining, variation);
        var coveredBand = batteryUsed * priority.CoverageFactor;
        remaining = Math.Max(remaining - batteryUsed, 0);

        return new BatteryLoadAllocation
        {
            Load = priority.Load,
            Function = priority.Function,
            CoverageFactor = priority.CoverageFactor,
            AverageLoadKw = averageLoad,
            VariationKw = variation,
            BatteryUsedKw = batteryUsed,
            CoveredBandKw = coveredBand,
            UncoveredReserveKw = variation - coveredBand
        };
    }

    /// <summary>
    /// (average load [kW], explicit variation override [kW] or null) for the load type in the
    /// given mode; null when the load type is not applicable in the mode.
    /// - DpReserve: avg = DP redundancy requirement (RESERVE ⇒ H = avg×(1+vf); Excel R5).
    /// - Mission: avg = 0 (already inside the Hotel/Mission input — no double counting);
    ///   variation override = heavy-consumer max as-is (Excel G7 = IF(E7&gt;0, I3, 0)).
    /// </summary>
    private static (double Average, double? VariationOverride)? GetLoadInputs(
        BatteryLoadType load, OperationalMode mode, CalculatorInput input)
    {
        return (load, mode) switch
        {
            (BatteryLoadType.DpReserve, OperationalMode.DP) => (input.DpRedundancyRequirementKw ?? 0, null),
            (BatteryLoadType.DpDemand, OperationalMode.DP) => (input.RequiredDPPowerKW ?? 0, null),
            (BatteryLoadType.Mission, OperationalMode.Transit or OperationalMode.DP)
                => (0, input.MissionHeavyConsumerMaxKw ?? 0),
            (BatteryLoadType.Propulsion, OperationalMode.Transit) => (input.EffectivePropulsionPower, null),
            (BatteryLoadType.Propulsion, OperationalMode.Maneuvering) => (input.ManeuveringPropulsionPowerKW, null),
            (BatteryLoadType.Hotel, OperationalMode.Transit) => (input.TransitHotelPowerKW, null),
            (BatteryLoadType.Hotel, OperationalMode.DP) => (input.DPHotelPowerKW ?? 0, null),
            (BatteryLoadType.Hotel, OperationalMode.Port) => (input.PortHotelPowerKW, null),
            (BatteryLoadType.Hotel, OperationalMode.Anchor) => (input.AnchorHotelPowerKW, null),
            (BatteryLoadType.Hotel, OperationalMode.Maneuvering) => (input.ManeuveringHotelPowerKW, null),
            _ => null
        };
    }
}
