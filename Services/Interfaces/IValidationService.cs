using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Interfaces;

public interface IValidationService
{
    ValidationResult ValidateInput(CalculatorInput input);
}
