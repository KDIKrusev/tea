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

    public static Level1Details FromResult(Level1Result result) => new()
    {
        ActiveMeCount = result.OptimalCombination.ActiveMeCount,
        SgEnabled = result.OptimalCombination.SgEnabled,
        ActiveAeCount = result.OptimalCombination.ActiveAeCount,
        OptimalFocTonPerHour = result.OptimalFocTonPerHour,

        BaselineMeCount = result.BaselineCombination.ActiveMeCount,
        BaselineSgEnabled = result.BaselineCombination.SgEnabled,
        BaselineAeCount = result.BaselineCombination.ActiveAeCount,
        BaselineFocTonPerHour = result.BaselineFocTonPerHour,

        BaselineMePowerKw = result.BaselineCombination.MePowerKw,
        BaselineSgPowerKw = result.BaselineCombination.SgPowerKw,
        BaselineAePowerKw = result.BaselineCombination.AePowerKw,
        BaselineMeLoadPercent = result.BaselineCombination.MeLoadPercent * 100,
        BaselineAeLoadPercent = result.BaselineCombination.AeLoadPercent * 100,

        SavingsTonPerHour = result.BaselineFocTonPerHour - result.OptimalFocTonPerHour,
        ValidCombinationsCount = result.AllValidCombinations.Count,
        SelectedBaselineIndex = result.SelectedBaselineIndex,
        ValidCombinations = result.AllValidCombinations.Select((c, i) => new ValidCombinationDto
        {
            Index = i,
            ActiveMeCount = c.ActiveMeCount,
            SgEnabled = c.SgEnabled,
            ActiveAeCount = c.ActiveAeCount,
            FocTonPerHour = c.FocTonPerHour,
            MeLoadPercent = c.MeLoadPercent * 100,
            AeLoadPercent = c.AeLoadPercent * 100,
            PtiKw = c.PtiPowerKw > 0 ? c.PtiPowerKw : null
        }).ToList()
    };
}

/// <summary>
/// Compact DTO for a single valid engine combination, sent to the client for baseline selection.
/// </summary>
public class ValidCombinationDto
{
    public int Index { get; set; }
    public int ActiveMeCount { get; set; }
    public bool SgEnabled { get; set; }
    public int ActiveAeCount { get; set; }
    public double FocTonPerHour { get; set; }
    public double MeLoadPercent { get; set; }
    public double AeLoadPercent { get; set; }

    /// <summary>PTI propulsion assist for this combination [kW]; null when no PTI used.</summary>
    public double? PtiKw { get; set; }
}

/// <summary>
/// Summary of Level 2 optimization included in the API response.
/// </summary>
public class Level2Details
{
    public List<GeneratorSetpoint> OptimalSetpoints { get; set; } = new();
    public double OptimalTotalSfoc { get; set; }
    public double SavingsTonPerHour { get; set; }

    public static Level2Details FromResult(Level2Result result) => new()
    {
        OptimalSetpoints = result.OptimalSetpoints,
        OptimalTotalSfoc = result.OptimalTotalSfoc,
        SavingsTonPerHour = result.Level2SavingsTonPerHour
    };
}

/// <summary>
/// Summary of Level 3 DRC savings included in the API response.
/// </summary>
public class Level3Details
{
    public double DrcSavingsTonPerYear { get; set; }
    public double VariationPerGeneratorKw { get; set; }
    public double ReducedVariationPerGeneratorKw { get; set; }

    /// <summary>Hotel/mission variation already shaved by the battery (excluded from DRC) [kW].</summary>
    public double BatteryShavedVariationKw { get; set; }

    public int ActiveGeneratorCount { get; set; }

    public static Level3Details FromResult(Level3Result result) => new()
    {
        DrcSavingsTonPerYear = result.DrcSavingsTonPerYear,
        VariationPerGeneratorKw = result.VariationPerGeneratorKw,
        ReducedVariationPerGeneratorKw = result.ReducedVariationPerGeneratorKw,
        BatteryShavedVariationKw = result.BatteryShavedVariationKw,
        ActiveGeneratorCount = result.ActiveGeneratorCount
    };
}