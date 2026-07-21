using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Services.Interfaces;

/// <summary>
/// Allocates the battery power budget over one operational mode's load demands
/// (port of the Excel "Load Demands" sheet cascade).
/// </summary>
public interface IBatteryAllocationService
{
    /// <summary>
    /// Cascade the battery budget (input.Battery.PowerKw) over the mode's loads in priority order.
    /// When the battery is null/inactive or does not apply to the mode, returns an allocation with
    /// zero coverage where every load's variation is uncovered (spinning reserve on the gensets).
    /// </summary>
    /// <param name="budgetOverrideKw">
    /// Overrides the battery budget regardless of battery state — pass 0 for the no-battery
    /// reference scenario (full variation carried by the gensets, R3a dual-scenario rule).
    /// </param>
    /// <param name="propulsionOverrideKw">
    /// Replaces the Propulsion row's average load (e.g. sail-adjusted Transit propulsion),
    /// mirroring Level 1's overridePropulsionKw flow.
    /// </param>
    BatteryModeAllocation Allocate(OperationalMode mode, CalculatorInput input,
        double? budgetOverrideKw = null, double? propulsionOverrideKw = null);
}
