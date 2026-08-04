using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Services.Results;

/// <summary>
/// Builds the power/energy panel the client displays once per calculation: hours-weighted average
/// power per plant side, average load percentages, and the per-mode breakdown.
///
/// Pure function over the per-mode results — no configuration, no I/O, no dependency on the
/// orchestrator.
/// </summary>
internal static class PowerDemandsBuilder
{
    internal static PowerDemands Build(
        List<ModePipelineResult> modes,
        CalculatorInput input, SailContributionResult? sailResult)
    {
        var totalHours = modes.Sum(m => m.Hours);

        var (mePower, sgPower, aePower) = CalculateWeightedPower(modes, totalHours);
        var (meAvgLoad, aeAvgLoad) = CalculateWeightedLoadPercent(modes);
        var modeBreakdowns = BuildModeBreakdowns(modes, input.DpEnabled, sailResult);

        return new PowerDemands
        {
            MainEnginePowerKw = mePower,
            ShaftGeneratorPowerKw = sgPower,
            AuxiliaryEnginePowerKw = aePower,
            TotalPowerKw = mePower + sgPower + aePower,
            TotalEnergyKwh = (mePower + sgPower + aePower) * totalHours,
            MeInstalled = input.TotalMeCapacity,
            AeInstalled = input.TotalAeCapacity,
            MeAverageLoadPercent = meAvgLoad,
            AeAverageLoadPercent = aeAvgLoad,
            ModeBreakdowns = modeBreakdowns
        };
    }

    private static (double me, double sg, double ae) CalculateWeightedPower(
        List<ModePipelineResult> modes, double totalHours)
    {
        if (totalHours <= 0) return (0, 0, 0);

        double me = modes.Sum(m => m.L1.OptimalCombination.MePowerKw * m.Hours);
        double sg = modes.Sum(m => m.L1.OptimalCombination.SgPowerKw * m.Hours);
        double ae = modes.Sum(m => m.L1.OptimalCombination.AePowerKw * m.Hours);

        return (me / totalHours, sg / totalHours, ae / totalHours);
    }

    /// <summary>
    /// Compute average load % weighted only by the hours each engine type is active.
    /// This avoids diluting load across the entire operation period when an engine
    /// only runs in certain modes (e.g. AE only in DP).
    /// </summary>
    private static (double meLoadPct, double aeLoadPct) CalculateWeightedLoadPercent(
        List<ModePipelineResult> modes)
    {
        double meNum = 0, meDen = 0;
        foreach (var m in modes)
        {
            if (m.L1.OptimalCombination.ActiveMeCount > 0 && m.Hours > 0)
            {
                meNum += m.L1.OptimalCombination.MeLoadPercent * m.Hours;
                meDen += m.Hours;
            }
        }

        double aeNum = 0, aeDen = 0;
        foreach (var m in modes)
        {
            if (m.L1.OptimalCombination.ActiveAeCount > 0 && m.Hours > 0)
            {
                aeNum += m.L1.OptimalCombination.AeLoadPercent * m.Hours;
                aeDen += m.Hours;
            }
        }

        return (
            meDen > 0 ? (meNum / meDen) * 100 : 0,
            aeDen > 0 ? (aeNum / aeDen) * 100 : 0);
    }

    private static List<ModePowerBreakdown> BuildModeBreakdowns(
        List<ModePipelineResult> modes, bool dpEnabled, SailContributionResult? sailResult)
    {
        var breakdowns = new List<ModePowerBreakdown>();

        foreach (var m in modes)
        {
            var sailPower = m.Mode == OperationalMode.Transit ? (sailResult?.SailPowerKw ?? 0) : 0;
            if (m.Hours > 0)
                breakdowns.Add(BuildModeBreakdown(m.L1.OptimalCombination, m.Mode, m.Hours, sailPower));
            else if (m.Mode == OperationalMode.DP && dpEnabled)
                breakdowns.Add(new ModePowerBreakdown { Mode = OperationalMode.DP });
        }

        return breakdowns;
    }

    private static ModePowerBreakdown BuildModeBreakdown(
        EngineCombination combo, OperationalMode mode, double hours, double sailPowerKw) => new()
        {
            Mode = mode,
            Hours = hours,
            PropulsionMainEngineKw = combo.MePowerKw - combo.SgPowerKw,
            PropulsionSailKw = sailPowerKw,
            HotelShaftGenKw = combo.SgPowerKw,
            HotelAuxGenKw = combo.AePowerKw,
            EnergyMainEngineKwh = combo.MePowerKw * hours,
            EnergyAuxGenKwh = combo.AePowerKw * hours
        };
}
