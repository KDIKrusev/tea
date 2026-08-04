using KSailCalc.Api.Models.Enums;
using KSailCalc.Api.Services.Helpers;

namespace KSailCalc.Api.Models.Domain;

/// <summary>
/// The SFOC curves one calculation needs, resolved once and then read synchronously.
///
/// Only two curves ever exist on any path: the main-engine curve (also used for the shaft
/// generator — <see cref="GeneratorTypeExtensions.ToEngineCategory"/> maps SG to
/// <see cref="EngineCategory.Main"/>) and the auxiliary-engine curve. Both arrive already filtered
/// to working points (Load &gt; 0) and sorted ascending, which is the precondition
/// <see cref="SfocInterpolationHelper"/> documents.
///
/// An empty curve is a valid state, not an error: it makes interpolation return the documented
/// fallback SFOC, exactly as a missing engine record did before the curves were pre-resolved.
/// </summary>
public sealed class EngineFuelCurves
{
    private static readonly List<SfocDataPoint> Empty = new();

    public EngineFuelCurves(List<SfocDataPoint> main, List<SfocDataPoint> auxiliary)
    {
        Main = main;
        Auxiliary = auxiliary;
    }

    /// <summary>Main-engine working points, ascending by load. Also serves the shaft generator.</summary>
    public List<SfocDataPoint> Main { get; }

    /// <summary>Auxiliary-engine working points, ascending by load.</summary>
    public List<SfocDataPoint> Auxiliary { get; }

    /// <summary>
    /// The curve for an engine category. An unrecognised category yields an empty curve rather than
    /// throwing — matching the pre-refactor lookup, which left the data null and fell back.
    /// </summary>
    public List<SfocDataPoint> For(EngineCategory category) => category switch
    {
        EngineCategory.Main => Main,
        EngineCategory.Auxiliary => Auxiliary,
        _ => Empty
    };

    /// <summary>
    /// SFOC [g/kWh] at the given load fraction. The parameter is <c>decimal</c> on purpose: callers
    /// hold a <c>double</c> load and cast at the call site, exactly as they did when this went
    /// through the async service — moving that cast would change the rounding.
    /// </summary>
    public double Sfoc(decimal loadPercentage, EngineCategory category)
        => SfocInterpolationHelper.Interpolate(loadPercentage, For(category));
}
