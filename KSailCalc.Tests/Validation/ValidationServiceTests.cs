using KSailCalc.Api.Models;
using FluentAssertions;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Tests.TestHelpers;

namespace KSailCalc.Tests.Validation;

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

    // ── The operating profile must fit inside a year ────────────────────────────

    /// <summary>
    /// Every annual figure is a per-hour rate × these hours, so a profile that does not fit into a
    /// year overstates all of them. Found by QA scenario 14, which carried 8935 h unnoticed because
    /// nothing checked.
    /// </summary>
    private static CalculatorInput ProfileWithHours(
        double transit, double port = 0, double anchor = 0, double maneuvering = 0)
    {
        var input = CalculatorInputBuilder.Default().WithTransitMode(transit, 800).Build();
        input.PortHours = port;
        input.AnchorHours = anchor;
        input.ManeuveringHours = maneuvering;
        return input;
    }

    [Fact]
    public void AnnualHours_CountsEveryMode_NotJustTransitAndDp()
    {
        var input = ProfileWithHours(transit: 4000, port: 1200, anchor: 800, maneuvering: 400);
        input.DpEnabled = true;
        input.DPHours = 2360;

        input.AnnualHours.Should().Be(8760, "4000 + 2360 + 1200 + 800 + 400");
    }

    [Theory]
    [InlineData(5000, 0, 0, 0)]        // a typical single-mode profile
    [InlineData(5717, 2592, 451, 0)]   // 8760 exactly — a full year, still fine
    public void AProfileThatFitsInsideAYear_RaisesNoHoursWarning(
        double transit, double port, double anchor, double maneuvering)
    {
        var result = _sut.ValidateInput(ProfileWithHours(transit, port, anchor, maneuvering));

        result.Warnings.Should().NotContain(w => w.Type == "operating-hours");
    }

    [Fact]
    public void AProfileLongerThanAYear_WarnsButDoesNotBlock()
    {
        // QA scenario 14's actual numbers: 175 h over, and nobody noticed for months.
        var result = _sut.ValidateInput(ProfileWithHours(5717, port: 2592, anchor: 451, maneuvering: 175));

        var warning = result.Warnings.Should().ContainSingle(w => w.Type == "operating-hours").Subject;
        warning.Severity.Should().Be(WarningSeverity.Warning,
            "a small overrun is usually rounding across modes — inform, do not block");
        warning.Message.Should().Contain("8935").And.Contain("8760");

        result.Valid.Should().BeTrue("an advisory warning must not turn a calculable input into a 400");
    }

    [Fact]
    public void TheHoursWarningIsAppendedAfterTheCapacityWarnings()
    {
        // Warning order is what the client renders and what the golden 400-responses pin.
        var input = CalculatorInputBuilder.Default()
            .WithMainEngines(5000, 1).WithShaftGenerators(1000)
            .WithPropulsionPower(4000).WithSeaMargin(15)
            .WithTransitMode(8000, 1000).Build();
        input.PortHours = 2000; // 10 000 h total

        var result = _sut.ValidateInput(input);

        result.Warnings.Should().HaveCountGreaterThan(1);
        result.Warnings[^1].Type.Should().Be("operating-hours");
    }
}
