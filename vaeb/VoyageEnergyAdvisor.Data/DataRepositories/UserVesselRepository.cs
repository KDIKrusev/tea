namespace VoyageEnergyAdvisor.Data.DataRepositories
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using VoyageEnergyAdvisor.Core.CommonModels;
    using VoyageEnergyAdvisor.Core.Repositories;

    public class UserVesselRepository : IUserVesselRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICurrentUserRepository _currentUserRepository;

        public UserVesselRepository(ApplicationDbContext dbContext,
                        IHttpContextAccessor httpContextAccessor,
                        ICurrentUserRepository currentUserRepository)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _currentUserRepository = currentUserRepository;
        }

        public async Task<List<VesselDto>> GetUserVesselsAsync()
        {
            var user = await _currentUserRepository.GetCurrentUserAsync();
            if (user == null) return [];

            return await _dbContext.UserVessels
                .Where(uv => uv.UserId == user.Id)
                .Select(uv => new VesselDto
                {
                    Id = uv.Vessel.Id,
                    Name = uv.Vessel.Name
                }).ToListAsync();
        }

        public async Task<VesselDto?> GetCurrentVesselAsync()
        {
            var vesselId = GetVesselIdFromClaims();
            if (!vesselId.HasValue) return null;

            var vessel = await _dbContext.Vessels.FindAsync(vesselId.Value);
            if (vessel == null) return null;

            return new VesselDto
            {
                Id = vessel.Id,
                Name = vessel.Name,
                VesselNumber = vessel.VesselNumber
            };
        }

        public async Task<VesselDto?> GetDefaultVesselForUserAsync(string userId)
        {
            var vessel = await _dbContext.UserVessels
                .Where(uv => uv.UserId == userId)
                .Select(uv => uv.Vessel)
                .FirstOrDefaultAsync();

            return vessel == null ? null : new VesselDto
            {
                Id = vessel.Id,
                Name = vessel.Name,
                VesselNumber = vessel.VesselNumber
            };
        }

        private int? GetVesselIdFromClaims()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var vesselIdClaim = user?.FindFirst("VesselId")?.Value;

            return int.TryParse(vesselIdClaim, out var id) ? id : null;
        }

    }
}
