using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Interfaces;

public interface ILevel2OptimizationService
{
    /// <summary>
    /// Find the optimal load percentage for each active generator (SG + AE) from Level 1's
    /// optimal combination that minimizes total SFOC. ME load is fixed (vessel speed).
    /// </summary>
    Task<Level2Result> OptimizeLoadSetpointsAsync(Level1Result level1Result, CalculatorInput input);
}
