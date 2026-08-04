using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Results;

/// <summary>
/// Builds the Level 2 block inside the client's <c>variant-detail-panel</c> — the generator setpoint
/// table shown for the Pro and Premium tiers.
///
/// Which tiers actually receive it is <see cref="TierResultBuilder"/>'s decision, not this one's.
/// </summary>
internal static class Level2DetailsBuilder
{
    internal static Level2Details Build(Level2Result result) => new()
    {
        OptimalSetpoints = result.OptimalSetpoints,
        OptimalTotalSfoc = result.OptimalTotalSfoc,
        SavingsTonPerHour = result.Level2SavingsTonPerHour
    };
}

/// <summary>
/// Builds the Level 3 block inside the client's <c>variant-detail-panel</c> — the DRC figures shown
/// for the Premium tier only.
///
/// <c>BatteryShavedVariationKw</c> is carried through so the field can audit the Q4 residual rule:
/// it says how much of the variation the battery already took, and therefore why DRC monetizes less.
/// </summary>
internal static class Level3DetailsBuilder
{
    internal static Level3Details Build(Level3Result result) => new()
    {
        DrcSavingsTonPerYear = result.DrcSavingsTonPerYear,
        VariationPerGeneratorKw = result.VariationPerGeneratorKw,
        ReducedVariationPerGeneratorKw = result.ReducedVariationPerGeneratorKw,
        BatteryShavedVariationKw = result.BatteryShavedVariationKw,
        ActiveGeneratorCount = result.ActiveGeneratorCount
    };
}
