using VoyageEnergyAdvisor.Core.Services.WindResistanceService;
using VoyageEnergyAdvisor.Core.Services.WindResistanceService.Models;
using Xunit;

namespace VoyageEnergyAdvisorService.Test;

public class WindResistanceServiceTests
{
    [Theory]
    [InlineData(0, 31, 5, 1 * 5)]
    [InlineData(10000, 31, 5, 3 * 5)]
    [InlineData(66, 50, 66, 4 * 66)]

    public void GetWindResistancePower_ShouldReturnCorrectPower(double relativeWindSpeed, double apparentWindDirection, double sog, double expectedPower)
    {
        // Arrange
        var windResistanceItems = new List<WindResistanceServiceConfigurationItem>
        {
            new (30, 10, 1),
            new (30, 15, 2),
            new (30, 20, 3),
            new (50, 66, 4),
            new (35, 10, 4),
            new (35, 15, 5),
            new (35, 20, 6),
            new (40, 10, 7),
            new (40, 15, 8),
            new (40, 20, 9),
        };
        var config = new WindResistanceServiceConfiguration { WindResistanceItems = windResistanceItems };

        var service = new WindResistanceService(config);

        // Act
        double result = service.GetWindResistancePower(relativeWindSpeed, apparentWindDirection, sog);

        // Assert
        Assert.Equal(expectedPower, result, precision: 2);
    }
}