using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Interfaces;

public interface ILevel3DrcService
{
    /// <summary>
    /// Calculate annual fuel savings from Dynamic Ramp Control (DRC).
    /// DRC reduces generator load spike amplitude by 20%, saving fuel due to
    /// the non-linearity of the SFOC curve.
    /// </summary>
    /// <param name="curves">
    /// Pre-resolved SFOC curves for the calculation — the generator loop reads them directly
    /// instead of awaiting a lookup per ramp point.
    /// </param>
    /// <param name="batteryHotelPeakShavingKw">
    /// Hotel/mission-side ± band already covered by the battery (Increment E, Q4 working rule):
    /// DRC operates on the residual variation max(0, variation − band) to avoid monetizing the
    /// same spikes twice. 0 = no battery effect (today's behaviour).
    /// </param>
    Level3Result CalculateDrcSavings(
        Level2Result level2Result, CalculatorInput input, EngineFuelCurves curves, double annualHours,
        double batteryHotelPeakShavingKw = 0);
}
