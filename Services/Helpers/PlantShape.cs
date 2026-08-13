using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Helpers;

/// <summary>
/// The one question the diesel-electric epic hangs on, asked in one place.
///
/// Deliberately a static helper and NOT a computed property on <see cref="CalculatorInput"/>:
/// the input model is JSON-bound and its public members can leak into any response that echoes
/// the input — exactly the silent drift the frozen goldens exist to catch (architecture §1,
/// docs/client-requests-2026-08/04-architecture-diesel-electric.md).
/// </summary>
internal static class PlantShape
{
    /// <summary>
    /// A plant with no main engines installed: propulsion is electric (thrusters/pods) and the
    /// auxiliary engines carry the entire demand. Decision D-DE1 — this is about INSTALLED
    /// engines; "MEs switched off per mode" on an ME-equipped vessel is out of scope.
    /// </summary>
    internal static bool IsDieselElectric(CalculatorInput input) => input.MeCount == 0;
}
