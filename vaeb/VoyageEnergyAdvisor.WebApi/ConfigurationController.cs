namespace VoyageEnergyAdvisor.WebApi
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using VoyageEnergyAdvisor.Core.Repositories;
    using VoyageEnergyAdvisor.Core.Services.CostCalculationService.Models;
    using VoyageEnergyAdvisor.WebApi.Dtos;

    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfigurationRepository _configurationRepository;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(
            IConfigurationRepository configurationRepository,
            ILogger<ConfigurationController> logger)
        {
            _configurationRepository = configurationRepository;
            _logger = logger;
        }

        [HttpGet("calculation-configuration")]
        public async Task<ActionResult<VoyageCalculationConfigurationResponseDto>> GetVoyageCalculationConfiguration()
        {
            try
            {
                var config = await _configurationRepository.GetConfigurationAsync<CostCalculationServiceConfiguration>();

                if (config == null)
                {
                    _logger.LogWarning("VoyageCalculationConfiguration not found for current vessel");
                    return NotFound(new { Message = "Voyage calculation configuration not found for current vessel" });
                }

                _logger.LogInformation(
                    "Retrieved calculation configuration: FuelPrice={FuelPrice}, EmissionFactor={EmissionFactor}",
                    config.FuelPricePerKg,
                    config.EmissionFactorCO2PerKg);

                return Ok(new VoyageCalculationConfigurationResponseDto
                {
                    FuelPricePerKg = config.FuelPricePerKg,
                    EmissionFactorCO2PerKg = config.EmissionFactorCO2PerKg
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving calculation configuration");
                return StatusCode(500, new { Message = "An error occurred while retrieving calculation configuration" });
            }
        }

        [HttpPut("calculation-configuration")]
        public async Task<ActionResult<VoyageCalculationConfigurationResponseDto>> UpdateVoyageCalculationConfiguration(
                    [FromBody] VoyageCalculationConfigurationRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new VoyageCalculationConfigurationResponseDto
                    {
                        Success = false,
                        Message = "Request body is required"
                    });
                }

                if (!request.FuelPricePerKg.HasValue && !request.EmissionFactorCO2PerKg.HasValue)
                {
                    return BadRequest(new VoyageCalculationConfigurationResponseDto
                    {
                        Success = false,
                        Message = "At least one field must be provided for update"
                    });
                }

                if (request.FuelPricePerKg.HasValue && request.FuelPricePerKg.Value <= 0)
                {
                    return BadRequest(new VoyageCalculationConfigurationResponseDto
                    {
                        Success = false,
                        Message = "Fuel price must be greater than 0"
                    });
                }

                var existingConfig = await _configurationRepository.GetConfigurationAsync<CostCalculationServiceConfiguration>();

                if (existingConfig == null)
                {
                    return NotFound();
                }
                else
                {
                    // Update only the provided fields
                    if (request.FuelPricePerKg.HasValue)
                    {
                        existingConfig.FuelPricePerKg = request.FuelPricePerKg.Value;
                    }

                    if (request.EmissionFactorCO2PerKg.HasValue)
                    {
                        existingConfig.EmissionFactorCO2PerKg = request.EmissionFactorCO2PerKg.Value;
                    }
                }

                // Save updated configuration
                await _configurationRepository.UpdateConfigurationAsync(existingConfig);

                _logger.LogInformation(
                    "Updated calculation configuration: FuelPrice={FuelPrice}, EmissionFactor={EmissionFactor}",
                    existingConfig.FuelPricePerKg,
                    existingConfig.EmissionFactorCO2PerKg);

                return Ok(new VoyageCalculationConfigurationResponseDto
                {
                    Success = true,
                    FuelPricePerKg = existingConfig.FuelPricePerKg,
                    EmissionFactorCO2PerKg = existingConfig.EmissionFactorCO2PerKg,
                    Message = "Configuration updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating calculation configuration");
                return StatusCode(500, new VoyageCalculationConfigurationResponseDto
                {
                    Success = false,
                    Message = "An error occurred while updating configuration"
                });
            }
        }

    }
}