# Epic E1 — Diesel-Electric Plant (0 Main Engines): Architecture Design

**Architect:** Winston (BMAD) · **Date:** 2026-08-13 · **Status:** Approved for story cutting
**Inputs:** PRD v1.0 (`03-brownfield-prd.md`), decisions D-DE1..D-DE5, analyst brief §Request 1.

The design goal is stated by CR1 and repeated here as the prime directive: **every behaviour
change is reachable only when `input.MeCount == 0`.** For any input with `MeCount ≥ 1` the
executed instruction stream must be identical to today's — goldens 01–35 stay frozen by
construction, not by luck.

---

## 1. The gate

One predicate, one place. A static helper, **not** a property on `CalculatorInput`:

```
Services/Helpers/PlantShape.cs   (new, static)
  internal static bool IsDieselElectric(CalculatorInput input) => input.MeCount == 0;
```

**Why not a computed property on the model:** `CalculatorInput` already carries serializable
computed members and is bound from JSON; a new public get-only property risks appearing in any
response that echoes the input, which is exactly the class of silent JSON drift the frozen goldens
exist to catch. A helper in `Services/Helpers` is invisible to serialization. (Verified risk, not
hypothetical — do not "improve" this into a property during review.)

## 2. Distribution: the new branch in `Level1CandidateBuilder.TryDistribute`

Placement: a single early branch at the top of `TryDistribute`
(`Level1CandidateBuilder.cs:54`), before the Transit/Maneuvering ME=0 rejection at lines 59–60.
The existing body remains untouched below it — the diesel-mechanic path does not gain a single
new conditional beyond the gate itself.

```
if (PlantShape.IsDieselElectric(input))
{
    // Only pure-AE states exist. SG needs a shaft; ME count is 0 by definition.
    if (combo.ActiveMeCount > 0 || combo.SgEnabled) return null;

    var demand = hotel + propulsion * (1 + electricLossFactor);   // FR2, D-DE2
    var aeCap  = combo.ActiveAeCount * input.AeCapacityPerEngine;
    var aePow  = Math.Min(demand, aeCap);
    if (aePow < demand - PlantLimits.PowerToleranceKw) return null;   // tallied Structural
    if (combo.ActiveAeCount > 0 && aePow == 0) return null;           // idle-AE rule, as today
    if (combo.ActiveAeCount == 0 && demand > PlantLimits.PowerToleranceKw) return null;

    return combo with
    {
        MePowerKw = 0, SgPowerKw = 0, AePowerKw = aePow,
        MeLoadPercent = 0,
        AeLoadPercent = CalculationHelpers.LoadPercent(aePow, aeCap)
    };
}
```

Notes the implementer must not lose:

- **`Generate` is untouched.** It already emits `me = 0..MeCount` → at MeCount = 0 the ME axis
  collapses on its own; SG=true variants die in the branch above. Survivor space is effectively
  "how many AEs run" (1..N).
- **The 90 % AE cap** (`Level1OptimizationService.cs:124`) stays downstream and now polices the
  whole electric load — that is decision D-DE4, not an accident.
- **PTI assist** needs no change: `ptiCapacity = MeCount × MaxPti = 0`
  (`Level1PtiAssist.cs:34`), and validation (FR4) refuses PTI input at 0 ME anyway.
- The structural guard at `Level1OptimizationService.cs:137-141` (`ActiveMeCount == 0 &&
  MePowerKw > 0`) never fires — the branch sets `MePowerKw = 0`. Leave the guard in place.

## 3. Loss factor: config surface and threading

- **Key:** `Calculator:ElectricPropulsionLossFactor`, bound on `CalculatorSettings`
  (same pattern as `VesselVariations` / `DefaultVesselVariationKw`). Default **0** (D-DE2).
- **Threading:** `Level1OptimizationService` receives `IOptions<CalculatorSettings>` alongside the
  existing `IOptions<BatterySettings>` and passes the factor into `TryDistribute` as a parameter
  (the builder is static and stays pure — no options access inside).
- **Semantic line in the sand:** the factor applies at the *distribution* step only. The battery
  cascade, `ResolveDemand`, Power Demands and every displayed demand figure keep the user-entered
  switchboard values. Grossing up earlier would silently inflate the cascade's Propulsion H
  (5 % of a grossed-up average) and the client's hand-checks against his workbook.
- DP at 0 ME: `RequiredDPPowerKW` flows through the same branch (thrust is electric through the
  same converter chain), so the factor applies there too — with default 0 this is a no-op.

## 4. Validation choreography (`ValidationService`)

Golden constraint: the 400-response **order** is pinned (`ValidationService.cs:19-22`). Rules:
existing messages keep their relative order; DE-only messages fire only at MeCount = 0 (no golden
scenario has MeCount = 0, so no golden can change); the one *edited* rule is `MeCount < 1` →
`MeCount < 0`, whose message no golden pins (unit tests pin it — update those, that is allowed).

In `ValidatePlantAndFinancials` (same slice, appended at its end to preserve order):

| Condition | Message (actionable style, NFR2) |
|---|---|
| `MeCount < 0` | "Number of main engines cannot be negative" |
| `MeCount >= 1` (unchanged) | existing ME capacity / engine-type requirements |
| `MeCount == 0 && SgCapacityPerEngine > 0` | "Shaft generators require a main engine. Set shaft generator capacity to 0 for a diesel-electric plant." |
| `MeCount == 0 && MaxPtiPerEngineKw > 0` | "PTI requires a main engine shaft. Clear the PTI capacity for a diesel-electric plant." |
| `MeCount == 0` | skip `MeCapacityPerEngine`/`MainEngineTypeId` requirements (`ValidationService.cs:63-64`, `:128-129` become conditional) |

In `ValidateSystemCapacity`, a diesel-electric branch replaces the ME-utilization arithmetic:

- capacity check: `EffectivePropulsionPower + TransitHotelPowerKW > TotalAeCapacity` →
  Error-severity warning: "Auxiliary engine capacity cannot carry propulsion and hotel load.
  Consider reducing propulsion power, decreasing sea margin, reducing hotel/mission load or
  increasing auxiliary engine capacity." (mirror of the scenario-17 text — FR4).
- the existing `meUtilization` / `hotel-load` / `shaft-capacity` blocks are already safe at 0
  (guarded divisions), but must be short-circuited for MeCount = 0 so the user gets the one
  correct message, not three misleading ones.

`Level1RejectionTally.ExplainFor` gains a diesel-electric sentence for the no-survivor case
(e.g. everything rejected by the 90 % cap): name the AE count/capacity and the 90 % ceiling.

## 5. What is explicitly NOT touched (regression contract)

`BatteryAllocationService`, `BatteryModeAdapter`, `ModePipelineRunner` (two worlds),
`SelectBaseline`, `SfocService`, `Level1PtiAssist`, `PowerDemandsBuilder`,
`Level2*`/`Level3*` services, all Results builders. If a story finds itself editing one of these
for Epic 1, stop and come back to this document — the design has been violated.

## 6. Client design (story DE-C)

- `VALIDATION_LIMITS.COUNT` splits: `ME_COUNT: { MIN: 0 }`, `AE_COUNT: { MIN: 1 }`
  (`defaults.constants.ts:16-21`); schema + template `min` attributes follow
  (`vessel-form.schema.ts:22`, `engine-config-section.component.html:96`).
- At `meCount = 0` the ME type select, ME capacity, SG capacity and PTI controls are disabled
  with cleared values (visible affordance, D-DE3 client mirror). Re-enabling restores catalog
  prefill via the existing cascade; the `FormEditTrackerService` baseline must be re-set on both
  transitions (the finding-4/5/6 family lives exactly here — single emission, no cascade loops).
- Results panels need no logic change (guards verified in the brief); DOM specs pin: ME row shows
  0, no NaN, combination labels render without "0×ME" artifacts (`baseline-panel.component.ts:59`
  already guards).
- Profile schema stays v3 (CR2); round-trip spec for `meCount: 0`.

## 7. Test plan mapped to stories

| Story | New tests |
|---|---|
| DE-A | Validation unit tests: 0 accepted, negative rejected, SG/PTI blocking, conditional ME type/capacity, AE-capacity 400 text; golden 400-order suite untouched-green |
| DE-B | `Level1CandidateBuilder` DE distribution (Transit/DP/Port, loss factor 0 and 0.05 via options); 90 % cap rejection + tally message; cascade + Benefit two-worlds at 0 ME (hand-derived numbers in the story, Increment-F style); L2 zero-SG characterization; config default test (`BatterySettingsConfigurationTests` pattern) |
| DE-C | Client DOM/unit specs: form gating, error surfacing, results at 0 ME, profile round-trip |
| DE-D | Golden scenarios 36+ (characterisation status) + cards + README + ORIENTATION note |

Every story ends with the full suite via the locked-bin workaround (CR4) and a byte-level golden
comparison.

## 8. Risks and their mitigations

1. **JSON drift via model members** — mitigated by §1 (helper, not property).
2. **400-order regression** — mitigated by append-only rule + the pinned-order suite (§4).
3. **L2 at zero SG** unproven — characterization test in DE-B *before* DE-D freezes scenarios.
4. **Client cascade re-prefill fighting the disabled fields** — the known emission-cascade family;
   DE-C touches it only through the tracker baseline, no new emission sources.
5. **Numbers without an authority** — new scenarios stay "characterisation — pending reference
   verification" until the client validates one against his workbook (D4 discipline).
