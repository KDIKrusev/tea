using FluentAssertions;
using KSailCalc.Api.Services;
using KSailCalc.Tests.TestHelpers;

namespace KSailCalc.Tests.Services;

public class SailContributionServiceTests
{
    private readonly SailContributionService _sut;

    public SailContributionServiceTests()
    {
        _sut = TestServiceFactory.Create(enableSailData: true).SailContributionService;
    }

    // === CalculateApparentWind ===
    // Convention: 0° = tailwind (stern→bow), 180° = headwind

    [Fact]
    public void ApparentWind_Tailwind_WeakApparentSpeed()
    {
        // Vessel and wind move same direction → apparent speed ≈ trueWind - vessel
        var result = _sut.CalculateApparentWind(trueWindSpeed: 10, windAngleRelVessel: 0, vesselSpeedKnots: 14);

        result.ApparentWindSpeed.Should().BeApproximately(2.8, 0.2);  // 10 - 7.2
        result.ApparentWindAngle.Should().BeApproximately(0, 1);
    }

    [Fact]
    public void ApparentWind_Headwind_StrongApparentSpeed()
    {
        // Vessel moves into wind → apparent speed ≈ trueWind + vessel
        var result = _sut.CalculateApparentWind(trueWindSpeed: 10, windAngleRelVessel: 180, vesselSpeedKnots: 14);

        result.ApparentWindSpeed.Should().BeApproximately(17.2, 0.2);  // 10 + 7.2
        result.ApparentWindAngle.Should().BeInRange(178, 180);
    }

    [Fact]
    public void ApparentWind_BeamWind_ModerateApparentSpeed()
    {
        var result = _sut.CalculateApparentWind(trueWindSpeed: 10, windAngleRelVessel: 90, vesselSpeedKnots: 14);

        result.ApparentWindSpeed.Should().BeInRange(8, 15);
        result.ApparentWindAngle.Should().BeInRange(30, 70);
    }

    [Fact]
    public void ApparentWind_ZeroVesselSpeed_EqualsTrueWind()
    {
        var result = _sut.CalculateApparentWind(trueWindSpeed: 10, windAngleRelVessel: 45, vesselSpeedKnots: 0);

        result.ApparentWindSpeed.Should().BeApproximately(10, 0.1);
        result.ApparentWindAngle.Should().BeApproximately(45, 0.1);
    }

    [Fact]
    public void ApparentWind_AlwaysNormalized0To360()
    {
        var result = _sut.CalculateApparentWind(trueWindSpeed: 10, windAngleRelVessel: 350, vesselSpeedKnots: 14);
        result.ApparentWindAngle.Should().BeInRange(0, 360);
    }

    // === CalculateSailContributionAsync ===

    [Fact]
    public async Task SailContribution_BeamWind_ReducesPropulsion()
    {
        var result = await _sut.CalculateSailContributionAsync(
            trueWindSpeed: 10, windAngleRelVessel: 90,
            vesselSpeedKnots: 14, transitPropulsionBeforeKw: 5000);

        result.Should().NotBeNull();
        result!.SailForceKn.Should().BeGreaterThan(0);
        result.SailPowerKw.Should().BeGreaterThan(0);
        result.TransitPropulsionAfterKw.Should().BeLessThan(5000);
        result.SailSavingsPercent.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SailContribution_PropulsionNeverBelowZero()
    {
        // Strong wind, very low propulsion → clamp to 0
        var result = await _sut.CalculateSailContributionAsync(
            trueWindSpeed: 20, windAngleRelVessel: 90,
            vesselSpeedKnots: 14, transitPropulsionBeforeKw: 50);

        result.Should().NotBeNull();
        result!.TransitPropulsionAfterKw.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task SailContribution_ReturnsAllFields()
    {
        var result = await _sut.CalculateSailContributionAsync(
            trueWindSpeed: 10, windAngleRelVessel: 90,
            vesselSpeedKnots: 14, transitPropulsionBeforeKw: 5000);

        result.Should().NotBeNull();
        result!.TrueWindSpeed.Should().Be(10);
        result.WindAngleRelVessel.Should().Be(90);
        result.ApparentWindSpeed.Should().BeGreaterThan(0);
        result.ApparentWindAngle.Should().BeInRange(0, 360);
        result.TransitPropulsionBeforeKw.Should().Be(5000);
    }

    [Fact]
    public async Task SailContribution_NoSailData_ReturnsNull()
    {
        // Factory without sail data
        var sut = TestServiceFactory.Create(enableSailData: false).SailContributionService;

        var result = await sut.CalculateSailContributionAsync(
            trueWindSpeed: 10, windAngleRelVessel: 90,
            vesselSpeedKnots: 14, transitPropulsionBeforeKw: 5000);

        result.Should().BeNull();
    }

    // === GetClosestSailContributionForce ===

    [Fact]
    public void ForceLookup_ExactMatch_ReturnsExactForce()
    {
        var force = _sut.GetClosestSailContributionForce(TestServiceFactory.DefaultSailItems, 90, 10);
        force.Should().Be(80.0);
    }

    [Fact]
    public void ForceLookup_NearestNeighbor_FindsClosest()
    {
        // 88° at 10 m/s → closest is 90° at 10 m/s (distance=2)
        var force = _sut.GetClosestSailContributionForce(TestServiceFactory.DefaultSailItems, 88, 10);
        force.Should().Be(80.0);
    }
}