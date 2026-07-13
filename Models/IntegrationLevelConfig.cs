namespace KSailCalc.Api.Models;

/// <summary>
/// Configuration for iEMS integration levels (Level 1, 2, 3)
/// Maps to IntegrationLevel table in hybrid schema
/// </summary>
public class IntegrationLevelConfig
{
    public int IntegrationLevelId { get; set; } // Database PK
    public string LevelName { get; set; } = string.Empty; // "Level 1", "Level 2", "Level 3"
    /// <summary>Legacy field — not used by L1/L2/L3 optimization pipeline. Retained for DB schema compatibility.</summary>
    public double BaseEfficiencyFactor { get; set; }
    public double IemsPriceNOK { get; set; }
    public double CommissioningNOK { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
