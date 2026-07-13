using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Interfaces;

/// <summary>
/// Resolves a parametric vessel request (category + size + speed) against the
/// reference VesselType records: bucket containment for profile/engines/sea margin,
/// ReferenceSize bracketing + 2D interpolation for calm water power.
/// </summary>
public interface IVesselResolutionService
{
    /// <summary>
    /// Returns null when the category has no active records.
    /// </summary>
    Task<VesselResolution?> ResolveAsync(string category, decimal size, decimal speed);
}

/// <summary>
/// Result of a parametric resolution: the record supplying profile/engine data,
/// the interpolated power, and the audit trace.
/// </summary>
public class VesselResolution
{
    public VesselType BucketRecord { get; set; } = new();
    public decimal? CalmWaterPowerKW { get; set; }
    public ResolutionInfo Info { get; set; } = new();
}
