using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Helpers;

/// <summary>
/// Synchronous SFOC interpolation for pre-fetched data.
/// Used by Level2 sweep loop to avoid async overhead.
/// </summary>
internal static class SfocInterpolationHelper
{
    internal const double DefaultSfocFallback = 220.0;

    /// <summary>
    /// Interpolate SFOC from pre-sorted working points.
    /// Data must be sorted by Load ascending.
    /// </summary>
    internal static double Interpolate(decimal loadPercentage, List<SfocDataPoint> sortedData)
    {
        if (sortedData == null || sortedData.Count == 0)
            return DefaultSfocFallback;

        if (loadPercentage <= (decimal)sortedData[0].Load)
        {
            // Extrapolate below minimum load point using first two data points
            // to reflect increased SFOC at very low engine loads (physical reality:
            // engines are less efficient at very low loads due to friction/standby losses)
            if (sortedData.Count >= 2 && loadPercentage < (decimal)sortedData[0].Load)
            {
                var p0 = sortedData[0];
                var p1 = sortedData[1];
                var loadRange = p1.Load - p0.Load;
                if (loadRange > 0)
                {
                    var sfocSlope = (p1.Sfoc - p0.Sfoc) / loadRange;
                    var loadOffset = (double)loadPercentage - p0.Load;
                    var extrapolated = p0.Sfoc + sfocSlope * loadOffset;
                    // SFOC below minimum data point should never be lower than at that point
                    return Math.Max(extrapolated, p0.Sfoc);
                }
            }
            return sortedData[0].Sfoc;
        }

        if (loadPercentage >= (decimal)sortedData[^1].Load)
            return sortedData[^1].Sfoc;

        for (int i = 0; i < sortedData.Count - 1; i++)
        {
            var lower = sortedData[i];
            var upper = sortedData[i + 1];

            if (loadPercentage >= (decimal)lower.Load && loadPercentage <= (decimal)upper.Load)
            {
                var loadRange = upper.Load - lower.Load;
                if (loadRange == 0) return lower.Sfoc;
                var sfocRange = upper.Sfoc - lower.Sfoc;
                var loadOffset = (double)loadPercentage - lower.Load;
                return lower.Sfoc + (sfocRange * loadOffset / loadRange);
            }
        }

        return sortedData[sortedData.Count / 2].Sfoc;
    }
}
