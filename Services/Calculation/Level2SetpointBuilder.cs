using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Helpers;

namespace KSailCalc.Api.Services.Calculation;

/// <summary>
/// Turns what the sweep found into the generator setpoints the client draws.
///
/// Separated from <see cref="Level2LoadSweep"/> because it answers a different question: not "which
/// split is cheapest" but "what does the answer look like on screen". It is also where the two
/// deliberate presentation rules live — the shaft generator is reported but never optimized, and an
/// engine Level 2 switched off still gets a row so the client can show it as OFF.
/// </summary>
internal static class Level2SetpointBuilder
{
    private const double PowerTolerance = PlantLimits.PowerToleranceKw;

    /// <summary>
    /// The shaft generator's setpoint, taken from Level 1 as-is. SG load is fixed by the main engine
    /// shaft, so Level 2 reports it but never redistributes it. Null when no SG is running.
    /// </summary>
    internal static GeneratorSetpoint? BuildFixedSgSetpoint(
        EngineCombination optimal, CalculatorInput input, EngineFuelCurves curves)
    {
        if (!optimal.SgEnabled || optimal.SgPowerKw <= PowerTolerance)
            return null;

        var sgCapacity = optimal.ActiveMeCount * input.SgCapacityPerEngine;
        var sgLoad = sgCapacity > 0 ? optimal.SgPowerKw / sgCapacity : 0;
        var sfoc = curves.Sfoc((decimal)sgLoad, EngineCategory.Main);

        return new GeneratorSetpoint
        {
            GeneratorType = GeneratorType.SG,
            CapacityKw = sgCapacity,
            LoadPercent = sgLoad,
            PowerKw = optimal.SgPowerKw,
            Sfoc = sfoc
        };
    }

    /// <summary>
    /// One row per engine Level 1 had running, in order, with the sweep's chosen load. An engine the
    /// sweep switched off keeps its row with zero power — the client renders it as OFF rather than
    /// omitting it.
    /// </summary>
    internal static List<GeneratorSetpoint> BuildSetpoints(
        GeneratorSetpoint? sgSetpoint, AeDistribution dist, int totalAeCount,
        double capacityKw, List<SfocDataPoint> sfocData)
    {
        var list = new List<GeneratorSetpoint>();
        if (sgSetpoint != null) list.Add(sgSetpoint);

        for (int i = 0; i < totalAeCount; i++)
        {
            var load = dist.LoadPercents[i];
            var power = load * capacityKw;
            var sfoc = power > PowerTolerance
                ? SfocInterpolationHelper.Interpolate((decimal)load, sfocData)
                : 0;

            list.Add(new GeneratorSetpoint
            {
                GeneratorType = GeneratorType.AE,
                CapacityKw = capacityKw,
                LoadPercent = load,
                PowerKw = power,
                Sfoc = sfoc
            });
        }

        return list;
    }

    /// <summary>
    /// Level 1's answer, unchanged: used when there is no aux demand to redistribute, or when the
    /// sweep found no valid split. Savings are zero by construction, never negative.
    /// </summary>
    internal static Level2Result BuildPassThroughResult(
        EngineCombination optimal, GeneratorSetpoint? sgSetpoint)
    {
        var setpoints = new List<GeneratorSetpoint>();
        if (sgSetpoint != null) setpoints.Add(sgSetpoint);

        return new Level2Result
        {
            OptimalSetpoints = setpoints,
            OptimalTotalSfoc = setpoints.Sum(s => s.Sfoc),
            Level2FocTonPerHour = optimal.FocTonPerHour,
            Level1FocTonPerHour = optimal.FocTonPerHour,
            Level2SavingsTonPerHour = 0
        };
    }
}
