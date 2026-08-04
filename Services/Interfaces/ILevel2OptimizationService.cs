using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Interfaces;

public interface ILevel2OptimizationService
{
    /// <summary>
    /// Find the optimal load percentage for each active generator (SG + AE) from Level 1's
    /// optimal combination that minimizes total SFOC. ME load is fixed (vessel speed).
    /// </summary>
    /// <param name="curves">
    /// Pre-resolved SFOC curves — the sweep already read the aux curve as data; now the shaft
    /// generator's lookup comes from the same place instead of an awaited service call.
    /// </param>
    Level2Result OptimizeLoadSetpoints(Level1Result level1Result, CalculatorInput input, EngineFuelCurves curves);
}
