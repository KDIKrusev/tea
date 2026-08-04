using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Results;

/// <summary>
/// Builds the client's <c>baseline-panel</c> — third from the top of the results page.
///
/// That panel shows the optimum, the baseline it is measured against, and the clickable table of
/// every valid combination the user can pin as "how we operate today".
///
/// Two conversions live here and nowhere else: load fractions become percentages (×100), and a
/// combination that used no PTI reports <c>null</c> rather than 0 so the client can leave the cell
/// empty.
/// </summary>
internal static class Level1DetailsBuilder
{
    internal static Level1Details Build(Level1Result result) => new()
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
