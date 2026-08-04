using KSailCalc.Api.Models;
namespace KSailCalc.Api.Models.Domain;

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
