namespace KSailCalc.Api.Models;

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
/// Sail calculation details returned to the client for UI display
/// </summary>
public class SailContributionResult
{
    /// <summary>True wind speed as entered by user (m/s)</summary>
    public double TrueWindSpeed { get; set; }

    /// <summary>Wind angle relative to vessel as entered by user (degrees)</summary>
    public double WindAngleRelVessel { get; set; }

    /// <summary>Calculated apparent wind speed (m/s)</summary>
    public double ApparentWindSpeed { get; set; }

    /// <summary>Calculated apparent wind angle (degrees 0-360)</summary>
    public double ApparentWindAngle { get; set; }

    /// <summary>Sail contribution force from lookup table (kN)</summary>
    public double SailForceKn { get; set; }

    /// <summary>Sail contribution converted to power (kW) = Force × VesselSpeed</summary>
    public double SailPowerKw { get; set; }

    /// <summary>Transit propulsion BEFORE sail contribution (kW)</summary>
    public double TransitPropulsionBeforeKw { get; set; }

    /// <summary>Transit propulsion AFTER sail contribution (kW)</summary>
    public double TransitPropulsionAfterKw { get; set; }

    /// <summary>Percentage of propulsion power saved by sails</summary>
    public double SailSavingsPercent { get; set; }
}

/// <summary>
/// Apparent wind calculation result (used by SailContributionService)
/// </summary>
public record ApparentWindResult(
    double ApparentWindSpeed,       // m/s
    double ApparentWindAngle        // degrees (0-360)
);