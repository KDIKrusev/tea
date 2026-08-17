using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Tests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace KSailCalc.Tests.Battery;

/// <summary>
/// AC6: the appsettings.json "BatterySettings" section must equal the Excel reference constants.
/// Binding onto a default-populated list overwrites by index, so code defaults and JSON can drift
/// silently — this test pins the effective (bound) configuration.
/// Path convention follows AppDataAggregationServiceParametricTests (project root = 4 levels up).
/// </summary>
public class BatterySettingsConfigurationTests
{
    [Fact]
    public void AppSettings_BatterySettingsSection_MatchesExcelReferenceConstants()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var config = new ConfigurationBuilder()
            .SetBasePath(projectRoot)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var settings = new BatterySettings();
        config.GetSection("BatterySettings").Bind(settings);

        settings.PtiLossFactor.Should().Be(0.05);
        settings.ChargeEfficiency.Should().Be(0.97);
        settings.DischargeEfficiency.Should().Be(0.97);
        settings.ElectricMotorEfficiency.Should().Be(0.965);

        // Others added by Epic E2 (D-BI4): mirrors Mission, right after it in the queue.
        settings.LoadPriorities.Should().HaveCount(6);
        settings.LoadPriorities.Select(p => p.Load).Should().ContainInOrder(
            BatteryLoadType.DpReserve, BatteryLoadType.DpDemand, BatteryLoadType.Mission,
            BatteryLoadType.Others, BatteryLoadType.Propulsion, BatteryLoadType.Hotel);

        var byLoad = settings.LoadPriorities.ToDictionary(p => p.Load);
        byLoad[BatteryLoadType.DpReserve].Function.Should().Be(BatteryFunction.Reserve);
        byLoad[BatteryLoadType.DpReserve].CoverageFactor.Should().Be(1.00);
        byLoad[BatteryLoadType.DpDemand].CoverageFactor.Should().Be(0.50);
        byLoad[BatteryLoadType.Mission].CoverageFactor.Should().Be(0.50);
        byLoad[BatteryLoadType.Others].CoverageFactor.Should().Be(0.50);
        byLoad[BatteryLoadType.Propulsion].CoverageFactor.Should().Be(0.35);
        byLoad[BatteryLoadType.Propulsion].VariationFactor.Should().Be(0.05);
        byLoad[BatteryLoadType.Hotel].CoverageFactor.Should().Be(0.05);
        byLoad[BatteryLoadType.Hotel].VariationFactor.Should().Be(0.02);
        settings.LoadPriorities.Skip(1).Select(p => p.Function)
            .Should().AllBeEquivalentTo(BatteryFunction.PeakShaving);
    }
}
