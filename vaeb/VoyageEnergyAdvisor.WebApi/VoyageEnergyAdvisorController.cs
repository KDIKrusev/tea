using VoyageEnergyAdvisor.WebApi.Dtos;

namespace VoyageEnergyAdvisor.WebApi
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Core.Services.VoyageEnergyAdvisorService;
    using VoyageEnergyAdvisor.Core.CommonModels.Exceptions;
    using Microsoft.AspNetCore.Authorization;
    using VoyageEnergyAdvisor.Core.Services.WeatherProviders;

    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class VoyageEnergyAdvisorController(
        IVoyageEnergyAdvisorService voyageService,
        ILogger<VoyageEnergyAdvisorController> logger,
         ICancellationTokenService cancellationTokenService)
        : ControllerBase
    {
        [HttpPost("update")]
        public async  Task<ActionResult<VoyageEnergyAdvisorResponseDto>> CalculateVoyageEnergy([FromBody] VoyageEnergyAdvisorRequestDto requestDto, CancellationToken cancellationToken = default)
        {

            cancellationTokenService.SetToken(cancellationToken);

            try
            {
                var request = VoyageEnergyAdvisorDtoHelpers.GetRequestFromDto(requestDto);
                var resultDto =  VoyageEnergyAdvisorDtoHelpers.GetResponseDto(await voyageService.GetVoyageOptions(request));
                logger.LogInformation("Voyage Energy Calculator Request processed successfully.");
                return Ok(resultDto);
            }
            catch (UserFacingException ex)
            {
                logger.LogWarning(ex, "A user-facing exception occurred.");
                return BadRequest(new
                {
                    IsUserFacing = true,
                    Message = ex.UserMessage
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Voyage Energy Calculator Request.");
                return StatusCode(500, "Internal server error");
            }
        }
        
        // Controller for live data
        [HttpPost("live")]
        public async Task<ActionResult<VoyageEnergyAdvisorLiveResponseDto>> GetLiveData([FromBody] VoyageEnergyAdvisorLiveRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            cancellationTokenService.SetToken(cancellationToken);

            try
            {
                var request = VoyageEnergyAdvisorDtoHelpers.GetLiveRequestFromDto(requestDto);
                var resultDto = VoyageEnergyAdvisorDtoHelpers.GetLiveResponseDto(await voyageService.GetLiveData(request));
                logger.LogInformation("Voyage Energy Live Data Request processed successfully.");
                return Ok(resultDto);
            }
            catch (UserFacingException ex)
            {
                logger.LogWarning(ex, "A user-facing exception occurred.");
                return BadRequest(new
                {
                    IsUserFacing = true,
                    Message = ex.UserMessage
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Voyage Energy Live Data Request.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("optimal")]
        public async Task<ActionResult<VoyageEnergyAdvisorOptimalVoyageResponseDto>> GetOptimalVoyage([FromBody] VoyageEnergyAdvisorOptimalVoyageRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            cancellationTokenService.SetToken(cancellationToken);

            try
            {
                var request = VoyageEnergyAdvisorDtoHelpers.GetOptimalVoyageRequestFromDto(requestDto);
                var resultDto = VoyageEnergyAdvisorDtoHelpers.GetOptimalVoyageResponseDto(await voyageService.GetOptimalVoyageOption(request));
                logger.LogInformation("Voyage Energy Optimal Voyage Request processed successfully.");
                return Ok(resultDto);
            }
            catch (UserFacingException ex)
            {
                logger.LogWarning(ex, "A user-facing exception occurred.");
                return BadRequest(new
                {
                    IsUserFacing = true,
                    Message = ex.UserMessage
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Voyage Energy Optimal Voyage Request.");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
