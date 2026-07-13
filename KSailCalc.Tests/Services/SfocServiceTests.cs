using FluentAssertions;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services;
using KSailCalc.Tests.TestHelpers;

namespace KSailCalc.Tests.Services;

public class SfocServiceTests
{
    private readonly SfocService _sut;

    public SfocServiceTests()
    {
        _sut = TestServiceFactory.Create().SfocService;
    }

    // ME SFOC curve: 0%→0, 10%→284.533, 20%→255.791, 30%→235.422, 40%→218.615, 50%→214.097, 60%→208.458, 70%→203.592, 80%→197.544, 90%→201.096, 100%→190.662
    // AE SFOC curve: 0%→0, 10%→345.288, 20%→292.729, 30%→257.287, 40%→240.613, 50%→228.148, 60%→219.784, 70%→216.409, 80%→214.753, 90%→215.417, 100%→217.341

    [Fact]
    public async Task GetSfoc_ExactDataPoint_ReturnsExactValue()
    {
        var sfoc = await _sut.GetSfocForLoadAsync(0.50m, EngineCategory.Main, 1);
        sfoc.Should().Be(214.097);
    }

    [Fact]
    public async Task GetSfoc_BetweenPoints_Interpolates()
    {
        // Midpoint between 60%(208.458) and 70%(203.592) at 62.5% = 207.2415
        var sfoc = await _sut.GetSfocForLoadAsync(0.625m, EngineCategory.Main, 1);
        sfoc.Should().BeApproximately(207.2415, 0.1);
    }

    [Fact]
    public async Task GetSfoc_BelowMinLoad_ExtrapolatesAboveFirstWorkingPoint()
    {
        // 0% load → working points filter out Load=0, extrapolates from first two points
        // First two working points: 10%=284.533, 20%=255.791
        // Slope = (255.791-284.533)/(0.20-0.10) = -287.42 per unit
        // At 0%: 284.533 + (-287.42 * -0.10) = 284.533 + 28.742 = 313.275
        // Math.Max(313.275, 284.533) = 313.275 (higher SFOC at very low load)
        var sfoc = await _sut.GetSfocForLoadAsync(0.0m, EngineCategory.Main, 1);
        sfoc.Should().BeGreaterThan(284.533, "SFOC at 0% should be higher than at 10% due to extrapolation");
    }

    [Fact]
    public async Task GetSfoc_AboveMaxLoad_ReturnsLastPointSfoc()
    {
        var sfoc = await _sut.GetSfocForLoadAsync(1.20m, EngineCategory.Main, 1);
        sfoc.Should().Be(190.662);
    }

    [Fact]
    public async Task GetSfoc_AuxiliaryEngine_ReturnsCorrectCurve()
    {
        var sfoc = await _sut.GetSfocForLoadAsync(0.50m, EngineCategory.Auxiliary, 1);
        sfoc.Should().Be(228.148);
    }

    [Fact]
    public async Task GetSfoc_UnknownEngineId_ReturnsFallback220()
    {
        var sfoc = await _sut.GetSfocForLoadAsync(0.50m, EngineCategory.Main, 999);
        sfoc.Should().Be(220.0);
    }

    [Fact]
    public async Task GetSfoc_UnknownEngineType_ReturnsFallback220()
    {
        var sfoc = await _sut.GetSfocForLoadAsync(0.50m, (EngineCategory)999, 1);
        sfoc.Should().Be(220.0);
    }
}