namespace VoyageEnergyAdvisor.Data.DataRepositories
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using VoyageEnergyAdvisor.Core.CommonModels;
    using VoyageEnergyAdvisor.Core.Repositories;
    using VoyageEnergyAdvisor.Data.Entities;

    public class CurrentUserRepository : ICurrentUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserRepository(
                UserManager<ApplicationUser> userManager, 
                IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<CurrentUserDto?> GetCurrentUserAsync()
        {
            var principal = _httpContextAccessor.HttpContext?.User;

            if (principal == null)
            {
                return null; 
            }

            var user = await _userManager.GetUserAsync(principal);
            if (user == null) return null;

            return new CurrentUserDto
            {
                Id = user.Id,
                Name = user.FullName
            };
        }

        public async Task<CurrentUserDto?> AuthenticateUserAsync(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return null; 
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordValid)
            {
                return null;
            }

            return new CurrentUserDto
            {
                Id = user.Id,
                Name = user.FullName
            };
        }

        public async Task<OperationResult> CreateUserAsync(CreateUserDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return new OperationResult { Success = false, Message = "User already exists." };

            var user = new ApplicationUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                FullName = dto.UserName
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new OperationResult { Success = false, Message = $"Failed to create user: {errors}" };
            }

            var allowedRoles = new[] { "Admin", "User" };
            var role = allowedRoles.Contains(dto.Role) ? dto.Role : "User";

            await _userManager.AddToRoleAsync(user, role);

            return new OperationResult
            {
                Success = true,
                Message = $"User '{dto.UserName}' created successfully with role '{role}'."
            };
        }
    }
}
