using System.Globalization;
using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Services.Calculation;

/// <summary>
/// Why Level 1 rejected the candidates it rejected, and the one sentence the user is shown when
/// nothing survived (QA-C-1).
///
/// This is a **presentation** concern, not optimization: it decides what a user is told to change.
/// It lives beside <see cref="Level1OptimizationService"/> rather than inside it so the wording and
/// the precedence can be read, reviewed and tested without opening the candidate loop.
///
/// The precedence in <see cref="ExplainFor"/> is load-bearing and ordered by how likely the cause is
/// something the user just typed. Changing that order changes which advice the user receives —
/// <c>Level1RejectionDiagnosticsTests</c> pins every branch by exact message.
/// </summary>
internal sealed class Level1RejectionTally
{
    /// <summary>ME=0 in Transit, hotel not coverable by SG+AE, idle AE…</summary>
    public int Structural;

    /// <summary>ME deficit beyond PTI capacity, or aux cannot feed the PTI motor.</summary>
    public int PtiAssist;

    /// <summary>AE above 90% load.</summary>
    public int AuxOverloaded;

    /// <summary>ME cannot carry its assigned load.</summary>
    public int InsufficientPower;

    /// <summary>Battery's propulsion band does not fit through PTI.</summary>
    public int BatteryPtiGate;

    /// <summary>Best PTI headroom seen across the gated candidates — quoted back to the user.</summary>
    public double BestAvailablePtiKw;

    public string ExplainFor(OperationalMode mode, CalculatorInput input, BatteryL1Adjustment? battery)
    {
        if (BatteryPtiGate > 0 && battery is not null)
        {
            // Invariant culture: the UI is English, the server may run under any locale
            var required = battery.PropulsionPeakShavingKw.ToString("0.#", CultureInfo.InvariantCulture);
            var available = BestAvailablePtiKw.ToString("0.#", CultureInfo.InvariantCulture);
            var configured = input.MaxPtiPerEngineKw.ToString("0.#", CultureInfo.InvariantCulture);
            return $"the battery needs {required} kW of PTI capacity to shave propulsion peaks in "
                 + $"{mode} mode, but only {available} kW is available. Increase the PTI capacity "
                 + $"per main engine (currently {configured} kW), reduce the battery "
                 + $"power, or clear the PTI field to model the battery at switchboard level only.";
        }

        if (PtiAssist > 0 || InsufficientPower > 0)
            return $"the installed engines cannot carry the {mode} demand. Increase engine capacity or "
                 + "engine count, or reduce the propulsion/hotel power for this mode.";

        if (AuxOverloaded > 0)
        {
            // Diesel-electric plant: the AEs carry propulsion too, so name that lever as well.
            // The MeCount >= 1 wording below is pinned by exact message — do not touch it.
            if (input.MeCount == 0)
                return $"the auxiliary engines would run above 90% load carrying the whole diesel-electric "
                     + $"demand in {mode} mode. Increase auxiliary engine capacity or count, or reduce "
                     + "the propulsion or hotel/mission power.";

            return $"the auxiliary engines would run above 90% load in {mode} mode. Increase auxiliary "
                 + "engine capacity or count, or reduce the hotel/mission power.";
        }

        return $"no engine configuration can cover the {mode} demand. Check the engine capacities, "
             + "engine counts and the power demands for this mode.";
    }
}
