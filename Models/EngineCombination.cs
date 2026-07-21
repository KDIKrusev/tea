namespace KSailCalc.Api.Models;

/// <summary>
/// Represents one ME/SG/AE ON/OFF combination with its computed load and FOC.
/// </summary>
public class EngineCombination
{
    public int ActiveMeCount { get; set; }
    public bool SgEnabled { get; set; }
    public int ActiveAeCount { get; set; }
    public double MePowerKw { get; set; }
    public double SgPowerKw { get; set; }
    public double AePowerKw { get; set; }
    public double MeLoadPercent { get; set; }
    public double AeLoadPercent { get; set; }
    public double FocTonPerHour { get; set; }
    public double MeFocTonPerHour { get; set; }
    public double AeFocTonPerHour { get; set; }

    /// <summary>PTI propulsion assist delivered by the shaft motor [kW] (0 when PTI unused).</summary>
    public double PtiPowerKw { get; set; }

    /// <summary>PTI headroom left for battery peak-shaving discharge [kW] (capacity − assist).</summary>
    public double AvailablePtiKw { get; set; }
}
