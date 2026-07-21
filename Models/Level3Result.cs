namespace KSailCalc.Api.Models;

/// <summary>
/// Result of Level 3 Dynamic Ramp Control savings calculation.
/// </summary>
public class Level3Result
{
    public double DrcSavingsTonPerYear { get; set; }
    public double WithoutDrcFocGramsPerCycle { get; set; }
    public double WithDrcFocGramsPerCycle { get; set; }
    public double VariationPerGeneratorKw { get; set; }
    public double ReducedVariationPerGeneratorKw { get; set; }

    /// <summary>Hotel/mission variation already shaved by the battery (excluded from DRC) [kW].</summary>
    public double BatteryShavedVariationKw { get; set; }

    public int ActiveGeneratorCount { get; set; }
    public double AnnualHours { get; set; }
}
