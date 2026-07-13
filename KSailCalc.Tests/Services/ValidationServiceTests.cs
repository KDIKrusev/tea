using FluentAssertions;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services;
using KSailCalc.Tests.TestHelpers;

namespace KSailCalc.Tests.Services;

public class ValidationServiceTests
{
    private readonly ValidationService _sut = new();

    [Fact]
    public void ValidInput_ReturnsValid()
    {
        var input = CalculatorInputBuilder.Default().Build();

        var result = _sut.ValidateInput(input);

        result.Valid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void PropulsionPowerZero_ReturnsError()
    {
        var input = CalculatorInputBuilder.Default()
            .WithPropulsionPower(0)
            .Build();

        var result = _sut.ValidateInput(input);

        result.Valid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Propulsion power"));
    }

    [Fact]
    public void SeaMarginOver100_ReturnsError()
    {
        var input = CalculatorInputBuilder.Default()
            .WithSeaMargin(101)
            .Build();

        var result = _sut.ValidateInput(input);

        result.Valid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Sea margin"));
    }

    [Fact]
    public void DpEnabled_DPHoursZero_ReturnsError()
    {
        var input = CalculatorInputBuilder.Default().Build();
        input.DpEnabled = true;
        input.DPHours = 0;
        input.DPHotelPowerKW = 300;
        input.RequiredDPPowerKW = 1200;

        var result = _sut.ValidateInput(input);

        result.Valid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("DP hours"));
    }

    [Fact]
    public void SailEnabled_NoWindSpeed_ReturnsError()
    {
        var input = CalculatorInputBuilder.Default().Build();
        input.SailEnabled = true;
        input.TrueWindSpeed = null;
        input.WindAngleRelVessel = 90;

        var result = _sut.ValidateInput(input);

        result.Valid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("wind speed"));
    }

    [Fact]
    public void MeUtilizationOver100_ReturnsInvalid()
    {
        // Set up so ME utilization exceeds 100%:
        // 1 ME x 5000 kW, propulsion = 4000, sea margin = 15% => effective = 4600
        // SG = 1000, hotel = 1000 => SG absorbs 1000 => ME total = 4600 + 1000 = 5600 > 5000
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(5000, 1)
            .WithShaftGenerators(1000)
            .WithPropulsionPower(4000)
            .WithSeaMargin(15)
            .WithTransitMode(5694, 1000)
            .Build();

        var result = _sut.ValidateInput(input);

        result.Valid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Main engine utilization"));
        result.Warnings.Should().Contain(w => w.Severity == WarningSeverity.Error);
    }
}
