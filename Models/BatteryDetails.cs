namespace KSailCalc.Api.Models;

/// <summary>
/// Battery contribution reported to the client (null on <see cref="AllVariantsCalculationResult"/>
/// when no battery is active). SpinningReserveKw / PeakShavingKw are the sketch's "Functions"
/// fields — computed by the allocation, not user inputs (decision D2).
/// </summary>
public class BatteryDetails
{
    public double CapacityKwh { get; set; }

    public double PowerKw { get; set; }

    /// <summary>ΣL over relevant modes — variation the gensets still carry as spinning reserve.</summary>
    public double SpinningReserveKw { get; set; }

    /// <summary>ΣJ over relevant modes — the ± band the battery shaves off the peaks.</summary>
    public double PeakShavingKw { get; set; }

    /// <summary>
    /// R3a dual-scenario benefit: optimal FOC of the no-battery reference (full variation as
    /// genset reserve) minus optimal FOC with battery, × mode hours, summed over relevant modes.
    /// Reported separately — NOT folded into the L1/L2/L3 tier savings.
    /// </summary>
    public double BenefitFocTonPerYear { get; set; }

    /// <summary>BenefitFocTonPerYear × fuel price [USD/yr].</summary>
    public double BenefitCostPerYear { get; set; }

    /// <summary>Per-mode allocation breakdown (rows in priority order).</summary>
    public List<BatteryModeAllocation> ModeAllocations { get; set; } = new();
}
