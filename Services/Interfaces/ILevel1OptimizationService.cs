using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Services.Interfaces;

public interface ILevel1OptimizationService
{
    /// <summary>
    /// Evaluate all valid ME/SG/AE ON/OFF combinations for the given mode and
    /// return the optimal (lowest FOC) and baseline (second-highest FOC) configurations.
    /// </summary>
    Task<Level1Result> FindOptimalCombinationAsync(CalculatorInput input, OperationalMode mode, double? overridePropulsionKw = null, int? baselineIndex = null);
}
