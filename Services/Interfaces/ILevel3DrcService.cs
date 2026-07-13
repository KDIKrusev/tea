using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Interfaces;

public interface ILevel3DrcService
{
    /// <summary>
    /// Calculate annual fuel savings from Dynamic Ramp Control (DRC).
    /// DRC reduces generator load spike amplitude by 20%, saving fuel due to
    /// the non-linearity of the SFOC curve.
    /// </summary>
    Task<Level3Result> CalculateDrcSavingsAsync(Level2Result level2Result, CalculatorInput input, double annualHours);
}
