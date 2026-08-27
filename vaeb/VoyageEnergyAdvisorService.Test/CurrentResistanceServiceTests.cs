using Moq;
using VoyageEnergyAdvisor.Core.Repositories;
using VoyageEnergyAdvisor.Core.Services.CurrentResistanceService;
using VoyageEnergyAdvisor.Core.Services.CurrentResistanceService.Models;
using Xunit;

namespace VoyageEnergyCalculatorService.Test
{
    public class CurrentResistanceServiceTests
    {
        [Theory]
        [InlineData(1.0, 10.0, 1.0, 10.0)]
        [InlineData(3.2, 15.0, 5.0, 150.0)]
        [InlineData(8, 90.0, 9.0, 360.0)]
        public void GetCurrentResistancePower_ReturnsExpectedResult(
            double relativeCurrentSpeed,
            double relativeCurrentDirection,
            double sog,
            double expectedResult)
        {
            // Arrange
            var mockConfigurationRepository = new Mock<IConfigurationRepository>();
            var currentResistanceConfig = new CurrentResistanceServiceConfiguration
            {
                CurrentResistanceItems = new List<CurrentResistanceServiceConfigurationItem>
                {
                    new CurrentResistanceServiceConfigurationItem(1.0, 10.0, 1.0, 10),
                    new CurrentResistanceServiceConfigurationItem(3.2, 15.0, 5.0, 20),
                    new CurrentResistanceServiceConfigurationItem(4.0, 15.0, 5.0, 30),
                    new CurrentResistanceServiceConfigurationItem(4.0, 90.0, 9.0, 40),
                }
            };

            mockConfigurationRepository.Setup(repo => repo.GetConfigurationAsync<CurrentResistanceServiceConfiguration>())
                .ReturnsAsync(currentResistanceConfig);

            var service = new CurrentResistanceService(currentResistanceConfig);

            // Act
            var result = service.GetCurrentResistancePower(relativeCurrentSpeed, relativeCurrentDirection, sog);

            // Assert
            Assert.Equal(expectedResult, result, precision: 5);
        }
    }
}