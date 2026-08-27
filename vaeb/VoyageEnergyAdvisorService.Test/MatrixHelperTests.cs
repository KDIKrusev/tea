using VoyageEnergyAdvisor.Core.CommonModels;
using Xunit;

namespace VoyageEnergyAdvisorService.Test;

public class MatrixHelperTests
{
    [Theory]
    [InlineData(30, 10, 0.5)]
    [InlineData(30, 999, 10)]
    [InlineData(60, 15, 0.7)]
    [InlineData(43, 20, 0.9)]
    [InlineData(45, 13, 0.6)]
    public void GetClosestValue_ShouldReturnCorrectValue(double xValue, double yValue, double expectedValue)
    {
        // Arrange
        var modelItems = new List<MatrixCell>
        {
            new MatrixCell(30, 10, 0.5),
            new MatrixCell(30, 1000, 10),
            new MatrixCell(60, 15, 0.7),
            new MatrixCell(45, 12, 0.6),
            new MatrixCell(45, 20, 0.9),
            new MatrixCell(45, 12, 0.6),
            new MatrixCell(45, 20, 0.9),
        };

        // Act
        double result = modelItems.GetClosestValue(xValue, yValue);

        // Assert
        Assert.Equal(expectedValue, result, precision: 2);
    }
}