using VoyageEnergyAdvisor.Core.Services.CostCalculationService;
using VoyageEnergyAdvisor.Core.Services.CostCalculationService.Models;
using Xunit;

namespace VoyageEnergyAdvisorService.Test;

public class CostCalculationServiceTests
{
    [Theory]
    [InlineData(0.85)]
    [InlineData(1.20)]
    [InlineData(0.50)]
    [InlineData(2.75)]
    public void GetFuelPricePerKg_ValidConfiguration_ReturnsConfiguredPrice(double expectedPrice)
    {
        // Arrange
        var config = new CostCalculationServiceConfiguration
        {
            FuelPricePerKg = expectedPrice,
            EmissionFactorCO2PerKg = 3.15 // Not used by GetFuelPricePerKg but part of config
        };
        var service = new CostCalculationService(config);

        // Act
        var result = service.GetFuelPricePerKg();

        // Assert
        Assert.Equal(expectedPrice, result);
    }

    [Fact]
    public void Constructor_ValidConfiguration_InitializesSuccessfully()
    {
        // Arrange
        var config = new CostCalculationServiceConfiguration
        {
            FuelPricePerKg = 1.50,
            EmissionFactorCO2PerKg = 3.15
        };

        // Act
        var service = new CostCalculationService(config);

        // Assert
        Assert.NotNull(service);
        var price = service.GetFuelPricePerKg();
        Assert.Equal(1.50, price);
    }

    [Fact]
    public void GetFuelPricePerKg_ZeroPrice_ReturnsZero()
    {
        // Arrange
        var config = new CostCalculationServiceConfiguration
        {
            FuelPricePerKg = 0,
            EmissionFactorCO2PerKg = 3.15
        };
        var service = new CostCalculationService(config);

        // Act
        var result = service.GetFuelPricePerKg();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetFuelPricePerKg_NegativePrice_ReturnsNegativeValue()
    {
        // Arrange
        var config = new CostCalculationServiceConfiguration
        {
            FuelPricePerKg = -0.50,
            EmissionFactorCO2PerKg = 3.15
        };
        var service = new CostCalculationService(config);

        // Act
        var result = service.GetFuelPricePerKg();

        // Assert
        // Service doesn't validate negative prices, it returns whatever is configured
        Assert.Equal(-0.50, result);
    }

    [Fact]
    public void GetFuelPricePerKg_ExtremelyHighPrice_ReturnsCorrectValue()
    {
        // Arrange
        var config = new CostCalculationServiceConfiguration
        {
            FuelPricePerKg = 999999.99,
            EmissionFactorCO2PerKg = 3.15
        };
        var service = new CostCalculationService(config);

        // Act
        var result = service.GetFuelPricePerKg();

        // Assert
        Assert.Equal(999999.99, result);
    }

    [Theory]
    [InlineData(0.123456789)]
    [InlineData(1.111111111)]
    [InlineData(2.999999999)]
    public void GetFuelPricePerKg_DecimalPrecision_ReturnsExactValue(double price)
    {
        // Arrange
        var config = new CostCalculationServiceConfiguration
        {
            FuelPricePerKg = price,
            EmissionFactorCO2PerKg = 3.15
        };
        var service = new CostCalculationService(config);

        // Act
        var result = service.GetFuelPricePerKg();

        // Assert
        Assert.Equal(price, result);
    }

    [Fact]
    public void Constructor_NullConfiguration_ThrowsNullReferenceException()
    {
        // Arrange
        CostCalculationServiceConfiguration config = null!;

        // Act & Assert
        // Service constructor doesn't validate null, so accessing _config.FuelPricePerKg will throw
        var service = new CostCalculationService(config);
        Assert.Throws<NullReferenceException>(() => service.GetFuelPricePerKg());
    }
}
