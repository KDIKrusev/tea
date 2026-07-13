using KSailCalc.Api.Models;
using KSailCalc.Api.Repositories.Interfaces;
using KSailCalc.Api.Services.Helpers;
using KSailCalc.Api.Services.Interfaces;

namespace KSailCalc.Api.Services;

/// <summary>
/// Resolves (category, size, speed) to a bucket record + interpolated calm water power.
/// Power: speed interpolation within each bracketing reference curve, then linear
/// blend on ReferenceSize. Categories without ReferenceSize anchors (Container, OSV)
/// resolve bucket-only: the bucket record's own curve is used directly.
/// Sizes outside the reference range clamp to the nearest reference curve.
/// </summary>
public class VesselResolutionService : IVesselResolutionService
{
    private readonly IKSailCalcConfigRepository _configRepository;
    private readonly ILogger<VesselResolutionService> _logger;

    public VesselResolutionService(
        IKSailCalcConfigRepository configRepository,
        ILogger<VesselResolutionService> logger)
    {
        _configRepository = configRepository;
        _logger = logger;
    }

    public async Task<VesselResolution?> ResolveAsync(string category, decimal size, decimal speed)
    {
        var categoryVessels = (await _configRepository.GetVesselTypesAsync())
            .Where(v => string.Equals(v.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (categoryVessels.Count == 0)
        {
            _logger.LogWarning("No active vessel records for category {Category}", category);
            return null;
        }

        // Bucket record supplies operational profile, engines and sea margin
        var bucket = categoryVessels.FirstOrDefault(v =>
                v.MinSize.HasValue && size >= v.MinSize.Value
                && (!v.MaxSize.HasValue || size <= v.MaxSize.Value))
            ?? categoryVessels.OrderBy(v => v.MinSize ?? 0).Last();

        // Curve anchors: only records with ReferenceSize participate in size interpolation
        var refs = categoryVessels
            .Where(v => v.ReferenceSize.HasValue)
            .OrderBy(v => v.ReferenceSize!.Value)
            .ToList();

        if (refs.Count == 0)
        {
            // Bucket-only category: serve the bucket record's own curve
            return new VesselResolution
            {
                BucketRecord = bucket,
                CalmWaterPowerKW = RoundPower(PowerInterpolationHelper.GetPowerAtSpeed(bucket, speed)),
                Info = new ResolutionInfo
                {
                    ProfileSource = bucket.VesselTypeName,
                    Clamped = false
                }
            };
        }

        var lower = refs.LastOrDefault(v => v.ReferenceSize!.Value <= size);
        var upper = refs.FirstOrDefault(v => v.ReferenceSize!.Value >= size);
        var clamped = lower == null || upper == null;

        var power = PowerInterpolationHelper.GetPowerAtSizeAndSpeed(lower, upper, size, speed);

        decimal? t = null;
        if (lower != null && upper != null && lower.ReferenceSize != upper.ReferenceSize)
            t = PowerInterpolationHelper.ComputeSizeFactor(lower, upper, size);

        return new VesselResolution
        {
            BucketRecord = bucket,
            CalmWaterPowerKW = RoundPower(power),
            Info = new ResolutionInfo
            {
                LowerRefSize = lower?.ReferenceSize,
                UpperRefSize = upper?.ReferenceSize,
                T = t,
                ProfileSource = bucket.VesselTypeName,
                Clamped = clamped
            }
        };
    }

    // Power is displayed directly in the UI — two decimals is plenty for kW figures
    private static decimal? RoundPower(decimal? power) =>
        power.HasValue ? Math.Round(power.Value, 2) : null;
}
