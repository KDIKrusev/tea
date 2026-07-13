using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Helpers;

/// <summary>
/// Linear interpolation helpers for vessel speed/power curves.
/// Moved from VesselTypeExtensions to keep Models as pure data containers.
/// </summary>
internal static class PowerInterpolationHelper
{
    /// <summary>
    /// Get power at specific speed, with linear interpolation if exact speed not in curve.
    /// </summary>
    public static decimal? GetPowerAtSpeed(VesselType vessel, decimal targetSpeed)
    {
        if (vessel.SpeedPowerCurve == null || !vessel.SpeedPowerCurve.Any())
            return null;

        var exactMatch = vessel.SpeedPowerCurve.FirstOrDefault(sp => sp.SpeedKnots == targetSpeed);
        if (exactMatch != null)
            return exactMatch.CalmWaterPowerKW;

        var orderedCurve = vessel.SpeedPowerCurve.OrderBy(sp => sp.SpeedKnots).ToList();

        var lowerPoint = orderedCurve.LastOrDefault(sp => sp.SpeedKnots < targetSpeed);
        var upperPoint = orderedCurve.FirstOrDefault(sp => sp.SpeedKnots > targetSpeed);

        if (lowerPoint == null)
            return orderedCurve.First().CalmWaterPowerKW;
        if (upperPoint == null)
            return orderedCurve.Last().CalmWaterPowerKW;

        var speedRange = upperPoint.SpeedKnots - lowerPoint.SpeedKnots;
        var powerRange = upperPoint.CalmWaterPowerKW - lowerPoint.CalmWaterPowerKW;
        var speedOffset = targetSpeed - lowerPoint.SpeedKnots;

        return lowerPoint.CalmWaterPowerKW + (powerRange * speedOffset / speedRange);
    }

    /// <summary>
    /// Get power at a specific size and speed by interpolating between two reference vessels:
    /// speed interpolation within each vessel's curve, then linear blend on ReferenceSize.
    /// Pass null lower/upper to clamp to the available end of the reference range.
    /// </summary>
    public static decimal? GetPowerAtSizeAndSpeed(
        VesselType? lower, VesselType? upper, decimal targetSize, decimal targetSpeed)
    {
        if (lower == null && upper == null)
            return null;
        if (lower == null)
            return GetPowerAtSpeed(upper!, targetSpeed);
        if (upper == null || lower == upper
            || lower.ReferenceSize == upper.ReferenceSize)
            return GetPowerAtSpeed(lower, targetSpeed);

        var powerLower = GetPowerAtSpeed(lower, targetSpeed);
        var powerUpper = GetPowerAtSpeed(upper, targetSpeed);
        if (powerLower == null || powerUpper == null)
            return powerLower ?? powerUpper;

        var t = ComputeSizeFactor(lower, upper, targetSize);
        if (t == null)
            return powerLower;

        return powerLower + t.Value * (powerUpper - powerLower);
    }

    /// <summary>
    /// Linear position of targetSize between the two vessels' ReferenceSize anchors (0..1).
    /// </summary>
    public static decimal? ComputeSizeFactor(VesselType lower, VesselType upper, decimal targetSize)
    {
        if (!lower.ReferenceSize.HasValue || !upper.ReferenceSize.HasValue)
            return null;

        var sizeRange = upper.ReferenceSize.Value - lower.ReferenceSize.Value;
        if (sizeRange == 0)
            return null;

        return (targetSize - lower.ReferenceSize.Value) / sizeRange;
    }
}
