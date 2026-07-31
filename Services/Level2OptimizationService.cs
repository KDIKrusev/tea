using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Helpers;
using KSailCalc.Api.Services.Interfaces;

namespace KSailCalc.Api.Services;

/// <summary>
/// Level 2: finds the optimal AE load distribution to minimize fuel consumption.
/// SG load is fixed from Level 1 (driven by ME shaft — not adjustable).
///
/// Algorithm:
///   For each possible number of active AE (N, N-1, ..., 1):
///     Sweep all load combinations in 2% steps.
///     Last engine absorbs the remaining demand.
///     Track the combination with lowest total FOC.
///
/// Example: 2 AE × 2000 kW, AE demand = 2750 kW
///   AE₁ = 58% (1160 kW) → AE₂ = 1590/2000 = 79.5% → FOC = 0.538
///   AE₁ = 60% (1200 kW) → AE₂ = 1550/2000 = 77.5% → FOC = 0.536
///   AE₁ = 68% (1360 kW) → AE₂ = 1390/2000 = 69.5% → FOC = 0.534 ← best
///   AE₁ = 70% (1400 kW) → AE₂ = 1350/2000 = 67.5% → FOC = 0.534
///   ...
///   Also tries 1 AE: 2750/2000 = 137.5% → SKIP (exceeds 80%)
///   Result: AE₁ = 68%, AE₂ = 69.5%
/// </summary>
public class Level2OptimizationService : ILevel2OptimizationService
{
    private readonly ISfocService _sfocService;

    private const double MinLoad = 0.10;
    private const double MaxLoad = PlantLimits.MaxAuxLoadFraction;
    private const double LoadStep = 0.02;
    private const double PowerTolerance = PlantLimits.PowerToleranceKw;

    public Level2OptimizationService(ISfocService sfocService)
    {
        _sfocService = sfocService;
    }

    public async Task<Level2Result> OptimizeLoadSetpointsAsync(
        Level1Result level1Result, CalculatorInput input)
    {
        var optimal = level1Result.OptimalCombination;
        var sgSetpoint = await BuildFixedSgSetpointAsync(optimal, input);

        var totalAeCount = optimal.ActiveAeCount;
        var demandKw = optimal.AePowerKw;

        if (totalAeCount == 0 || demandKw < PowerTolerance)
            return BuildPassThroughResult(optimal, sgSetpoint);

        var sfocData = await GetFilteredAeSfocDataAsync(input);
        var best = SweepAllDistributions(totalAeCount, input.AeCapacityPerEngine, demandKw, sfocData);

        if (best is null)
            return BuildPassThroughResult(optimal, sgSetpoint);

        var savingsPerHour = Math.Max(0, optimal.AeFocTonPerHour - best.TotalFoc);
        var setpoints = BuildSetpoints(sgSetpoint, best, totalAeCount, input.AeCapacityPerEngine, sfocData);

        return new Level2Result
        {
            OptimalSetpoints = setpoints,
            OptimalTotalSfoc = setpoints.Sum(s => s.Sfoc),
            Level1FocTonPerHour = optimal.FocTonPerHour,
            Level2FocTonPerHour = optimal.MeFocTonPerHour + best.TotalFoc,
            Level2SavingsTonPerHour = savingsPerHour
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SWEEP: test all valid load distributions
    // ═══════════════════════════════════════════════════════════════════════════

    private record AeDistribution(double[] LoadPercents, double TotalFoc, int ActiveCount);

    /// <summary>
    /// Tries each possible number of active AE (N, N-1, ..., 1).
    /// For each count, generates all valid load combinations, evaluates FOC, picks the best.
    /// </summary>
    private static AeDistribution? SweepAllDistributions(
        int totalAeCount, double capacityKw, double demandKw, List<SfocDataPoint> sfocData)
    {
        AeDistribution? best = null;

        for (int activeCount = totalAeCount; activeCount >= 1; activeCount--)
        {
            // PHASE 1: generate all valid load combinations for this active count
            var candidates = GenerateCandidates(activeCount, capacityKw, demandKw);

            // PHASE 2: evaluate FOC for each, pick the best
            var candidate = FindLowestFoc(candidates, capacityKw, sfocData, totalAeCount, activeCount);
             
            if (candidate != null && (best is null || candidate.TotalFoc < best.TotalFoc))
                best = candidate;
        }

        return best;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PHASE 1: Generate all valid load distributions (works for any N)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Generates all valid load distributions for a given number of active AE.
    /// Works for 1, 2, 3, 4+ engines — one unified method.
    ///
    /// Approach: fill engines left to right.
    ///   - Each engine (except last) sweeps from 10% to 80% in 2% steps.
    ///   - Last engine absorbs the remainder.
    ///   - Pruning: skip if remaining engines can't cover what's left.
    ///
    /// Example: 3 AE × 2000 kW, demand = 4949 kW
    ///   AE₁ = 78% (1560 kW) → remaining = 3389 kW
    ///     AE₂ = 80% (1600 kW) → AE₃ = 1789/2000 = 89.5% → within limit → ADD
    ///     AE₂ = 78% (1560 kW) → AE₃ = 1829/2000 = 91.5% → exceeds 90% → SKIP
    ///   AE₁ = 80% (1600 kW) → remaining = 3349 kW
    ///     AE₂ = 78% (1560 kW) → AE₃ = 1789/2000 = 89.5% → within limit → ADD
    ///     AE₂ = 80% (1600 kW) → AE₃ = 1749/2000 = 87.5% → within limit → ADD
    ///
    /// In debugger: put breakpoint on candidates.Add, see current[] with actual load values.
    /// </summary>
    private static List<double[]> GenerateCandidates(
        int activeCount, double capacityKw, double demandKw)
    {
        var candidates = new List<double[]>();

        if (activeCount == 1)
        {
            // Single engine — just check if it can cover the demand
            var load = capacityKw > 0 ? demandKw / capacityKw : 0;
            if (load >= MinLoad && load <= MaxLoad)
                candidates.Add(new[] { load });
        }
        else
        {
            // Multiple engines — sweep and let last absorb remainder
            var current = new double[activeCount];
            BuildCombinations(candidates, current, 0, demandKw, capacityKw);
        }

        return candidates;
    }

    /// <summary>
    /// Recursively fills engine loads left to right.
    /// Max recursion depth = number of AE engines (2-4 in practice).
    ///
    /// At each level you can see in debugger:
    ///   current[0] = 0.78, current[1] = 0.80, remainingKw = 1789
    /// </summary>
    private static void BuildCombinations(
        List<double[]> candidates, double[] current, int engineIndex,
        double remainingKw, double capacityKw)
    {
        int lastIndex = current.Length - 1;

        // ── LAST ENGINE: absorb remainder, check if valid ──
        if (engineIndex == lastIndex)
        {
            double lastLoad = capacityKw > 0 ? remainingKw / capacityKw : 0;
            if (lastLoad >= MinLoad && lastLoad <= MaxLoad)
            {
                current[lastIndex] = lastLoad;
                candidates.Add((double[])current.Clone());
            }
            return;
        }

        // ── FREE ENGINE: sweep from 10% to 90%, recurse for next ──
        int enginesAfter = lastIndex - engineIndex; // how many engines still need assignment
        double minCoverable = enginesAfter * MinLoad * capacityKw;
        double maxCoverable = enginesAfter * MaxLoad * capacityKw;

        for (double load = MinLoad; load <= MaxLoad + PowerTolerance; load += LoadStep)
        {
            double powerKw = Math.Min(load, MaxLoad) * capacityKw;
            double newRemaining = remainingKw - powerKw;

            // Pruning: can the remaining engines cover what's left?
            if (newRemaining > maxCoverable)
                continue;  // too much left — this engine's load is too low
            if (newRemaining < minCoverable)
                break;     // too little left — this and higher loads won't work

            current[engineIndex] = Math.Min(load, MaxLoad);
            BuildCombinations(candidates, current, engineIndex + 1, newRemaining, capacityKw);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PHASE 2: Evaluate candidates and pick lowest FOC
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Evaluates FOC for each candidate and returns the best one.
    /// Breakpoint on foreach → inspect 'loads' directly: [0.78, 0.80, 0.895]
    /// </summary>
    private static AeDistribution? FindLowestFoc(
        List<double[]> candidates, double capacityKw,
        List<SfocDataPoint> sfocData, int totalCount, int activeCount)
    {
        double bestFoc = double.MaxValue;
        double[]? bestLoads = null;

        foreach (var loads in candidates)
        {
            double totalFoc = 0;
            for (int i = 0; i < loads.Length; i++)
            {
                double powerKw = loads[i] * capacityKw;
                double sfoc = SfocInterpolationHelper.Interpolate((decimal)loads[i], sfocData);
                totalFoc += CalculationHelpers.FocTonPerHour(powerKw, sfoc);
            }

            if (totalFoc < bestFoc)
            {
                bestFoc = totalFoc;
                bestLoads = new double[totalCount];
                Array.Copy(loads, bestLoads, activeCount);
                // Remaining slots stay 0 → engines OFF
            }
        }

        return bestLoads != null
            ? new AeDistribution(bestLoads, bestFoc, activeCount)
            : null;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task<GeneratorSetpoint?> BuildFixedSgSetpointAsync(
        EngineCombination optimal, CalculatorInput input)
    {
        if (!optimal.SgEnabled || optimal.SgPowerKw <= PowerTolerance)
            return null;

        var sgCapacity = optimal.ActiveMeCount * input.SgCapacityPerEngine;
        var sgLoad = sgCapacity > 0 ? optimal.SgPowerKw / sgCapacity : 0;
        var sfoc = await _sfocService.GetSfocForLoadAsync(
            (decimal)sgLoad, EngineCategory.Main, input.MainEngineTypeId);

        return new GeneratorSetpoint
        {
            GeneratorType = GeneratorType.SG,
            CapacityKw = sgCapacity,
            LoadPercent = sgLoad,
            PowerKw = optimal.SgPowerKw,
            Sfoc = sfoc
        };
    }

    private static List<GeneratorSetpoint> BuildSetpoints(
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

    private static Level2Result BuildPassThroughResult(
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

    private async Task<List<SfocDataPoint>> GetFilteredAeSfocDataAsync(CalculatorInput input)
    {
        var data = await _sfocService.GetSfocDataAsync(EngineCategory.Auxiliary, input.AuxEngineTypeId);
        return data.Where(p => p.Load > 0).OrderBy(p => p.Load).ToList();
    }
}