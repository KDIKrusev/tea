using VoyageEnergyAdvisor.Core.Services.FuelConsumptionService;
using VoyageEnergyAdvisor.Core.Services.FuelConsumptionService.Models;
using Xunit;

namespace VoyageEnergyAdvisorService.Test;

public class FuelConsumptionServiceTests
{
    [Theory]
    [InlineData(1000, 75, 0.7, 0.2, 285.71)] // resistancePower, engineLoad%, efficiency, fuelKgPerKWh, expected
    [InlineData(500, 50, 0.65, 0.18, 138.46)]
    [InlineData(2000, 100, 0.75, 0.22, 586.67)]
    public void GetFuelConsumption_ValidInputs_ReturnsCorrectValue(
        double resistancePower,
        int engineLoadPercent,
        double propulsionEfficiency,
        double fuelConsumptionKgPerKWh,
        double expectedResult)
    {
        // Arrange
        var config = new FuelConsumptionServiceConfiguration
        {
            PropulsionEfficiencyItems = new[] { new PropulsionEfficiencyItem(10, propulsionEfficiency) },
            FuelConversionFactorItems = new[] { new FuelConversionFactorItem(engineLoadPercent, fuelConsumptionKgPerKWh) },
            AssumedEngineLoadPercent = engineLoadPercent
        };
        var service = new FuelConsumptionService(config);

        // Act
        var result = service.GetFuelConsumption(resistancePower);

        // Assert
        Assert.Equal(expectedResult, result, precision: 2);
    }

    [Theory]
    [InlineData(1000, 0.7, 50, 257.14)] // AssumedEngineLoadPercent = 50
    [InlineData(1000, 0.7, 75, 285.71)] // AssumedEngineLoadPercent = 75
    [InlineData(1000, 0.7, 100, 314.29)] // AssumedEngineLoadPercent = 100
    public void GetFuelConsumption_DifferentEngineLoadPercentages_ReturnsCorrectValues(
        double resistancePower,
        double propulsionEfficiency,
        int assumedEngineLoadPercent,
        double expectedResult)
    {
        // Arrange
        var config = new FuelConsumptionServiceConfiguration
        {
            PropulsionEfficiencyItems = new[] { new PropulsionEfficiencyItem(10, propulsionEfficiency) },
            FuelConversionFactorItems = new[]
            {
                new FuelConversionFactorItem(50, 0.18),
                new FuelConversionFactorItem(75, 0.20),
                new FuelConversionFactorItem(100, 0.22)
            },
            AssumedEngineLoadPercent = assumedEngineLoadPercent
        };
        var service = new FuelConsumptionService(config);

        // Act
        var result = service.GetFuelConsumption(resistancePower);

        // Assert
        Assert.Equal(expectedResult, result, precision: 2);
    }

    [Theory]
    [InlineData(1000, 75, 0.6, 0.20, 333.33)] // Lower efficiency = higher consumption
    [InlineData(1000, 75, 0.7, 0.20, 285.71)]
    [InlineData(1000, 75, 0.8, 0.20, 250.00)] // Higher efficiency = lower consumption
    public void GetFuelConsumption_DifferentPropulsionEfficiency_ReturnsCorrectValues(
        double resistancePower,
        int engineLoadPercent,
        double propulsionEfficiency,
        double fuelConsumptionKgPerKWh,
        double expectedResult)
    {
        // Arrange
        var config = new FuelConsumptionServiceConfiguration
        {
            PropulsionEfficiencyItems = new[]
            {
                new PropulsionEfficiencyItem(10, propulsionEfficiency),
                new PropulsionEfficiencyItem(15, 0.75) // Service uses First(), so only first matters
            },
            FuelConversionFactorItems = new[] { new FuelConversionFactorItem(engineLoadPercent, fuelConsumptionKgPerKWh) },
            AssumedEngineLoadPercent = engineLoadPercent
        };
        var service = new FuelConsumptionService(config);

        // Act
        var result = service.GetFuelConsumption(resistancePower);

        // Assert
        Assert.Equal(expectedResult, result, precision: 2);
    }

    [Fact]
    public void GetFuelConsumption_ZeroResistancePower_ReturnsZero()
    {
        // Arrange
        var config = new FuelConsumptionServiceConfiguration
        {
            PropulsionEfficiencyItems = new[] { new PropulsionEfficiencyItem(10, 0.7) },
            FuelConversionFactorItems = new[] { new FuelConversionFactorItem(75, 0.20) },
            AssumedEngineLoadPercent = 75
        };
        var service = new FuelConsumptionService(config);

        // Act
        var result = service.GetFuelConsumption(0);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetFuelConsumption_NegativeResistancePower_ReturnsNegativeValue()
    {
        // Arrange
        var config = new FuelConsumptionServiceConfiguration
        {
            PropulsionEfficiencyItems = new[] { new PropulsionEfficiencyItem(10, 0.7) },
            FuelConversionFactorItems = new[] { new FuelConversionFactorItem(75, 0.20) },
            AssumedEngineLoadPercent = 75
        };
        var service = new FuelConsumptionService(config);

        // Act
        var result = service.GetFuelConsumption(-1000);

        // Assert
        // Service doesn't validate negative input, it calculates negative fuel consumption
        Assert.True(result < 0);
        Assert.Equal(-285.71, result, precision: 2);
    }

    [Fact]
    public void GetFuelConsumption_ExtremelyHighResistancePower_ReturnsCorrectValue()
    {
        // Arrange
        var config = new FuelConsumptionServiceConfiguration
        {
            PropulsionEfficiencyItems = new[] { new PropulsionEfficiencyItem(10, 0.7) },
            FuelConversionFactorItems = new[] { new FuelConversionFactorItem(75, 0.20) },
            AssumedEngineLoadPercent = 75
        };
        var service = new FuelConsumptionService(config);

        // Act
        var result = service.GetFuelConsumption(1000000); // 1 million kW

        // Assert
        Assert.Equal(285714.29, result, precision: 2);
    }

    [Fact]
    public void GetFuelConsumption_EmptyPropulsionEfficiencyItems_ThrowsException()
    {
        // Arrange
        var config = new FuelConsumptionServiceConfiguration
        {
            PropulsionEfficiencyItems = Array.Empty<PropulsionEfficiencyItem>(),
            FuelConversionFactorItems = new[] { new FuelConversionFactorItem(75, 0.20) },
            AssumedEngineLoadPercent = 75
        };
        var service = new FuelConsumptionService(config);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.GetFuelConsumption(1000));
    }

    [Fact]
    public void GetFuelConsumption_EmptyFuelConversionFactorItems_ThrowsException()
    {
        // Arrange
        var config = new FuelConsumptionServiceConfiguration
        {
            PropulsionEfficiencyItems = new[] { new PropulsionEfficiencyItem(10, 0.7) },
            FuelConversionFactorItems = Array.Empty<FuelConversionFactorItem>(),
            AssumedEngineLoadPercent = 75
        };
        var service = new FuelConsumptionService(config);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.GetFuelConsumption(1000));
    }

    [Fact]
    public void GetFuelConsumption_MismatchedEngineLoadPercent_ThrowsException()
    {
        // Arrange
        var config = new FuelConsumptionServiceConfiguration
        {
            PropulsionEfficiencyItems = new[] { new PropulsionEfficiencyItem(10, 0.7) },
            FuelConversionFactorItems = new[]
            {
                new FuelConversionFactorItem(50, 0.18),
                new FuelConversionFactorItem(100, 0.22)
            },
            AssumedEngineLoadPercent = 75 // This doesn't match any FuelConversionFactorItem
        };
        var service = new FuelConsumptionService(config);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.GetFuelConsumption(1000));
    }
}
