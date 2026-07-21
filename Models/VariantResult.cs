namespace KSailCalc.Api.Models;

/// <summary>
/// Per-variant calculation result containing only what differs between integration levels.
/// Shared data (PowerDemands, Level1Details, baseline metrics) lives on AllVariantsCalculationResult.
/// </summary>
public class VariantResult
{
    // Savings
    public double FuelSavings { get; set; } // tons/year
    public double FuelSavingsPercentage { get; set; }
    public double OptimizedFOC { get; set; } // tons/year
    public double OptimizedCO2 { get; set; } // tons/year
    public double Co2Reduction { get; set; } // tons/year
    public double Co2ReductionPercentage { get; set; }

    // Financial
    public double AnnualCostSavings { get; set; }
    public double TotalInvestment { get; set; }
    public double PaybackPeriod { get; set; } // years
    public double Roi { get; set; } // percentage — 10-year simple ROI

    // Efficiency
    public double EfficiencyFactor { get; set; } // 0-1

    // Optimized fuel breakdown (baseline is shared)
    public double OptimizedME { get; set; }
    public double OptimizedAE { get; set; }

    /// <summary>Optimized ME CO2 (tons/yr) using the main fuel's factor — do NOT recompute client-side.</summary>
    public double OptimizedMeCO2 { get; set; }

    /// <summary>Optimized AE CO2 (tons/yr) using the aux fuel's factor — do NOT recompute client-side.</summary>
    public double OptimizedAeCO2 { get; set; }

    // Load % for this variant
    public double MainEngineLoadPercent { get; set; }
    public double AuxiliaryEngineLoadPercent { get; set; }

    // Per-level annual savings breakdown (ton/yr)
    public double Level1SavingsTonPerYear { get; set; }
    public double Level2SavingsTonPerYear { get; set; }
    public double Level3SavingsTonPerYear { get; set; }

    // Level-specific optimization details (nullable)
    public Level2Details? Level2Details { get; set; }
    public Level3Details? Level3Details { get; set; }

    // Validation warnings
    public List<ValidationWarning> Warnings { get; set; } = new();
}
