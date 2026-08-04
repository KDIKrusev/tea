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

    /// <summary>Installed ME capacity (kW) — MeCapacityPerEngine × MeCount</summary>
    public double MeInstalled { get; set; }

    /// <summary>Installed AE capacity (kW) — AeCapacityPerEngine × AeCount</summary>
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
