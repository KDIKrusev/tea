using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Interfaces;

public interface ICalculatorService
{
    Task<AllVariantsCalculationResult> CalculateAllVariantsAsync(CalculatorInput input, List<ValidationWarning>? warnings = null);
}
