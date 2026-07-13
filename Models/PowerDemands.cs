using System.Text.Json.Serialization;
using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Models;

/// <summary>
/// Power demands for all engines
/// </summary>
public class PowerDemands
{
    public double MainEnginePowerKw { get; set; }
    public double ShaftGeneratorPowerKw { get; set; }
    public double AuxiliaryEnginePowerKw { get; set; }
    public double TotalPowerKw { get; set; }
    [JsonIgnore] public double TotalEnergyKwh { get; set; }

    /// <summary>Total power demand (alias for TotalPowerKw, used by frontend)</summary>
    public double TotalDemand => TotalPowerKw;

    /// <summary>Installed ME capacity (kW), populated from EngineCapacities</summary>
    public double MeInstalled { get; set; }

    /// <summary>Installed AE capacity (kW), populated from EngineCapacities</summary>
    public double AeInstalled { get; set; }

    /// <summary>ME average load % weighted by individual operating hours (0–100)</summary>
    [JsonIgnore] public double MeAverageLoadPercent { get; set; }

    /// <summary>AE average load % weighted by individual operating hours (0–100)</summary>
    [JsonIgnore] public double AeAverageLoadPercent { get; set; }

    /// <summary>
    /// Detailed breakdown by operational mode (Transit, DP, Anchor, Port, Maneuvering)
    /// </summary>
    public List<ModePowerBreakdown>? ModeBreakdowns { get; set; }
}

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