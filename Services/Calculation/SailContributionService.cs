using KSailCalc.Api.Models;
using KSailCalc.Api.Repositories.Interfaces;
using KSailCalc.Api.Services.Interfaces;

namespace KSailCalc.Api.Services.Calculation;

/// <summary>
/// Sail contribution business logic: apparent wind calculation, force lookup, and power savings.
/// Data access delegated to ISailContributionRepository.
/// </summary>
public class SailContributionService : ISailContributionService
{
    private readonly ISailContributionRepository _repository;

    private const double KnotsToMs = 0.514444;

    public SailContributionService(ISailContributionRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public ApparentWindResult CalculateApparentWind(
        double trueWindSpeed,
        double windAngleRelVessel,
        double vesselSpeedKnots)
    {
        var sogMs = vesselSpeedKnots * KnotsToMs;
        var angleRad = windAngleRelVessel * Math.PI / 180.0;

        // Apparent Speed (VEA convention: minus SOG)
        var speedX = trueWindSpeed * Math.Cos(angleRad) - sogMs;
        var speedY = trueWindSpeed * Math.Sin(angleRad);
        var apparentWindSpeed = Math.Sqrt(Math.Pow(speedX, 2) + Math.Pow(speedY, 2));

        // Apparent Direction (VEA convention: plus SOG)
        var dirX = trueWindSpeed * Math.Cos(angleRad) + sogMs;
        var dirY = trueWindSpeed * Math.Sin(angleRad);
        var apparentWindAngle = NormalizeDegrees(
            Math.Atan2(dirY, dirX) * 180.0 / Math.PI);

        return new ApparentWindResult(apparentWindSpeed, apparentWindAngle);
    }

    /// <inheritdoc />
    public async Task<SailContributionResult?> CalculateSailContributionAsync(
        double trueWindSpeed,
        double windAngleRelVessel,
        double vesselSpeedKnots,
        double transitPropulsionBeforeKw)
    {
        var apparentWind = CalculateApparentWind(trueWindSpeed, windAngleRelVessel, vesselSpeedKnots);

        var sailItems = await _repository.GetSailContributionItemsAsync();
        if (sailItems == null || sailItems.Count == 0)
            return null;

        var sailForceKn = GetClosestSailContributionForce(
            sailItems, apparentWind.ApparentWindAngle, apparentWind.ApparentWindSpeed);

        var vesselSpeedMs = vesselSpeedKnots * KnotsToMs;
        var sailPowerKw = sailForceKn * vesselSpeedMs;

        var transitPropulsionAfterKw = Math.Max(0, transitPropulsionBeforeKw - sailPowerKw);

        var savingsPercent = transitPropulsionBeforeKw > 0
            ? ((transitPropulsionBeforeKw - transitPropulsionAfterKw) / transitPropulsionBeforeKw) * 100
            : 0;

        return new SailContributionResult
        {
            TrueWindSpeed = trueWindSpeed,
            WindAngleRelVessel = windAngleRelVessel,
            ApparentWindSpeed = apparentWind.ApparentWindSpeed,
            ApparentWindAngle = apparentWind.ApparentWindAngle,
            SailForceKn = sailForceKn,
            SailPowerKw = sailPowerKw,
            TransitPropulsionBeforeKw = transitPropulsionBeforeKw,
            TransitPropulsionAfterKw = transitPropulsionAfterKw,
            SailSavingsPercent = savingsPercent
        };
    }

    /// <summary>
    /// Find the closest sail contribution force using nearest-neighbor lookup.
    /// Speed is clamped to the table range to avoid out-of-range matches.
    /// </summary>
    public double GetClosestSailContributionForce(
        List<SailContributionItem> items,
        double apparentWindAngle,
        double apparentWindSpeed)
    {
        var speeds = items.Select(i => i.ApparentWindSpeed).Distinct().OrderBy(s => s).ToList();
        var clampedSpeed = Math.Clamp(apparentWindSpeed, speeds.First(), speeds.Last());

        var closest = items
            .OrderBy(item =>
                Math.Abs(item.ApparentWindAngle - apparentWindAngle) +
                Math.Abs(item.ApparentWindSpeed - clampedSpeed))
            .First();

        return closest.SailContributionForce;
    }

    private static double NormalizeDegrees(double degrees)
    {
        degrees %= 360.0;
        if (degrees < 0) degrees += 360.0;
        return degrees;
    }
}