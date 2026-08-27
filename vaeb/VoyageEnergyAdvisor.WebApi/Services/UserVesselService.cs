namespace VoyageEnergyAdvisor.WebApi.Services
{
    using Microsoft.AspNetCore.Http;
    using System.Security.Claims;
    using VoyageEnergyAdvisor.Core.CommonModels;

    public class UserVesselService : IUserVesselService
    {
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserVesselService(IJwtTokenService jwtTokenService, IHttpContextAccessor httpContextAccessor)
        {
            _jwtTokenService = jwtTokenService;
            _httpContextAccessor = httpContextAccessor;
        }

        public string? SetCurrentVessel(int vesselId)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return null;

            var identity = user.Identity as ClaimsIdentity;
            if (identity == null) return null;

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return null;

            var currentUser = new CurrentUserDto
            {
                Id = userId,
                Name = user.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown"
            };

            return _jwtTokenService.GenerateToken(currentUser, vesselId);
        }
    }
}
