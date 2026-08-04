namespace KSailCalc.Api.Models.Domain;

/// <summary>
/// The price list for the iEMS product itself (Level 1, 2, 3) — what the customer pays to install
/// a tier, not the price of fuel. Maps to the IntegrationLevel table in the hybrid schema.
///
/// Only <see cref="IemsPriceNOK"/> and <see cref="CommissioningNOK"/> reach a calculation: together
/// they become <c>VariantResult.TotalInvestment</c>, and from it the payback period, the ROI and
/// therefore which tier the client marks as recommended.
///
/// The table also carries a <c>BaseEfficiencyFactor</c> column (0.97 / 0.955 / 0.94 — the old fixed
/// "3 % / 4.5 % / 6 % FOC reduction" model). It is deliberately NOT mapped here: the L1/L2/L3
/// optimization replaced it, and <c>VariantResult.EfficiencyFactor</c> — a computed
/// <c>optimizedFoc / baselineFoc</c> — lands in the same 0.94–0.98 range, so a mapped-but-unread
/// copy of the old constants was an invitation to mistake one for the other. The column is still in
/// the database if the figures are ever wanted again.
/// </summary>
public class IntegrationLevelConfig
{
    public int IntegrationLevelId { get; set; } // Database PK
    public string LevelName { get; set; } = string.Empty; // "Level 1", "Level 2", "Level 3"
    public double IemsPriceNOK { get; set; }
    public double CommissioningNOK { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
