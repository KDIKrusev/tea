namespace KSailCalc.Api.Models.Domain;

/// <summary>
/// Single data point in the sail contribution lookup table
/// </summary>
public class SailContributionItem
{
    public double ApparentWindAngle { get; set; }       // degrees (0-360)
    public double ApparentWindSpeed { get; set; }       // m/s
    public double SailContributionForce { get; set; }   // kN (positive = assists vessel)
}

/// <summary>
/// Sail contribution configuration containing the lookup table
/// </summary>
public class SailContributionServiceConfiguration
{
    public List<SailContributionItem> SailContributions { get; set; } = new();
}

/// <summary>
/// Root wrapper matching JSON structure:
/// { "SailContributionServiceConfiguration": { "SailContributions": [...] } }
/// </summary>
public class SailContributionConfigJson
{
    public SailContributionServiceConfiguration SailContributionServiceConfiguration { get; set; } = new();
}

/// <summary>
/// Apparent wind calculation result (used by SailContributionService)
/// </summary>
public record ApparentWindResult(
    double ApparentWindSpeed,       // m/s
    double ApparentWindAngle        // degrees (0-360)
);
