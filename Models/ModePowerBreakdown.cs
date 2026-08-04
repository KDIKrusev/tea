using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Models;

/// <summary>
/// Power and energy breakdown for a single operational mode
/// </summary>
public class ModePowerBreakdown
{
    /// <summary>
    /// Operational mode: Transit, DP, Anchor, Port, Maneuvering
    /// </summary>
    public OperationalMode Mode { get; set; }

    /// <summary>
    /// Hours per year for this mode
    /// </summary>
    public double Hours { get; set; }

    // === Propulsion Power Breakdown (kW) ===

    /// <summary>
    /// Main engine power used for propulsion (excluding shaft generator)
    /// </summary>
    public double PropulsionMainEngineKw { get; set; }

    /// <summary>
    /// Sail contribution to propulsion (Transit mode only, 0 for others)
    /// </summary>
    public double PropulsionSailKw { get; set; }

    // === Hotel/Mission Power Breakdown (kW) ===

    /// <summary>
    /// Hotel load covered by shaft generator
    /// </summary>
    public double HotelShaftGenKw { get; set; }

    /// <summary>
    /// Hotel load covered by auxiliary generators
    /// </summary>
    public double HotelAuxGenKw { get; set; }

    // === Annual Energy Breakdown (kWh/year) ===

    /// <summary>
    /// Main engine energy consumption for this mode (Power × Hours)
    /// </summary>
    public double EnergyMainEngineKwh { get; set; }

    /// <summary>
    /// Auxiliary generator energy consumption for this mode (Power × Hours)
    /// </summary>
    public double EnergyAuxGenKwh { get; set; }
}