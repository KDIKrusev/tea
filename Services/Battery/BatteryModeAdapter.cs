using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Results;

namespace KSailCalc.Api.Services.Battery;

/// <summary>
/// Translates what <see cref="BatteryAllocationService"/> allocated into what the rest of the
/// pipeline needs: the Level 1 load adjustment, the hotel-side band that offsets Level 3's DRC
/// variation, and the response panel.
///
/// This is battery domain knowledge — which plant side carries which reserve, which loads are
/// thrust-side, when a panel exists at all. It lived in the orchestrator until story R-B; it belongs
/// next to the allocation itself. Pure and static: no state, no I/O.
/// </summary>
internal static class BatteryModeAdapter
{
    /// <summary>Hotel/mission-side ± band covered by the battery (offsets the L3 DRC variation).</summary>
    internal static double HotelPeakShavingKw(BatteryModeAllocation allocation)
        => allocation.Loads
            .Where(l => !l.Load.IsThrustSide() && l.Function == BatteryFunction.PeakShaving)
            .Sum(l => l.CoveredBandKw);

    /// <summary>
    /// Uncovered reserve split by plant side: thrust-related loads raise the propulsion demand,
    /// electrical loads (hotel, mission consumers) raise the hotel demand.
    /// PropulsionPeakShavingKw = the battery's covered ± band on thrust loads — must flow through
    /// PTI when PTI is modelled (Increment C discharge gate).
    /// </summary>
    internal static BatteryL1Adjustment ToAdjustment(BatteryModeAllocation allocation)
    {
        double propulsion = 0, hotel = 0, propulsionBand = 0;
        foreach (var load in allocation.Loads)
        {
            if (load.Load.IsThrustSide())
            {
                propulsion += load.UncoveredReserveKw;
                propulsionBand += load.CoveredBandKw;
            }
            else
            {
                hotel += load.UncoveredReserveKw;
            }
        }
        return new BatteryL1Adjustment(propulsion, hotel, propulsionBand);
    }

}
