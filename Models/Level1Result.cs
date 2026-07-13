namespace KSailCalc.Api.Models;

/// <summary>
/// Result of Level 1 optimization: optimal and baseline engine combinations with all valid combinations evaluated.
/// </summary>
public class Level1Result
{
    public EngineCombination OptimalCombination { get; set; } = new();
    public EngineCombination BaselineCombination { get; set; } = new();
    public double OptimalFocTonPerHour { get; set; }
    public double BaselineFocTonPerHour { get; set; }
    public List<EngineCombination> AllValidCombinations { get; set; } = new();
    public int SelectedBaselineIndex { get; set; }
}
