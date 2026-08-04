# Backend Refactoring — Architecture Design

<!-- Author: Winston (Architect) · Date: 2026-08-03 -->
<!-- Source for stories: docs/stories/brownfield-refactor-{a..e}-*.md -->
<!-- Scope: KSailCalc.Api backend only. No client changes. No calculation changes. -->

## 1. Why

`Services/CalculatorService.cs` is 534 lines carrying **seven** responsibilities. Its `#region`
markers map almost 1:1 onto the classes it should have been split into. The effect is not
"too many lines" — it is that a change to the tier pricing, the battery adaptation, the mode
aggregation and the presentation model all land in the same file, and none of them can be read or
debugged in isolation.

Secondary findings (duplicated arithmetic in the Level 1 hot loop, per-call re-sorting of SFOC
curves, viral `async` with no I/O behind it) come from the same root: knowledge that belongs to one
component is spread across three.

This document defines the target structure and the **invariants** that make the whole epic provably
behaviour-preserving.

## 2. Non-goals

Explicitly **out of scope** for this epic. Each is a separate backlog item, tracked in §7.

- Any change to a calculation formula, constant, rounding, or ordering rule.
- Any change to the wire contract (`CalculatorInput`, `AllVariantsCalculationResult`, `Level*Details`).
- Any client (`cl/`) change. `ng build` is not part of any story's Definition of Done here.
- Any change to error/exception *behaviour* (see §7.1).
- Any database, schema or configuration change.
- Fixing the open `baselineIndex` restore finding (see §7.5) — deliberately after this epic.

## 3. Invariants (binding on every story)

These are the contract that makes "refactoring only" verifiable rather than asserted.

**I1 — Golden snapshots are frozen.**
`KSailCalc.Tests/Golden/Expected/` holds 18 approved snapshots for the 18 QA scenarios.
`GOLDEN_UPDATE=1` is **forbidden** for the entire epic. `git status` must show zero modified files
under `Expected/` at every commit. A story that cannot satisfy this is not a refactoring story —
stop and escalate to the Architect.

**I2 — Wire contract frozen.**
No property in the API request/response models is added, removed, renamed or retyped. Serialization
shape is unchanged, therefore the client needs no change.

**I3 — Public service surface may change; test infrastructure follows.**
Constructor signatures and internal service interfaces MAY change (that is the point).
`KSailCalc.Tests/Golden/GoldenScenarioHost.cs` composes `CalculatorService` by hand and will need
updating — that is an **allowed and expected** edit. Editing the *host* is not editing the
*snapshots*: I1 still applies.

**I4 — One story, one commit, independently revertable.**
Full suite green (269 tests) before the commit. No story depends on a later one.

**I5 — Test-run workaround.**
The API keeps `bin\` locked while running, so the suite is run with a redirected output path:

```
dotnet test KSailCalc.Tests\KSailCalc.Tests.csproj -p:BaseOutputPath=<temp>\
```

Three tests (`BatterySettingsConfigurationTests`, the two `AppDataAggregationServiceParametricTests`)
load `appsettings.json` by walking **four levels up from the test assembly**. With a redirected
output the assembly sits at `<temp>\Debug\net10.0\`, so a copy of the repo's `appsettings.json` must
be placed two levels above `<temp>`. Without it those three fail with `FileNotFoundException` while
everything else passes — do not mistake that for a regression, and re-copy after any edit to
`appsettings.json`.

`GoldenPaths` resolves the repo root from `[CallerFilePath]`, so the golden suite itself works from
any output directory.

**Verified baseline (2026-08-03, before any refactoring): 269/269 passing, 360 ms.**
This is the number every story's Task 0 must reproduce before starting.

**I6 — Behaviour-preserving means numerically identical.**
Not "within tolerance". The golden snapshots compare exact serialized values.

## 4. Target structure

```
CalculatorController
└─ CalculatorService                     ~110 lines — orchestration only
   ├─ EngineFuelCurves                   resolves the ME/AE SFOC curves ONCE per calculation
   ├─ ModePipelineRunner                 L1 → L2 → L3 + battery, for ONE mode
   │   ├─ Level1OptimizationService      sync, pure
   │   ├─ Level2OptimizationService      sync, pure
   │   ├─ Level3DrcService               sync, pure
   │   └─ BatteryModeAdapter             ← CalculatorService region "Battery"
   ├─ SavingsAggregator     (static)     ← region "FOC & Savings Aggregation"
   ├─ TierResultBuilder     (static)     ← region "Result Building" + financials + CO2
   └─ PowerDemandsBuilder   (static)     ← region "Power Demands"
```

Deliberate choice: `SavingsAggregator`, `TierResultBuilder` and `PowerDemandsBuilder` are
`internal static` classes over pure data — **no interfaces**. An interface is warranted only where an
implementation would realistically be substituted or mocked; these never will be, and `CalculatorService`
already has six constructor dependencies. `ModePipelineRunner` is DI-registered because it holds the
three level services.

### 4.1 Component contracts

| Component | Input | Output | Purity |
|---|---|---|---|
| `PowerDemandsBuilder` | mode results, input, sail result | `PowerDemands` | pure static |
| `SavingsAggregator` | mode results | FOC breakdown + `SavingsBreakdown(L1,L2,L3)` | pure static |
| `TierResultBuilder` | FOC breakdown, savings, tier config, input, settings | `VariantResult` | pure static |
| `BatteryModeAdapter` | `BatteryModeAllocation` | `BatteryL1Adjustment`, hotel band, `BatteryDetails` | pure static |
| `ModePipelineRunner` | input, mode, overrides | `ModePipelineResult` | async today, sync after story D |
| `EngineFuelCurves` | input | ME curve + AE curve (sorted, filtered) | resolved once, then pure |

## 5. Story breakdown

Five stories, in order. Each is behaviour-preserving on its own.

| Story | Title | Removes |
|---|---|---|
| **R-A** | Extract the pure builders | `PowerDemandsBuilder`, `SavingsAggregator`, `TierResultBuilder`; the triple copy-paste of `BuildVariantResult(new(...))`; the derived fields in `TierSavings` |
| **R-B** | Battery adaptation and mode pipeline out of the orchestrator | `BatteryModeAdapter`, `ModePipelineRunner` |
| **R-C** | Level 1 internal structure | duplicated capacity arithmetic between `IsValid` and `DistributeLoad`; unnamed baseline-selection policy |
| **R-D** | Pre-resolved SFOC curves; levels become synchronous | per-call `Where().OrderBy().ToList()` in the hot loop; viral `async` |
| **R-E** | Cleanups | dead branches, `EngineCapacities`, magic tier keys, stale docs, `ValidationService` shape, repository duplication |

### 5.1 R-A — Extract the pure builders

Move, unchanged, out of `CalculatorService`:

- region *Power Demands* (lines 396–499) → `Services/Results/PowerDemandsBuilder.cs`
- region *FOC & Savings Aggregation* (lines 265–293) → `Services/Results/SavingsAggregator.cs`
- region *Result Building* (lines 295–394), incl. `CalculateFinancials` and the three `Co2For*`
  helpers → `Services/Results/TierResultBuilder.cs`

Then collapse the duplication the move exposes:

- `TierSavings(Advanced, Pro, Premium, L1, L2, L3)` → `SavingsBreakdown(L1, L2, L3)` with
  `TotalUpTo(level)`. `Advanced == L1`, `Pro == L1+L2`, `Premium == L1+L2+L3` are derived, not stored.
- The three near-identical `BuildVariantResult(new(input, foc, _settings, transitOptimal, new TierPlan(...)))`
  blocks become a tier table + one call per tier. A tier is `(ConfigKey, HighestLevel)`; which detail
  panels travel with it follows from `HighestLevel`.
- `BuildResultContext` / `TierPlan` shrink accordingly, or disappear if the builder's signature is
  readable without them.

**Arithmetic must not be retyped.** Move the expressions verbatim; only their location and the
plumbing around them changes.

### 5.2 R-B — Battery adaptation and mode pipeline out of the orchestrator

- Region *Battery* (lines 183–263): `ToAdjustment`, `HotelPeakShavingKw`, `BuildBatteryDetails` and
  the battery branch of `RunL1Async` → `Services/Battery/BatteryModeAdapter.cs`. This is battery
  domain knowledge; it belongs next to `BatteryAllocationService`, not in the orchestrator.
- `RunOptimizationPipelineAsync` + `RunL1Async` + the per-mode loop → `ModePipelineRunner`, which
  owns `ILevel1/2/3OptimizationService` and `IBatteryAllocationService`.
- `CalculatorService` keeps: resolve pricing configs, resolve sail, run Transit, run the other active
  modes, hand the results to the three builders, attach warnings.

The R3a reference-scenario run (battery budget 0) stays exactly as written — same call order, same
`Math.Max(0, …)` clamp, same `modeHours` multiplication.

### 5.3 R-C — Level 1 internal structure

Two changes inside `Level1OptimizationService`, no signature change:

1. `IsValid` (lines 190–193) and `DistributeLoad` (lines 213–227) compute the same four quantities
   (`sgCapacity`, `aeCapacity`, `sgPower`, `aePower`) for every candidate. Merge into one
   `TryEvaluate` that computes them once and either rejects or fills the combination.
   **Rejection order must be preserved** — `RejectionTally` drives the user-facing diagnostic
   message, and the message depends on which counter fires.
2. Extract the baseline choice (lines 112–119) into a named `SelectBaseline(sorted, baselineIndex, hasBattery)`.
   The D1 "third highest with an active battery" rule is business policy and deserves a name and a
   direct unit test.

The candidate ordering (`OrderBy(Foc).ThenBy(ActiveMe + ActiveAe)`) is load-bearing for the baseline
index and must not change — including its stability.

### 5.4 R-D — Pre-resolved SFOC curves; levels become synchronous

Today `SfocService.GetSfocForLoadAsync` re-filters and re-sorts the curve on **every** call, inside
the Level 1 candidate loop and per generator in Level 3. The data is constant for the whole request.

- Resolve both curves once per calculation: ME by `MainEngineTypeId`, AE by `AuxEngineTypeId`.
  These are the only two ever used — Level 3 maps SG to the *Main* category
  (`GeneratorType.ToEngineCategory()`), so no third curve exists.
- Pass the resolved curves down. `Level1`, `Level2` and `Level3` lose `async` and become pure
  functions over `(input, curves)`.

**Numerical equivalence argument (verify, do not assume):**
`SfocService.GetSfocForLoadAsync` filters `Load > 0`, sorts ascending, and calls
`SfocInterpolationHelper.Interpolate` — exactly what `Level2.GetFilteredAeSfocDataAsync` already
does today. Missing-curve behaviour also matches: `GetSfocForLoadAsync` returns
`DefaultSfocFallback` (220), and an empty curve makes `Interpolate` return the same constant.
Therefore pre-resolution is identical for both the happy and the missing-data path.

`SfocService` itself stays (it is the composition point and other callers exist); its two lookup
paths — the `if/else` in `GetSfocForLoadAsync` and the `switch` in `GetSfocDataAsync` — collapse into
one. Its exception-swallowing `catch` is **not** touched here (see §7.1).

### 5.5 R-E — Cleanups

Individually small, collectively the difference between "reads cleanly" and "nearly".

1. `Level3DrcService` lines 69–74: the `PowerKw <= 0` / `CapacityKw <= 0` fallbacks are unreachable —
   setpoints always come from `Level2`, which always populates both. Remove; keep a note in the
   XML doc that the invariant is Level 2's.
2. `Level3DrcService` line 33: `var modeHours = annualHours;` — an alias with no purpose.
3. `CalculatorService.BuildEngineCapacities` (503–513): duplicates rules `ValidationService` already
   enforces (so the throw is unreachable through the API), and the two consumed fields are literally
   `input.TotalMeCapacity` / `input.TotalAeCapacity`. Delete the method and the `EngineCapacities`
   record; move the guard to `ValidationService` if any coverage is lost.
4. `configMap["1"] / ["2"] / ["3"]` (133, 152–159): a deactivated `IntegrationLevel` row becomes a
   `KeyNotFoundException` → HTTP 500. Use `TryGetValue` and fail with a diagnosable message.
   *This changes only the failure text on an already-failing path, not any successful result.*
5. `Level2OptimizationService`: `MinLoad` (line 31) joins `MaxAuxLoadFraction` in `PlantLimits`;
   the class XML doc still says "exceeds 80%" (line 24) while the limit is 90% — fix the doc.
6. `ValidationService.ValidateInput`: split the 110-line linear method into `ValidatePlant`,
   `ValidateModes`, `ValidateBattery`, `ValidateSail`. Same checks, same order, same messages.
7. `HybridConfigRepository`: three copies of the double-checked-lock load block → one
   `GetOrLoadAsync` helper. `ClearCache()` currently mutates the cache fields outside the lock —
   take the lock.

## 6. Verification per story

1. Full suite green before starting (baseline recorded in the story's Debug Log).
2. Implement.
3. Full suite green — **including** all 18 golden scenarios and the calculation-card tests.
4. `git status` on `KSailCalc.Tests/Golden/Expected/` — must be clean (I1).
5. QA gate by Quinn, `docs/qa/gates/refactor.{a..e}-*.yml`.

Step 4 is the one that cannot be skipped or argued around. It is the whole safety case.

## 7. Deferred — behaviour-changing, tracked separately

Not part of this epic. Each needs its own decision and its own story.

1. **SFOC failure behaviour.** `SfocService` catches `Exception` and returns 220 g/kWh. A database
   outage currently produces a plausible but wrong number instead of an error. Should be narrowed to
   the "no data for this engine" case. *Needs Kamen's decision.*
2. **`Level3Result.VariationPerGeneratorKw` is misnamed** — the code comment says the value is the
   bus-wide swing, explicitly *not* per generator. Renaming breaks the client contract.
3. **`CancellationToken`** threaded from controller to the sweep loops. A disconnected client
   currently keeps burning CPU.
4. **Immutable `EngineCombination`.** Services mutate it (`DistributeLoad`, `TryApplyPtiAssist`) and
   the same instance travels into the response.
5. **The open `baselineIndex` restore finding** (docs/qa/manual-test-scenarios). Deliberately fixed
   *after* this epic, so that any golden-snapshot change is unambiguously attributable to the fix and
   not to a refactoring step.

## 8. Estimate

R-A ≈ 3h · R-B ≈ 3h · R-C ≈ 2h · R-D ≈ 3h · R-E ≈ 2h — roughly 1.5 days including QA gates.
The largest readability payoff is R-A and R-B; the largest correctness-and-performance payoff is R-D.
