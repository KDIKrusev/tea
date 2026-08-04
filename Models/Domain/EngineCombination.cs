namespace KSailCalc.Api.Models.Domain;

/// <summary>
/// One ME/SG/AE ON/OFF combination with its computed load and FOC.
///
/// **Immutable.** Level 1 builds a candidate in stages — counts → distributed load → PTI assist →
/// fuel consumption — and each stage returns a NEW instance via <c>with</c> rather than writing into
/// the previous one. That is deliberate: when these were settable, stepping through the candidate
/// loop meant watching a value change under you, and it was impossible to tell which stage had last
/// written a field.
///
/// A record, not a class, so the staged copies read as <c>candidate with { MePowerKw = … }</c>.
/// </summary>
public record EngineCombination
{
    public int ActiveMeCount { get; init; }
    public bool SgEnabled { get; init; }
    public int ActiveAeCount { get; init; }
    public double MePowerKw { get; init; }
    public double SgPowerKw { get; init; }
    public double AePowerKw { get; init; }
    public double MeLoadPercent { get; init; }
    public double AeLoadPercent { get; init; }
    public double FocTonPerHour { get; init; }
    public double MeFocTonPerHour { get; init; }
    public double AeFocTonPerHour { get; init; }

    /// <summary>PTI propulsion assist delivered by the shaft motor [kW] (0 when PTI unused).</summary>
    public double PtiPowerKw { get; init; }

    /// <summary>PTI headroom left for battery peak-shaving discharge [kW] (capacity − assist).</summary>
    public double AvailablePtiKw { get; init; }
}
