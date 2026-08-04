using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Catalog;

/// <summary>
/// Builds the category list the client's vessel picker offers: one entry per category with the
/// size and speed bounds the user may choose between.
///
/// The bounds are not simply the min and max of every record. Categories that carry
/// <c>ReferenceSize</c> anchors interpolate BETWEEN those anchors, so the anchors define the usable
/// range; categories without them fall back to the bucket bounds. An open-ended top bucket
/// (no <c>MaxSize</c>) reports a null upper bound rather than inventing one.
/// </summary>
internal static class VesselCategoryBuilder
{
    internal static List<VesselCategoryData> Build(IReadOnlyList<VesselType> vesselTypes, ILogger logger)
        => vesselTypes
            .Where(vt => !string.IsNullOrEmpty(vt.Category))
            .GroupBy(vt => vt.Category!)
            .Select(g =>
            {
                var units = g.Select(vt => vt.Unit).Distinct().ToList();
                if (units.Count > 1)
                    logger.LogWarning("Category {Category} has mixed units: {Units}",
                        g.Key, string.Join(", ", units));

                var refSizes = g.Where(vt => vt.ReferenceSize.HasValue)
                    .Select(vt => vt.ReferenceSize!.Value).ToList();
                var speeds = g.SelectMany(vt => vt.SpeedPowerCurve)
                    .Select(sp => sp.SpeedKnots).ToList();

                return new VesselCategoryData
                {
                    Name = g.Key,
                    Unit = units.FirstOrDefault() ?? string.Empty,
                    // Reference anchors define the interpolation range; bucket bounds are the fallback
                    SizeMin = refSizes.Count > 0 ? refSizes.Min() : g.Min(vt => vt.MinSize),
                    SizeMax = refSizes.Count > 0 ? refSizes.Max()
                        : (g.Any(vt => !vt.MaxSize.HasValue) ? null : g.Max(vt => vt.MaxSize)),
                    SpeedMin = speeds.Count > 0 ? speeds.Min() : 0,
                    SpeedMax = speeds.Count > 0 ? speeds.Max() : 0
                };
            })
            .OrderBy(c => c.Name)
            .ToList();
}
