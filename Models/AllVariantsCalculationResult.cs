namespace KSailCalc.Api.Models;

/// <summary>
/// Response containing calculation results for all variants.
/// Shared data (PowerDemands, Level1Details, baseline) is at the top level;
/// per-variant data lives in Advanced/Pro/Premium.
/// </summary>
public class AllVariantsCalculationResult
{
    // === Shared across all variants (displayed once) ===

    public PowerDemands PowerDemands { get; set; } = new();
    public Level1Details? Level1Details { get; set; }
    public SailContributionResult? SailContribution { get; set; }

    // Baseline metrics (same for all variants)
    public double BaselineFOC { get; set; } // tons/year
    public double BaselineCO2 { get; set; } // tons/year
    public double BaselineME { get; set; } // tons/year
    public double BaselineAE { get; set; } // tons/year

    // Validation warnings
    public List<ValidationWarning> Warnings { get; set; } = new();

    // === Per-variant results (only what differs) ===

    public VariantResult Premium { get; set; } = new();
    public VariantResult Pro { get; set; } = new();
    public VariantResult Advanced { get; set; } = new();
}
