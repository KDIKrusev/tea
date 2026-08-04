namespace KSailCalc.Api.Models;

/// <summary>
/// Summary of Level 1 optimization included in the API response.
/// </summary>
public class Level1Details
{
    // Optimal configuration
    public int ActiveMeCount { get; set; }
    public bool SgEnabled { get; set; }
    public int ActiveAeCount { get; set; }
    public double OptimalFocTonPerHour { get; set; }

    // Baseline configuration
    public int BaselineMeCount { get; set; }
    public bool BaselineSgEnabled { get; set; }
    public int BaselineAeCount { get; set; }
    public double BaselineFocTonPerHour { get; set; }

    // Baseline power & load (weighted across modes)
    public double BaselineMePowerKw { get; set; }
    public double BaselineSgPowerKw { get; set; }
    public double BaselineAePowerKw { get; set; }
    public double BaselineMeLoadPercent { get; set; }
    public double BaselineAeLoadPercent { get; set; }

    public double SavingsTonPerHour { get; set; }
    public int ValidCombinationsCount { get; set; }
    public int SelectedBaselineIndex { get; set; }
    public List<ValidCombinationDto> ValidCombinations { get; set; } = new();

}
