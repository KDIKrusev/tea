using FluentAssertions;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;
using KSailCalc.Tests.TestHelpers;

namespace KSailCalc.Tests.Calculation;

/// <summary>
/// The SFOC contract: resolve an engine's curve once, then interpolate against it.
///
/// These assertions used to go through <c>GetSfocForLoadAsync</c>, a per-load lookup that has since
/// been deleted — nothing called it, and it swallowed failures into a silent fallback. The behaviour
/// it described still matters, so the same cases now run against the resolved curve.
/// </summary>
public class SfocServiceTests
{
    // ME SFOC curve: 0%→0, 10%→284.533, 20%→255.791, 30%→235.422, 40%→218.615, 50%→214.097, 60%→208.458, 70%→203.592, 80%→197.544, 90%→201.096, 100%→190.662
    // AE SFOC curve: 0%→0, 10%→345.288, 20%→292.729, 30%→257.287, 40%→240.613, 50%→228.148, 60%→219.784, 70%→216.409, 80%→214.753, 90%→215.417, 100%→217.341

    private static EngineFuelCurves CurvesFor(int mainEngineId = 1, int auxEngineId = 1)
    {
        var factory = TestServiceFactory.Create();
        var input = CalculatorInputBuilder.Default().Build();
        input.MainEngineTypeId = mainEngineId;
        input.AuxEngineTypeId = auxEngineId;
        return factory.CurvesFor(input);
    }

    [Fact]
    public void ExactDataPoint_ReturnsExactValue()
    {
        CurvesFor().Sfoc(0.50m, EngineCategory.Main).Should().Be(214.097);
    }

    [Fact]
    public void BetweenPoints_Interpolates()
    {
        // Midpoint between 60%(208.458) and 70%(203.592) at 62.5% = 207.2415
        CurvesFor().Sfoc(0.625m, EngineCategory.Main).Should().BeApproximately(207.2415, 0.1);
    }

    [Fact]
    public void BelowMinLoad_ExtrapolatesAboveTheFirstWorkingPoint()
    {
        // 0% load → working points filter out Load=0, extrapolates from first two points
        // First two working points: 10%=284.533, 20%=255.791
        // Slope = (255.791-284.533)/(0.20-0.10) = -287.42 per unit
        // At 0%: 284.533 + (-287.42 * -0.10) = 313.275, and Max(313.275, 284.533) keeps it
        CurvesFor().Sfoc(0.0m, EngineCategory.Main)
            .Should().BeGreaterThan(284.533, "engines are less efficient at very low load, not more");
    }

    [Fact]
    public void AboveMaxLoad_ReturnsTheLastPoint()
    {
        CurvesFor().Sfoc(1.20m, EngineCategory.Main).Should().Be(190.662);
    }

    [Fact]
    public void TheAuxiliaryCurveIsSeparateFromTheMainOne()
    {
        var curves = CurvesFor();

        curves.Sfoc(0.50m, EngineCategory.Auxiliary).Should().Be(228.148);
        curves.Sfoc(0.50m, EngineCategory.Main).Should().Be(214.097, "the two curves must not be confused");
    }

    // ── Missing data falls back rather than throwing ────────────────────────────

    [Fact]
    public void AnUnknownEngineId_YieldsAnEmptyCurveAndTheFallbackSfoc()
    {
        CurvesFor(mainEngineId: 999).Sfoc(0.50m, EngineCategory.Main).Should().Be(220.0);
    }

    [Fact]
    public void AnUnknownEngineCategory_AlsoFallsBack()
    {
        CurvesFor().Sfoc(0.50m, (EngineCategory)999).Should().Be(220.0);
    }
}
