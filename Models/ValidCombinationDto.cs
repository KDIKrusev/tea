namespace KSailCalc.Api.Models;

/// <summary>
/// Compact DTO for a single valid engine combination, sent to the client for baseline selection.
/// </summary>
public class ValidCombinationDto
{
    public int Index { get; set; }
    public int ActiveMeCount { get; set; }
    public bool SgEnabled { get; set; }
    public int ActiveAeCount { get; set; }
    public double FocTonPerHour { get; set; }
    public double MeLoadPercent { get; set; }
    public double AeLoadPercent { get; set; }

    /// <summary>PTI propulsion assist for this combination [kW]; null when no PTI used.</summary>
    public double? PtiKw { get; set; }
}
