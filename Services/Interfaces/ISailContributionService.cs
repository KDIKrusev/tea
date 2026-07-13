using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Interfaces;

public interface ISailContributionService
{
    /// <summary>
    /// Full sail contribution calculation: apparent wind → lookup → power.
    /// Returns null if no sail data is available in the database.
    /// </summary>
    Task<SailContributionResult?> CalculateSailContributionAsync(
        double trueWindSpeed,
        double windAngleRelVessel,
        double vesselSpeedKnots,
        double transitPropulsionBeforeKw);
}