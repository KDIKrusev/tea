
using VoyageEnergyAdvisor.Core.Services.SailContributionService;
using VoyageEnergyAdvisor.Core.Services.SailContributionService.Models;
using Xunit;

namespace VoyageEnergyAdvisorService.Test
{
    public class SailContributionServiceTests
    {
        private SailContributionService _service;

        public SailContributionServiceTests()
        {
            var sailContributionServiceConfiguration = new SailContributionServiceConfiguration
            {
                SailContributions = new List<SailContributionItem>
                {
                    new SailContributionItem(45, 10, 1.5),
                },
                SailActivePowers = new List<SailActivePowerItem>
                {
                    new SailActivePowerItem(45, 10, 2.0),
                }
            };

            _service = new SailContributionService(sailContributionServiceConfiguration);
        }

        [Theory]
        [InlineData(10, 45, 5, 1.5 * 5 - 2.0)] // relativeWindSpeed, relativeWindToDirection, sog, expectedResult
        public void GetSailContributionPower_ValidInput_ReturnsExpectedResult(double apparentWindSpeed, double apparentWindDirection, double sog, double expectedResult)
        {
            // Act
            double result = _service.GetSailContributionPower(apparentWindSpeed, apparentWindDirection, sog);

            // Assert
            Assert.Equal(expectedResult, result, precision: 2);
        }
    }
}