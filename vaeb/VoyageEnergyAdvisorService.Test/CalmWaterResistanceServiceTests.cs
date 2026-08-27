using VoyageEnergyAdvisor.Core.Services.CalmWaterResistanceService;
using VoyageEnergyAdvisor.Core.Services.CalmWaterResistanceService.Models;
using Xunit;

namespace VoyageEnergyAdvisorService.Test;

public class CalmWaterResistanceServiceTests
{
    private readonly CalmWaterResistanceService _service;

    public CalmWaterResistanceServiceTests()
    {
        var config = new CalmWaterResistanceServiceConfiguration
        {
            CalmWaterResistanceItems = new List<CalmWaterResistanceServiceConfigurationItem>
            {
                new (10, 100),
                new(20, 200),
                new(30, 300)
            }
        };

        _service = new CalmWaterResistanceService(config);
    }

    [Theory]
    [InlineData(22, 200 * 22.0)] // This should pick the SpeedOverGround = 20 item
    [InlineData(26, 300 * 26.0)] // This should pick the SpeedOverGround = 30 item
    public void GetCalmWaterResistancePower_ValidSpeed_ReturnsCorrectResistancePower(double speedOverGround, double expectedResult)
    {
        // Act
        double result = _service.GetCalmWaterResistancePower(speedOverGround);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void GetCalmWaterResistancePower_ZeroSpeed_ReturnsZero()
    {
        // Arrange
        double speedOverGround = 0.0;

        // Act
        double result = _service.GetCalmWaterResistancePower(speedOverGround);

        // Assert
        Assert.Equal(0.0, result);
    }

    [Theory]
    [InlineData(35)] // Above highest configuration
    [InlineData(40)]
    [InlineData(50)]
    public void GetCalmWaterResistancePower_SpeedAboveMaxConfiguration_UsesHighestConfiguration(double speedOverGround)
    {
        // Act
        double result = _service.GetCalmWaterResistancePower(speedOverGround);

        // Assert
        // Should use the 30 knot configuration (300 * speed)
        double expectedResult = 300 * speedOverGround;
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(5)] // Below lowest configuration
    [InlineData(8)]
    public void GetCalmWaterResistancePower_SpeedBelowMinConfiguration_UsesLowestConfiguration(double speedOverGround)
    {
        // Act
        double result = _service.GetCalmWaterResistancePower(speedOverGround);

        // Assert
        // Should use the 10 knot configuration (100 * speed)
        double expectedResult = 100 * speedOverGround;
        Assert.Equal(expectedResult, result);
    }
}