using Microsoft.AspNetCore.Mvc;
using KSailCalc.Api.Models;
using KSailCalc.Api.Services.Interfaces;
using System.Linq;

namespace KSailCalc.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CalculatorController : ControllerBase
{
    private readonly ICalculatorService _calculatorService;
    private readonly IValidationService _validationService;
    private readonly ILogger<CalculatorController> _logger;

    public CalculatorController(
        ICalculatorService calculatorService,
        IValidationService validationService,
        ILogger<CalculatorController> logger)
    {
        _calculatorService = calculatorService;
        _validationService = validationService;
        _logger = logger;
    }

    /// <summary>
    /// Calculate iEMS savings for all integration levels at once
    /// </summary>
    /// <param name="input">Calculator input parameters</param>
    /// <returns>Calculation results for all three integration levels</returns>
    [HttpPost("calculate-all-variants")]
    [ProducesResponseType(typeof(AllVariantsCalculationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CalculateAllVariants([FromBody] CalculatorInput input)
    {
        try
        {
            _logger.LogInformation("Calculating iEMS savings for all variants");

            // Validate input
            var validationResult = _validationService.ValidateInput(input);
            if (!validationResult.Valid)
            {
                _logger.LogWarning("Validation failed: {Errors}", string.Join(", ", validationResult.Errors));
                return BadRequest(validationResult);
            }

            // Perform calculation with validation warnings attached
            var result = await _calculatorService.CalculateAllVariantsAsync(input, validationResult.Warnings);

            if (validationResult.Warnings.Any())
            {
                _logger.LogWarning("Calculation completed with warnings: {Warnings}",
                    string.Join(", ", validationResult.Warnings.Select(w => w.Message)));
            }

            return Ok(result);
        }
        catch (NoValidCombinationException ex)
        {
            // Infeasible plant configuration is a user-input problem, not a server fault:
            // answer 400 in the same shape as validation so the client shows the reason (QA-C-1).
            _logger.LogWarning(ex, "No valid engine combination for {Mode} mode", ex.Mode);
            return BadRequest(new ValidationResult
            {
                Valid = false,
                Errors = { $"No feasible engine configuration: {ex.UserMessage}" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating iEMS savings for all variants");
            return StatusCode(500, new { error = "An error occurred while calculating savings" });
        }
    }
}
