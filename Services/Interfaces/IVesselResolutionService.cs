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
