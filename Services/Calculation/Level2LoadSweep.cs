using KSailCalc.Api.Models;
using KSailCalc.Api.Services.Helpers;

namespace KSailCalc.Api.Services.Calculation;

/// <summary>One evaluated way of splitting the aux demand: a load per engine and what it costs.</summary>
/// <param name="LoadPercents">
/// One entry per INSTALLED engine — trailing zeros mean Level 2 switched that engine off.
/// </param>
internal record AeDistribution(double[] LoadPercents, double TotalFoc, int ActiveCount);

/// <summary>
/// The search: which load split across the auxiliary engines burns the least fuel.
///
/// Kept whole and in one file on purpose. This is a single algorithm — generate, prune, evaluate —
/// and cutting it further would scatter something that only makes sense together. What was taken out
/// of it is the surrounding work: the orchestration (<see cref="Level2OptimizationService"/>) and
/// turning the winner into setpoints (<see cref="Level2SetpointBuilder"/>).
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
///   Also tries 1 AE: 2750/2000 = 137.5% → SKIP (exceeds the 90% ceiling)
///   Result: AE₁ = 68%, AE₂ = 69.5%
///
/// The load window is <see cref="PlantLimits.MinAuxLoadFraction"/>..<see cref="PlantLimits.MaxAuxLoadFraction"/>
/// (10%..90%) — quoted rather than restated in prose, because this comment previously said 80%
/// while the code enforced 90%.
///
/// What the search GUARANTEES is asserted in <c>Level2InvariantTests</c> — read those before
/// reading this loop; they are the faster path to understanding it.
/// </summary>
internal static class Level2LoadSweep
{
    private const double MinLoad = PlantLimits.MinAuxLoadFraction;
    private const double MaxLoad = PlantLimits.MaxAuxLoadFraction;
    private const double LoadStep = 0.02;
    private const double PowerTolerance = PlantLimits.PowerToleranceKw;

    /// <summary>
    /// Tries each possible number of active AE (N, N-1, ..., 1).
    /// For each count, generates all valid load combinations, evaluates FOC, picks the best.
    /// Returns null when no count can cover the demand inside the load window.
    /// </summary>
    internal static AeDistribution? FindBestDistribution(
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
    ///   - Each engine (except last) sweeps from 10% to 90% in 2% steps.
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
}
