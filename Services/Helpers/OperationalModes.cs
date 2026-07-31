using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Services.Helpers;

/// <summary>
/// What one operational mode IS: the hours it runs, the demands it places on the plant, and
/// whether the input activates it at all.
/// </summary>
/// <param name="Mode">The mode this row describes.</param>
/// <param name="Hours">Annual hours spent in the mode.</param>
/// <param name="PropulsionKw">Average propulsion demand (Transit applies the sea margin).</param>
/// <param name="HotelKw">Average hotel/mission demand.</param>
/// <param name="IsActive">
/// Whether the mode participates in the calculation — hours above zero, plus any mode-specific
/// switch (DP also requires <see cref="CalculatorInput.DpEnabled"/>).
/// </param>
public sealed record OperationalModeSpec(
    OperationalMode Mode,
    Func<CalculatorInput, double> Hours,
    Func<CalculatorInput, double> PropulsionKw,
    Func<CalculatorInput, double> HotelKw,
    Func<CalculatorInput, bool> IsActive);

/// <summary>
/// The single source of truth for the operational modes. Before this registry the same knowledge
/// lived in four places (the orchestrator's per-mode blocks and hours switch, Level 1's load
/// switch, the battery cascade's load matrix) and had already drifted apart.
///
/// Adding a mode is one row here plus its input fields — but note the two deliberate asymmetries
/// that are NOT covered by this list: Transit is the only mode that runs the full L1→L2→L3
/// pipeline (decision D4/Q5 — Level 2 has no Excel counterpart in other modes), and the battery
/// cascade decides per (load × mode) which rows exist at all.
/// </summary>
public static class OperationalModes
{
    /// <summary>Every mode, in the order results are reported to the client.</summary>
    public static readonly IReadOnlyList<OperationalModeSpec> All = new[]
    {
        new OperationalModeSpec(
            OperationalMode.Transit,
            input => input.TransitHours,
            input => input.EffectivePropulsionPower,
            input => input.TransitHotelPowerKW,
            input => input.TransitHours > 0),

        new OperationalModeSpec(
            OperationalMode.DP,
            input => input.DPHours ?? 0,
            input => input.RequiredDPPowerKW ?? 0,
            input => input.DPHotelPowerKW ?? 0,
            input => input.DpEnabled && (input.DPHours ?? 0) > 0),

        new OperationalModeSpec(
            OperationalMode.Port,
            input => input.PortHours,
            _ => 0,
            input => input.PortHotelPowerKW,
            input => input.PortHours > 0),

        new OperationalModeSpec(
            OperationalMode.Anchor,
            input => input.AnchorHours,
            _ => 0,
            input => input.AnchorHotelPowerKW,
            input => input.AnchorHours > 0),

        new OperationalModeSpec(
            OperationalMode.Maneuvering,
            input => input.ManeuveringHours,
            input => input.ManeuveringPropulsionPowerKW,
            input => input.ManeuveringHotelPowerKW,
            input => input.ManeuveringHours > 0)
    };

    /// <summary>
    /// Every mode except Transit. Transit is handled on its own because it carries the sail
    /// override, the user-pinned baseline and the L2/L3 stages.
    /// </summary>
    public static IEnumerable<OperationalModeSpec> ExceptTransit
        => All.Where(spec => spec.Mode != OperationalMode.Transit);

    public static OperationalModeSpec For(OperationalMode mode)
        => All.FirstOrDefault(spec => spec.Mode == mode)
           ?? throw new ArgumentException($"Unknown mode: {mode}", nameof(mode));

    public static double HoursOf(CalculatorInput input, OperationalMode mode) => For(mode).Hours(input);

    /// <summary>Average (propulsion, hotel) demand for the mode, before any battery adjustment.</summary>
    public static (double Propulsion, double Hotel) LoadsOf(CalculatorInput input, OperationalMode mode)
    {
        var spec = For(mode);
        return (spec.PropulsionKw(input), spec.HotelKw(input));
    }
}
