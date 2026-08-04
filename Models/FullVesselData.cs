namespace KSailCalc.Api.Models;

/// <summary>
/// Complete vessel data including configuration, operational profile, and engine details
/// Optimized response for single API call to get all vessel-related data
/// </summary>
public class FullVesselData
{
    public VesselType VesselConfig { get; set; } = new();
    public VesselOperationalProfile OperationalProfile { get; set; } = new();
    public EngineType? MainEngineData { get; set; }
    public AuxiliaryEngineType? AuxEngineData { get; set; }

    // Audit trace for parametric (category+size+speed) resolution; null on the legacy path
    public ResolutionInfo? Resolution { get; set; }
}

/// <summary>
/// How a parametric vessel request was resolved: which reference curves bracketed the
/// requested size, the interpolation factor, and which record supplied the profile.
/// </summary>
public class ResolutionInfo
{
    public decimal? LowerRefSize { get; set; }
    public decimal? UpperRefSize { get; set; }
    public decimal? T { get; set; }
    public string ProfileSource { get; set; } = string.Empty;
    public bool Clamped { get; set; }
}
