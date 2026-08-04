namespace KSailCalc.Api.Models;

/// <summary>
/// Summary of Level 3 DRC savings included in the API response.
/// </summary>
public class Level3Details
{
    public double DrcSavingsTonPerYear { get; set; }
    public double VariationPerGeneratorKw { get; set; }
    public double ReducedVariationPerGeneratorKw { get; set; }

    /// <summary>Hotel/mission variation already shaved by the battery (excluded from DRC) [kW].</summary>
    public double BatteryShavedVariationKw { get; set; }

    public int ActiveGeneratorCount { get; set; }

}
