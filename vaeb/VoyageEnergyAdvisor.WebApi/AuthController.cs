namespace VoyageEnergyAdvisor.WebApi
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using VoyageEnergyAdvisor.Core.Repositories;
    using VoyageEnergyAdvisor.WebApi.Dtos;
    using VoyageEnergyAdvisor.WebApi.Services;

    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ICurrentUserRepository _currentUserRepository;
        private readonly IUserVesselRepository _userVesselRepository;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
                ICurrentUserRepository currentUserRepository,
                IUserVesselRepository userVesselRepository,
                IJwtTokenService jwtTokenService,
                ILogger<AuthController> logger)
        {
            _currentUserRepository = currentUserRepository;
            _userVesselRepository = userVesselRepository;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto)
        {
            var user = await _currentUserRepository.AuthenticateUserAsync(loginDto.Username, loginDto.Password);
            if (user == null)
            {
                _logger.LogWarning("Invalid login attempt for {Username} ", loginDto.Username);
                return Unauthorized(new { Message = "Invalid username or password" });
            }

            var defaultVessel = await _userVesselRepository.GetDefaultVesselForUserAsync(user.Id);

            var token = _jwtTokenService.GenerateToken(user, defaultVessel?.Id);
            return Ok(new { Token = token });
        }

    }

}
