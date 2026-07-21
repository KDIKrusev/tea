# Story: Battery Increment B — Pipeline Wiring, Dual-Scenario Benefit & API Contract

<!-- Source: docs/battery-feature-analysis/06-architecture-design.md (§2, §3.3–3.6, §5 Increment B) + QA gate battery.a (carried items) -->
<!-- Context: Brownfield enhancement to KSailCalc.Api — builds on Increment A (Done, gate PASS) -->

## Status: Done

<!-- QA Gate: PASS (docs/qa/gates/battery.b-pipeline-wiring.yml) · Owner approved 2026-07-13 -->


## Story

As a **naval architect / sales engineer using the iEMS Savings Calculator**,
I want **a configured battery to actually change the calculation — adjusted plant demand, a
"third-highest" default baseline, and an explicit "Battery benefit" line in the response**,
so that **battery-equipped configurations show honest, Excel-reconcilable numbers instead of
ignoring the battery entirely**.

## Context Source

- Source Documents: `06-architecture-design.md` §2 (pipeline placement + dual-scenario), §3.3
  (Level 1 changes), §3.5 (response contract), §3.6 (validation); decisions D1/D3 (R3a, R5);
  QA gate `battery.a-allocation-engine.yml` → carried recommendations.
- Enhancement Type: Wiring an existing isolated service into the calculation pipeline + additive
  API contract change.
- Existing System Impact: **`CalculatorService`, `Level1OptimizationService`, `ValidationService`
  are modified.** Hard requirement: with no battery, results must be **identical to today**.

## Scope

1. `IBatteryAllocationService.Allocate` gains optional `budgetOverrideKw` (reference scenario) and
   `propulsionOverrideKw` (sail-adjusted transit propulsion — QA carry-over #3).
2. `Level1OptimizationService.FindOptimalCombinationAsync` gains optional
   `BatteryL1Adjustment? batteryAdjustment` (record: `PropulsionReserveKw`, `HotelReserveKw`):
   adds uncovered spinning reserve to the mode loads; when non-null, default baseline =
   `sorted[max(0, Count−3)]` (D1 "third highest"); explicit `baselineIndex` still wins.
3. `CalculatorService`: for each mode where `input.Battery.AppliesTo(mode)`:
   - allocation → adjustment (per-load `UncoveredReserveKw`, split: Propulsion/DpReserve/DpDemand →
     propulsion side; Hotel/Mission → hotel side) → L1 (and L2/L3 for Transit as today);
   - **reference run** (R3a): same mode with `budgetOverrideKw = 0` (⇒ full variation ΣH as genset
     reserve), optimal FOC only; `benefit = max(0, refOptimal − batteryOptimal) × modeHours`;
   - build `AllVariantsCalculationResult.BatteryDetails` (null when battery inactive).
4. `BatteryDetails` response model: `capacityKwh`, `powerKw`, `spinningReserveKw` (ΣL over relevant
   modes), `peakShavingKw` (ΣJ), `benefitFocTonPerYear`, `benefitCostPerYear`, `modeAllocations`.
5. Validation (`ValidationService`): PowerKw/CapacityKwh ≥ 0; PowerKw > 0 ⇒ CapacityKwh > 0;
   RelevantModes ⊆ {Transit, DP, Port}; DP in modes ⇒ `DpEnabled`; warning when PowerKw > 0 but
   no relevant modes (inert battery); warning when CapacityKwh < 0.5 × PowerKw (30-min plausibility,
   Q1 placeholder).
6. QA carry-overs: log effective `BatterySettings` at startup (source: appsettings vs code
   defaults); `[JsonConverter(typeof(JsonStringEnumConverter))]` on `BatteryFunction` /
   `BatteryLoadType` (OperationalMode already has it — RelevantModes serializes as strings).

**Out of scope:** PTI/PTO gates (Increment C), client (D), L3 residual-variation rule (E), battery
CAPEX in ROI (Q9).

## Acceptance Criteria

1. **Zero regression:** `CalculateAllVariantsAsync` with `Battery = null` and with
   `Battery = { PowerKw: 0 }` returns results numerically identical to the pre-change behaviour
   (assert on BaselineFOC, per-tier FuelSavings, SelectedBaselineIndex), and `BatteryDetails`
   is null in both cases.
2. **Demand adjustment:** with an active Transit battery, L1 evaluates
   propulsion + propulsion-side ΣL and hotel + hotel-side ΣL (verifiable via
   `Level1Result.OptimalCombination.MePowerKw` growth vs the no-battery run).
3. **Baseline rule (D1):** with an active battery and ≥3 valid combinations,
   `SelectedBaselineIndex == Count − 3` by default; explicit `BaselineIndex` still wins; with
   <3 combinations the index clamps to 0.
4. **Battery benefit (R3a):** `BatteryDetails.BenefitFocTonPerYear ≥ 0` and equals
   `(referenceOptimalFoc − batteryOptimalFoc) × modeHours` summed over relevant modes;
   `BenefitCostPerYear = benefit × FuelPrice`. The benefit is NOT included in L1/L2/L3 tier savings.
5. **Contract:** `BatteryDetails` present (non-null) iff battery is active; `SpinningReserveKw` /
   `PeakShavingKw` equal the allocation totals (Excel example: 1260 kW Transit battery on the
   AC1 scenario from story A ⇒ 444.7475 / 204.4025).
6. **Validation:** each rule in Scope #5 covered by a test (errors block, warnings pass through).
7. Full suite green; all existing tests unchanged (TestServiceFactory may gain the new dependency).

## Dev Technical Guidance

- `CalculatorService` ctor gains `IBatteryAllocationService` → update `TestServiceFactory.Create`
  (single construction point for tests; `CalculatorServiceTests` etc. construct via factory only).
- Non-transit modes today call `FindOptimalCombinationAsync(input, mode)` **without**
  `input.BaselineIndex` — preserve that (user baseline selection applies to Transit only).
- Transit propulsion override (sail) must flow into `Allocate(propulsionOverrideKw:)` so the
  battery allocates against sail-adjusted demand (QA carry-over #3).
- `BatteryModeAllocation` is reused inside `BatteryDetails.ModeAllocations` (camelCase JSON is
  global; enums get string converters per Scope #6).
- Startup log after `builder.Build()`: row count + whether LoadPriorities came from configuration
  or `CreateDefaultLoadPriorities()` fallback.
- Do not touch `RunOptimizationPipelineAsync`'s L2/L3 semantics — they consume the (adjusted) L1.

## Risk Assessment

- **Primary Risk:** unintended change to no-battery results (the pipeline methods are shared).
  **Mitigation:** every new branch is gated on `input.Battery?.AppliesTo(mode) == true`; AC1 pins
  equality; full regression suite.
- **Secondary:** double-running L1 (reference scenario) per relevant mode adds latency.
  Negligible (~100 combos), matches design §3.3 cost note.
- **Rollback:** revert the four modified service/model files; Increment A remains intact and inert.

## Tasks / Subtasks

- [x] Task 1: Extend `IBatteryAllocationService`/`BatteryAllocationService` (budget & propulsion
      overrides) + enum JSON converters + startup logging
- [x] Task 2: `BatteryL1Adjustment` + `Level1OptimizationService` changes (loads, baseline rule)
- [x] Task 3: `BatteryDetails` model + `AllVariantsCalculationResult.BatteryDetails`
- [x] Task 4: `CalculatorService` wiring (per-mode allocation, dual-scenario benefit, details)
- [x] Task 5: `ValidationService` battery rules
- [x] Task 6: Tests — AC1 regression equality, AC2 demand adjustment, AC3 baseline rule,
      AC4 benefit, AC5 contract totals, AC6 validation; `TestServiceFactory` update
- [x] Task 7: Full suite green (153/153: 134 existing + 19 new); story record updated

## Dev Agent Record

### Agent Model Used

Claude Fable 5 (claude-fable-5)

### Completion Notes

- All 7 ACs implemented and covered; full suite **153/153 green**.
- Battery-aware Level 1 runs through a single gate (`CalculatorService.RunL1Async`): inactive
  battery takes literally today's code path (AC1); active battery allocates, adjusts loads
  (propulsion side: Propulsion/DpReserve/DpDemand; hotel side: Hotel/Mission), and runs the R3a
  zero-budget reference scenario for the benefit line.
- "Third highest" default baseline lives in `Level1OptimizationService` keyed on
  `batteryAdjustment != null`; explicit `baselineIndex` still wins (D1); clamped via `Max(0, n−3)`.
- QA carry-overs from gate battery.a all addressed: startup logging of effective `BatterySettings`
  (with source: appsettings vs code defaults), string enum converters on `BatteryFunction`/
  `BatteryLoadType` (`OperationalMode` already had one), and `propulsionOverrideKw` so the
  allocation sees sail-adjusted transit propulsion.
- **Deviation (story guidance was wrong, not the code):** Dev Technical Guidance claimed tests
  construct `CalculatorService` via `TestServiceFactory` only; `EngineFuelCo2Tests.cs:89`
  constructs it directly and needed the new constructor argument (one-line, additive).
- Note for QA: AC1's "identical to pre-change behaviour" is evidenced two ways — the untouched
  pre-existing suite (134 tests) all pass unchanged, and a direct equality test between
  `Battery = null` and inert-battery runs.

### Debug Log References

- One test-design failure on first run: the default builder scenario has only **1 valid
  combination** (SG covers hotel → AE-idle variants pruned), too few for the third-highest rule.
  Introduced `RichPlant()` (AE 2000×3 ⇒ several valid ME×AE combos) for the baseline-rule tests.
- Full-suite runs use `-p:BaseOutputPath=<scratchpad>` (repo `bin\` still locked by the running
  dev server + VS, same as Increment A).

### File List

New:
- `Models/BatteryDetails.cs`
- `KSailCalc.Tests/Services/CalculatorServiceBatteryTests.cs` (19 tests)

Modified:
- `Models/BatteryAllocation.cs` (added `BatteryL1Adjustment` record)
- `Models/AllVariantsCalculationResult.cs` (nullable `BatteryDetails`)
- `Models/Enums/BatteryFunction.cs` (JsonStringEnumConverter on both enums)
- `Services/Interfaces/IBatteryAllocationService.cs` (budget/propulsion override params)
- `Services/BatteryAllocationService.cs` (override handling)
- `Services/Interfaces/ILevel1OptimizationService.cs` (+ `batteryAdjustment` param)
- `Services/Level1OptimizationService.cs` (load adjustment + third-highest default baseline)
- `Services/CalculatorService.cs` (battery wiring: RunL1Async, ToAdjustment, BuildBatteryDetails)
- `Services/ValidationService.cs` (battery errors + advisory warnings)
- `Program.cs` (startup logging of effective BatterySettings)
- `KSailCalc.Tests/TestHelpers/TestServiceFactory.cs` (BatteryAllocationService dependency)
- `KSailCalc.Tests/Services/EngineFuelCo2Tests.cs` (new ctor argument)

### Change Log

| Date | Change |
|---|---|
| 2026-07-13 | Increment B implemented: battery wired into the pipeline (per-mode allocation, adjusted L1 demand, D1 third-highest default baseline, R3a dual-scenario benefit), BatteryDetails contract, validation rules, QA carry-overs. Full suite 153/153 green. Status → Ready for Review. |

## QA Results

### Review Date: 2026-07-13

### Reviewed By: Quinn (Test Architect)

### Code Quality Assessment

Clean, well-gated wiring. The single-gate design (`RunL1Async`) makes the zero-regression claim
structurally verifiable: the inactive-battery branch is literally the pre-change call. Level 1
changes are minimal and additive; the D1 baseline rule and its clamp are correct; all three QA
carry-overs from gate battery.a are properly addressed (verified startup logging source logic,
string enum converters, and sail-adjusted propulsion flowing into the allocation). Deep review
triggered by: core-service diff, 7 ACs, prior gate finding.

**No defects found.** Two test-coverage gaps were found and closed during review (below).

### Refactoring Performed

- **File**: `KSailCalc.Tests/Services/CalculatorServiceBatteryTests.cs`
  - **Change**: added `Level1_WithBatteryAdjustment_FewerThanThreeCombinations_ClampsBaselineToZero`.
  - **Why**: AC3 promises the `max(0, n−3)` clamp but no test exercised the <3-combination branch;
    the ExcelPlant scenario (exactly 1 valid combination) covers it naturally.
  - **How**: pins `SelectedBaselineIndex == 0` with a guard that the list is genuinely small.
- **File**: `KSailCalc.Tests/Services/CalculatorServiceBatteryTests.cs`
  - **Change**: AC4 benefit assertion strengthened from `≥ 0` to `> 0`.
  - **Why**: in the Excel scenario the reference demand (avg + ΣH) is strictly higher than the
    battery demand (avg + ΣL) for the same combination, so a zero benefit would indicate the
    dual-scenario plumbing silently short-circuiting; `≥ 0` could mask that.
  - **How**: `BeGreaterThan(0)` with an explanatory comment.

### Compliance Check

- Coding Standards: ✓ (idiomatic with the surrounding services; region conventions followed)
- Project Structure: ✓
- Testing Strategy: ✓ (unit-level for Level 1 rules, service-level for wiring/contract; factory
  extended at the single construction point + one direct-construction call site fixed by dev)
- All ACs Met: ✓ (AC1–AC7; AC3 clamp and AC4 strictness now explicitly covered)

### Improvements Checklist

- [x] AC3 clamp test added
- [x] AC4 benefit assertion strengthened to strictly-positive
- [ ] **Increment D (client)**: battery active but relevant modes have zero hours ⇒ `batteryDetails`
      is null by design — the client should surface a hint ("battery has no effect: no hours in
      relevant modes") instead of silently hiding the panel
- [ ] **Future**: `SpinningReserveKw`/`PeakShavingKw` are summed **across relevant modes** (per
      design §3.5); for multi-mode batteries this overstates a single physical battery's kW figures
      in the headline — revisit presentation (per-mode display or max) when Increment D lands
- [ ] **Increment C**: `BuildPowerDemands` now reflects battery-adjusted optimal combos (intended —
      the plant carries the reserve); recheck the power-demands panel copy in D so users understand
      demand includes uncovered spinning reserve

### Security Review

No concerns — no new I/O or endpoints; input validation added for all new fields.

### Performance Considerations

One extra L1 enumeration per relevant mode (reference scenario) — bounded by ~100 combinations;
measured suite time unchanged (218 ms). Negligible.

### Files Modified During Review

- `KSailCalc.Tests/Services/CalculatorServiceBatteryTests.cs` (2 test additions/strengthenings)

(Dev: add to File List on next touch.)

### Gate Status

Gate: **PASS** → docs/qa/gates/battery.b-pipeline-wiring.yml
(Full suite after review: **154/154 green**, 218 ms.)

### Recommended Status

✓ Ready for Done — carry the three unchecked items into Increments C/D story notes.
(Story owner decides final status.)
