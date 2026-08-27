namespace VoyageEnergyCalculatorService.Test.Data
{
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using VoyageEnergyAdvisor.Core.CommonModels;
    using VoyageEnergyAdvisor.Core.Repositories;
    using VoyageEnergyAdvisor.Data.DataRepositories;
    using VoyageEnergyAdvisor.Data.Entities;
    using VoyageEnergyAdvisor.Data;
    using Xunit;

    public class ConfigurationRepositoryTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IUserVesselRepository> _userVesselRepositoryMock;
        private readonly ConfigurationRepository _repository;

        public ConfigurationRepositoryTests()
        {
            var serviceProvider = TestHelper.BuildTestServiceProvider();
            _dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

            _userVesselRepositoryMock = new Mock<IUserVesselRepository>();
            _repository = new ConfigurationRepository(_dbContext, _userVesselRepositoryMock.Object);
        }

        public class TestConfig
        {
            public string Value { get; set; } = string.Empty;
        }

        [Fact]
        public async Task GetConfigurationAsync_Should_Return_Config_When_Exists()
        {
            // Arrange
            var vesselId = 1;
            var configName = nameof(TestConfig);
            var configJson = JsonConvert.SerializeObject(new Dictionary<string, TestConfig>
            {
                { configName, new TestConfig { Value = "TestValue" } }
            });

            _dbContext.Configurations.Add(new Configuration
            {
                ConfigName = $"{configName}.json",
                ConfigJson = configJson,
                VesselId = vesselId
            });

            await _dbContext.SaveChangesAsync();

            _userVesselRepositoryMock
                .Setup(r => r.GetCurrentVesselAsync())
                .ReturnsAsync(new VesselDto { Id = vesselId, Name = "TestVessel" });

            // Act
            var result = await _repository.GetConfigurationAsync<TestConfig>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestValue", result?.Value);
        }

        [Fact]
        public async Task GetConfigurationAsync_Should_Return_Null_When_Config_Not_Exists()
        {
            _userVesselRepositoryMock
                .Setup(r => r.GetCurrentVesselAsync())
                .ReturnsAsync(new VesselDto { Id = 99 });

            var result = await _repository.GetConfigurationAsync<TestConfig>();

            Assert.Null(result);
        }

        [Fact]
        public async Task GetConfigurationAsync_Should_Throw_If_Vessel_Not_Selected()
        {
            _userVesselRepositoryMock
                .Setup(r => r.GetCurrentVesselAsync())
                .ReturnsAsync((VesselDto?)null);

            var ex = await Assert.ThrowsAsync<Exception>(() => _repository.GetConfigurationAsync<TestConfig>());

            Assert.Equal("Vessel not selected.", ex.Message);
        }
    }
}
