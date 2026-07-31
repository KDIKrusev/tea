namespace KSailCalc.Api.Services.Helpers;

/// <summary>
/// Plant-wide numeric limits shared by the optimization levels.
/// Level 2 already named these locally; Level 1 repeated the bare literals nine times, which is
/// how a limit ends up changed in one level and not the other.
/// </summary>
internal static class PlantLimits
{
    /// <summary>
    /// Slack (kW) for capacity comparisons. Loads are sums of products of user-entered numbers,
    /// so an exact ">" would reject configurations that fit to within a milliwatt.
    /// </summary>
    internal const double PowerToleranceKw = 0.001;

    /// <summary>
    /// Auxiliary engines are never dispatched above 90 % load: above it Level 2 has no room left
    /// to redistribute, so Level 1 must not offer such a combination in the first place.
    /// </summary>
    internal const double MaxAuxLoadFraction = 0.90;
}
