namespace VoyageEnergyAdvisor.WebApi
{
    using Microsoft.AspNetCore.Mvc;
    using VoyageEnergyAdvisor.Core.CommonModels;
    using VoyageEnergyAdvisor.Core.Repositories;

    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ICurrentUserRepository _userRepo;

        public UserController(ICurrentUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            var result = await _userRepo.CreateUserAsync(dto);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }
    }
}
