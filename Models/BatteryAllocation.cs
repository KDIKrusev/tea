using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Models;

/// <summary>
/// Result of allocating the battery power budget over one operational mode's load demands
/// (port of the Excel "Load Demands" sheet — see docs/battery-feature-analysis/02 §1.3).
/// </summary>
public class BatteryModeAllocation
{
    public OperationalMode Mode { get; set; }

    /// <summary>Per-load allocation rows, in priority order.</summary>
    public List<BatteryLoadAllocation> Loads { get; set; } = new();

    /// <summary>ΣJ — the ± band the battery covers (Excel row 10, col J total).</summary>
    public double PeakShavingBandKw { get; set; }

    /// <summary>ΣL — uncovered variation the running generators must carry as spinning reserve.</summary>
    public double AdditionalSpinningReserveKw { get; set; }

    /// <summary>ΣI — battery kW committed across all loads.</summary>
    public double CommittedBatteryKw { get; set; }

    /// <summary>Budget left after the cascade (final K).</summary>
    public double RemainingBatteryKw { get; set; }
}

/// <summary>
/// Battery-driven adjustment to a mode's Level 1 loads: the spinning reserve the battery could
/// NOT cover, split by which side of the plant carries it. Passing a non-null adjustment also
/// switches the default baseline to the "third highest" rule (decision D1).
/// PropulsionPeakShavingKw is the battery's covered ± band on thrust loads — with PTI configured
/// it must fit through the remaining PTI capacity (Excel "Insufficient PTI" gate, Increment C).
/// </summary>
public record BatteryL1Adjustment(
    double PropulsionReserveKw, double HotelReserveKw, double PropulsionPeakShavingKw = 0);

/// <summary>One row of the allocation cascade (one load demand).</summary>
public class BatteryLoadAllocation
{
    public BatteryLoadType Load { get; set; }

    public BatteryFunction Function { get; set; }

    /// <summary>D — fraction of the battery usage that counts as covered band (1.0 / 0.5 / 0.35 / 0.05).</summary>
    public double CoverageFactor { get; set; }

    /// <summary>E — average load in the period [kW].</summary>
    public double AverageLoadKw { get; set; }

    /// <summary>H — max variation (peak-shaving potential) or full reserve requirement [kW].</summary>
    public double VariationKw { get; set; }

    /// <summary>I = min(remaining budget, H) — battery power used for this load [kW].</summary>
    public double BatteryUsedKw { get; set; }

    /// <summary>J = I × D — the ± band the battery actually covers [kW].</summary>
    public double CoveredBandKw { get; set; }

    /// <summary>L = H − J — what the battery could not cover → extra genset spinning reserve [kW].</summary>
    public double UncoveredReserveKw { get; set; }
}
