namespace KSailCalc.Api.Services.Results;

/// <summary>
/// Annual fuel oil consumption [tons], split baseline vs optimal and main vs auxiliary engines.
/// Every figure is an hours-weighted sum over the modes that ran.
/// </summary>
internal sealed record FocBreakdown(
    double BaselineFoc, double BaselineMeFoc, double BaselineAeFoc,
    double OptimalMeFoc, double OptimalAeFoc);

/// <summary>
/// Annual savings [tons] per optimization level — the three independent numbers, nothing derived.
/// The product tiers are cumulative (Advanced = L1, Pro = L1+L2, Premium = L1+L2+L3), so a tier's
/// total is computed on demand by <see cref="TotalUpTo"/> rather than stored alongside its own inputs.
/// </summary>
internal sealed record SavingsBreakdown(double L1, double L2, double L3)
{
    /// <summary>
    /// Total savings for a tier that includes every optimization level up to
    /// <paramref name="highestLevel"/>.
    /// </summary>
    internal double TotalUpTo(int highestLevel) => highestLevel switch
    {
        1 => L1,
        2 => L1 + L2,
        _ => L1 + L2 + L3
    };
}

/// <summary>
/// Aggregates the per-mode Level 1/2/3 results into the annual figures the response is built from.
/// Pure function over the per-mode results.
/// </summary>
internal static class SavingsAggregator
{
    internal static FocBreakdown CalculateFocBreakdown(List<ModePipelineResult> modes)
    {
        return new FocBreakdown(
            BaselineFoc: modes.Sum(m => m.L1.BaselineFocTonPerHour * m.Hours),
            BaselineMeFoc: modes.Sum(m => m.L1.BaselineCombination.MeFocTonPerHour * m.Hours),
            BaselineAeFoc: modes.Sum(m => m.L1.BaselineCombination.AeFocTonPerHour * m.Hours),
            OptimalMeFoc: modes.Sum(m => m.L1.OptimalCombination.MeFocTonPerHour * m.Hours),
            OptimalAeFoc: modes.Sum(m => m.L1.OptimalCombination.AeFocTonPerHour * m.Hours)
        );
    }

    internal static SavingsBreakdown CalculateSavings(List<ModePipelineResult> modes)
    {
        var l1 = modes.Sum(m => (m.L1.BaselineFocTonPerHour - m.L1.OptimalFocTonPerHour) * m.Hours);
        var l2 = modes.Sum(m => m.L2.Level2SavingsTonPerHour * m.Hours);
        var l3 = modes.Sum(m => m.L3.DrcSavingsTonPerYear);

        return new SavingsBreakdown(l1, l2, l3);
    }
}
