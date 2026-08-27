using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Data.DataRepositories;
using VoyageEnergyAdvisor.Data.Entities;
using Xunit;
namespace VoyageEnergyCalculatorService.Test.Data
{
    public class CurrentUserRepositoryTests
    {
        private readonly CurrentUserRepository _repository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CurrentUserRepositoryTests()
        {
            var provider = TestHelper.BuildTestServiceProvider();

            _userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            _roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
            var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();

            TestHelper.SetHttpContextUser(httpContextAccessor);
            TestHelper.SeedRolesAsync(_roleManager).GetAwaiter().GetResult();

            _repository = new CurrentUserRepository(_userManager, httpContextAccessor);
        }

        [Fact]
        public async Task CreateUserAsync_Should_Create_User_And_Assign_Role()
        {
            var dto = new CreateUserDto
            {
                UserName = "testuser",
                Email = "testuser@example.com",
                Password = "Test@123",
                Role = "Admin"
            };

            var result = await _repository.CreateUserAsync(dto);

            Assert.True(result.Success);
            Assert.Contains("created successfully", result.Message);
        }

        [Fact]
        public async Task CreateUserAsync_Should_Fail_If_User_Already_Exists()
        {
            var dto = new CreateUserDto
            {
                UserName = "testuser",
                Email = "testuser@example.com",
                Password = "Test@123",
                Role = "User"
            };

            await _repository.CreateUserAsync(dto);
            var result = await _repository.CreateUserAsync(dto);

            Assert.False(result.Success);
            Assert.Contains("User already exists", result.Message);
        }

        [Fact]
        public async Task GetCurrentUserAsync_Should_Return_User_From_Claims()
        {
            var dto = new CreateUserDto
            {
                UserName = "testuser",
                Email = "testuser@example.com",
                Password = "Test@123",
                Role = "User"
            };

            var result = await _repository.CreateUserAsync(dto);
            Assert.True(result.Success);

            var user = await _userManager.FindByNameAsync(dto.UserName);
            Assert.NotNull(user);

            TestHelper.CreateAccessorWithUser(user);

            var currentUser = await _repository.GetCurrentUserAsync();

            Assert.NotNull(currentUser);
            Assert.Equal("testuser", currentUser.Name);
        }

        [Fact]
        public async Task AuthenticateUserAsync_Should_Return_User_If_Valid()
        {
            var dto = new CreateUserDto
            {
                UserName = "validuser",
                Email = "validuser@example.com",
                Password = "Valid@123",
                Role = "User"
            };

            await _repository.CreateUserAsync(dto);
            var result = await _repository.AuthenticateUserAsync(dto.UserName, dto.Password);

            Assert.NotNull(result);
            Assert.Equal("validuser", result.Name);
        }

        [Fact]
        public async Task AuthenticateUserAsync_Should_Return_Null_If_Invalid()
        {
            var result = await _repository.AuthenticateUserAsync("nonexistent", "wrongpass");
            Assert.Null(result);
        }
    }
}
