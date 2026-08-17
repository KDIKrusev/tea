using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Helpers;
using KSailCalc.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace KSailCalc.Api.Services.Battery;

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
        // Which rows EXIST per mode stays here. The matrix originally mirrored our reading of
        // the Excel sheet; the client corrected it for Mission (Epic E2, D-BI1: mission
        // operations are a DP affair — the row does NOT exist in Transit) and added Others for
        // the remaining relevant modes (D-BI4/5: mirrors Mission — full kW, hotel side).
        // Note DP's thrust arrives as the DpDemand row, so DP must NOT also produce a
        // Propulsion row (it would double-count the same kW).
        return (load, mode) switch
        {
            (BatteryLoadType.DpReserve, OperationalMode.DP) => (input.DpRedundancyRequirementKw ?? 0, null),
            (BatteryLoadType.DpDemand, OperationalMode.DP)
                => (OperationalModes.For(mode).PropulsionKw(input), null),
            (BatteryLoadType.Mission, OperationalMode.DP)
                => (0, input.MissionHeavyConsumerMaxKw ?? 0),
            (BatteryLoadType.Others, OperationalMode.Transit or OperationalMode.Port)
                => (0, input.OthersConsumerMaxKw ?? 0),
            (BatteryLoadType.Propulsion, OperationalMode.Transit or OperationalMode.Maneuvering)
                => (OperationalModes.For(mode).PropulsionKw(input), null),
            (BatteryLoadType.Hotel, _) => (OperationalModes.For(mode).HotelKw(input), null),
            _ => null
        };
    }
}
