namespace VoyageEnergyAdvisor.WebApi
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using VoyageEnergyAdvisor.Core.CommonModels;
    using VoyageEnergyAdvisor.Core.Repositories;
    using VoyageEnergyAdvisor.WebApi.Services;

    [Authorize]
    [ApiController]
    [Route("api/v1/vessel")]
    public class VesselController : ControllerBase
    {
        private readonly IUserVesselRepository _userVesselRepository;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IUserVesselService _userVesselService;
        private readonly ILogger<VesselController> _logger;

        public VesselController(
            IUserVesselRepository userVesselRepository,
            IJwtTokenService jwtTokenService,
            IUserVesselService userVesselService,
            ILogger<VesselController> logger)
        {
            _userVesselRepository = userVesselRepository;
            _jwtTokenService = jwtTokenService;
            _userVesselService = userVesselService;
            _logger = logger;
        }

        [HttpGet("user-vessels")]
        public async Task<IActionResult> GetUserVessels()
        {
            try
            {
                var vessels = await _userVesselRepository.GetUserVesselsAsync();

                if (vessels == null || vessels.Count == 0)
                {
                    return NotFound(new { Message = "No vessels found for this user." });
                }

                return Ok(vessels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user vessels.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("set-current-vessel")]
        public IActionResult SetCurrentVessel([FromBody] int vesselId)
        {
            var newToken = _userVesselService.SetCurrentVessel(vesselId);
            if (newToken == null)
            {
                _logger.LogWarning("Unauthorized attempt to set vessel.");
                return Unauthorized("User is not authenticated.");
            }

            return Ok(new { Message = "Vessel updated successfully.", Token = newToken });
        }
    }
}

