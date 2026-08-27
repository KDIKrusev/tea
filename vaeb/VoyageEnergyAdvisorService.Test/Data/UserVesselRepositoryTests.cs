namespace VoyageEnergyCalculatorService.Test.Data
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using System.Security.Claims;
    using VoyageEnergyAdvisor.Core.CommonModels;
    using VoyageEnergyAdvisor.Core.Repositories;
    using VoyageEnergyAdvisor.Data.DataRepositories;
    using VoyageEnergyAdvisor.Data.Entities;
    using VoyageEnergyAdvisor.Data;
    using Xunit;

    public class UserVesselRepositoryTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserVesselRepository _repository;

        public UserVesselRepositoryTests()
        {
            var serviceProvider = TestHelper.BuildTestServiceProvider();

            _dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            _httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

            var userId = "test-user-id";

            _httpContextAccessor.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim("VesselId", "1")
                }, "TestAuth"))
            };

            var currentUserRepositoryMock = new Mock<ICurrentUserRepository>();
            currentUserRepositoryMock.Setup(repo => repo.GetCurrentUserAsync())
                .ReturnsAsync(new CurrentUserDto { Id = userId, Name = "Test User" });

            SeedTestData(userId);

            _repository = new UserVesselRepository(_dbContext, _httpContextAccessor, currentUserRepositoryMock.Object);
        }

        private void SeedTestData(string userId)
        {
            var vessel = new Vessel { Id = 1, Name = "Test Vessel", VesselNumber = "V123" };
            var userVessel = new UserVessel { UserId = userId, VesselId = vessel.Id };

            _dbContext.Vessels.Add(vessel);
            _dbContext.UserVessels.Add(userVessel);
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task GetUserVesselsAsync_Should_Return_User_Vessels()
        {
            var vessels = await _repository.GetUserVesselsAsync();

            Assert.Single(vessels);
            Assert.Equal("Test Vessel", vessels[0].Name);
        }

        [Fact]
        public async Task GetCurrentVesselAsync_Should_Return_Vessel_From_Claims()
        {
            var vessel = await _repository.GetCurrentVesselAsync();

            Assert.NotNull(vessel);
            Assert.Equal("Test Vessel", vessel.Name);
        }

        [Fact]
        public async Task GetDefaultVesselForUserAsync_Should_Return_First_Vessel()
        {
            var vessel = await _repository.GetDefaultVesselForUserAsync("test-user-id");

            Assert.NotNull(vessel);
            Assert.Equal("Test Vessel", vessel.Name);
        }

        [Fact]
        public async Task GetDefaultVesselForUserAsync_Should_Return_Null_If_Not_Found()
        {
            var vessel = await _repository.GetDefaultVesselForUserAsync("unknown-user");

            Assert.Null(vessel);
        }

        [Fact]
        public async Task GetCurrentVesselAsync_Should_Return_Null_If_Claim_Missing()
        {
            _httpContextAccessor.HttpContext = new DefaultHttpContext(); // Clear claims

            var result = await _repository.GetCurrentVesselAsync();

            Assert.Null(result);
        }
    }
}
