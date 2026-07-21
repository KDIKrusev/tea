# Story: Battery Increment A — Battery Allocation Engine (pure logic)

<!-- Source: docs/battery-feature-analysis/ (01-task-brief, 02-excel-model-analysis, 06-architecture-design) -->
<!-- Context: Brownfield enhancement to KSailCalc.Api — iEMS Savings Calculator -->

## Status: Done

<!-- QA Gate: PASS (docs/qa/gates/battery.a-allocation-engine.yml) · Owner approved 2026-07-13 -->


## Story

As a **naval architect / sales engineer using the iEMS Savings Calculator**,
I want **the system to compute how a battery's power budget is allocated across a mode's load
demands (peak shaving vs spinning reserve, per the reference Excel model)**,
so that **subsequent increments can adjust plant demand and demonstrate battery benefits with
numbers that reconcile against the domain expert's workbook**.

## Context Source

- Source Documents: `docs/battery-feature-analysis/06-architecture-design.md` (§3.1, §3.2, §5
  Increment A), `02-excel-model-analysis.md` (§1.2, §1.3 — the algorithm), `05-decisions-log.md`
  (D2, D3/R1–R3).
- Enhancement Type: New isolated calculation service (pure logic, **not wired into the pipeline**).
- Existing System Impact: **None at runtime** — new files + DI registration only; no existing code
  path calls the new service in this increment.

## Scope (this increment ONLY)

1. New enum `BatteryFunction { Reserve, PeakShaving }` (`Models/Enums/`).
2. New models `BatteryConfigurationInput`, `BatteryModeAllocation`, `BatteryLoadAllocation`
   (`Models/`) exactly as specified in `06-architecture-design.md` §3.1.
3. New settings class `BatterySettings` (+ nested `BatteryLoadPriority`) bound from a new
   `appsettings.json` section with the Excel default values (design §3.2).
4. New service `BatteryAllocationService : IBatteryAllocationService` (`Services/` +
   `Services/Interfaces/`) — stateless, synchronous, no I/O.
5. DI registration in `Program.cs` (scoped, alongside the other calc services).
6. Unit tests reproducing the Excel reference example.

**Explicitly out of scope:** any change to `CalculatorService`, `Level1OptimizationService`,
`ValidationService`, controllers, response DTOs, or the Angular client (Increments B/D); PTI/PTO
(Increment C); L3 interaction (Increment E).

## Algorithm (normative — from the Excel "Load Demands" sheet)

For a given `OperationalMode` and `CalculatorInput`, build the mode's load rows from
`BatterySettings.LoadPriorities` (order = allocation priority), mapping loads to input fields:

| Priority load key | Applies in mode(s) | AverageLoadKw source | VariationKw source |
|---|---|---|---|
| `DpReserve` | DP | 0 (reserve row: variation = DP redundancy requirement; 0 until a dedicated input exists — see Missing Information) | 0 for now |
| `DpDemand` | DP | `RequiredDPPowerKW ?? 0` | `avg × variationFactor` (config, default 0) |
| `Mission` | Transit, DP | 0 (no mission-load input exists yet) | 0 for now |
| `Propulsion` | Transit | Transit: `EffectivePropulsionPower` | `avg × variationFactor` (default 0.05) |
| `Hotel` | Transit, DP, Port | mode's hotel field (`TransitHotelPowerKW` / `DPHotelPowerKW` / `PortHotelPowerKW`) | `avg × variationFactor` (default 0.02) |

Then cascade the budget (`battery.PowerKw`) over the rows in priority order:

```
remaining = battery.PowerKw
for each row (priority order):
    H = row.VariationKw                      // max variation (reserve rows: full requirement)
    I = min(remaining, H)                    // battery used for this row
    J = I × row.CoverageFactor               // covered ± band (peak shaving) / covered reserve
    L = H − J                                // uncovered → additional spinning reserve
    remaining = max(remaining − I, 0)
totals: PeakShavingBandKw = Σ J (PeakShaving rows)
        AdditionalSpinningReserveKw = Σ L
        CommittedBatteryKw = Σ I
        RemainingBatteryKw = remaining
```

Edge rules:
- `battery == null` or `PowerKw <= 0` or mode ∉ `RelevantModes` → return an all-zero allocation
  where every row has `I = J = 0` and `L = H` (i.e. the full variation is uncovered).
- Negative inputs are not this service's concern (Increment B adds validation); clamp to 0
  defensively.

## Acceptance Criteria

1. **Excel reference reproduction** (workbook saved state, budget = 1 260 kW; Transit-like scenario
   with Propulsion avg = 11 463 kW @ ±5 %, Hotel avg = 3 800 kW @ ±2 %, other rows zero) — the
   service returns, within 1e-6:
   - Propulsion row: `H = 573.15`, `I = 573.15`, `J = 200.6025`, `L = 372.5475`
   - Hotel row: `H = 76`, `I = 76`, `J = 3.8`, `L = 72.2`
   - Totals: `CommittedBatteryKw = 649.15`, `PeakShavingBandKw = 204.4025`,
     `AdditionalSpinningReserveKw = 444.7475`, `RemainingBatteryKw = 610.85`
2. **Budget exhaustion:** with a small budget (e.g. 100 kW), the first-priority row consumes it
   (`I = 100`) and all later rows get `I = 0`, `L = H`.
3. **Zero/inactive battery:** `PowerKw = 0`, `battery = null`, and mode not in `RelevantModes` all
   yield `PeakShavingBandKw = 0` and `AdditionalSpinningReserveKw = Σ H`.
4. **Config-driven behaviour:** changing a `coverageFactor` or row order in `BatterySettings`
   changes the result accordingly (test with a custom settings instance, not the JSON file).
5. **No runtime impact:** `dotnet build` clean; **all existing tests pass unchanged**; no existing
   service/controller references the new service.
6. Default `BatterySettings` values in `appsettings.json` equal the Excel constants
   (design §3.2: PtiLossFactor 0.05, efficiencies 0.97/0.97/0.965, the five LoadPriorities rows).

## Tasks / Subtasks

- [x] Task 1: Models & enum
  - [x] `Models/Enums/BatteryFunction.cs` (follow `GeneratorType.cs` style)
  - [x] `Models/BatteryConfigurationInput.cs`, `Models/BatteryAllocation.cs` (both allocation
        classes in one file is fine — follow `SailContributionModels.cs` precedent)
- [x] Task 2: Settings
  - [x] `BatterySettings` + `BatteryLoadPriority` — add to `Models/CalculatorSettings.cs` file or a
        sibling file; bind in `Program.cs` via `builder.Services.Configure<BatterySettings>(
        builder.Configuration.GetSection("BatterySettings"))` (mirror `CalculatorSettings` at
        `Program.cs:10`)
  - [x] Add the `BatterySettings` section to `appsettings.json` with Excel defaults
- [x] Task 3: Service
  - [x] `Services/Interfaces/IBatteryAllocationService.cs`:
        `BatteryModeAllocation Allocate(OperationalMode mode, CalculatorInput input)`
  - [x] `Services/BatteryAllocationService.cs` — pure implementation per the normative algorithm;
        inject `IOptions<BatterySettings>`; keep the mode→loads mapping as a private static method
        (style reference: `Level1OptimizationService.GetModeLoads`)
  - [x] Register scoped in `Program.cs` (alongside lines 29-37)
- [x] Task 4: Tests (`KSailCalc.Tests/Services/BatteryAllocationServiceTests.cs`)
  - [x] xUnit + FluentAssertions, follow existing test style; build inputs with
        `CalculatorInputBuilder` (add `WithBattery(double powerKw, double capacityKwh,
        params OperationalMode[] modes)` to the builder)
  - [x] AC1 Excel reference test (exact numbers above)
  - [x] AC2 budget exhaustion, AC3 inactive battery, AC4 custom settings
- [x] Task 5: Verify existing functionality
  - [x] `dotnet test` — full suite green (133/133: 124 existing + 9 new)
  - [x] Confirm no diff in any existing file except `Program.cs` (2 additive lines) and
        `appsettings.json` (1 additive section) — see deviation note in Dev Agent Record

## Dev Agent Record

### Agent Model Used

Claude Fable 5 (claude-fable-5)

### Completion Notes

- All 6 acceptance criteria verified: AC1 reproduces the workbook rows and totals to 1e-6
  (Propulsion H/I/J/L = 573.15 / 573.15 / 200.6025 / 372.5475; Hotel 76 / 76 / 3.8 / 72.2;
  ΣI 649.15, ΣJ 204.4025, ΣL 444.7475, remaining 610.85); AC2–AC4 covered by dedicated tests;
  AC5 full suite green; AC6 `appsettings.json` defaults = Excel constants (also duplicated as
  code defaults in `BatterySettings` so the service works without the config section).
- **Deviation 1 (story-internal contradiction resolved):** the specified interface
  `Allocate(OperationalMode, CalculatorInput)` requires the battery to be reachable from
  `CalculatorInput`, and Task 4's `WithBattery` builder method implies the same — so an **additive
  nullable `Battery` property was added to `CalculatorInput`** (plus a deprecation note on the
  legacy `BatteryCapacity` stub). Zero behavioral impact: nothing reads it except the new,
  uncalled service. Task 5's "no diff except…" list should have included this file.
- **Deviation 2 (minor):** `BatteryLoadAllocation.Load` uses a typed enum `BatteryLoadType`
  instead of the design doc's `LoadName string` — config binding still accepts the string keys
  ("DpReserve", "Propulsion", …), and the service gains exhaustive typed mode-mapping.
- Reserve-row semantics implemented per Excel row 5: `H = avg × (1 + variationFactor)` (full
  requirement), vs peak-shaving rows `H = avg × variationFactor`; covered by a dedicated test.
- `DpReserve` and `Mission` rows evaluate to 0 kW (no input fields yet) as documented in
  Missing Information — mode mapping includes them so Increment B/C only adds field wiring.

### Debug Log References

- Full-suite run initially blocked: `bin\Debug\net10.0\KSailCalc.Api.*` locked by the running dev
  server (PID 14516) and Visual Studio. Worked around with
  `dotnet test -p:BaseOutputPath=<scratchpad>` (no state on the dev machine was touched);
  2 transient failures in `AppDataAggregationServiceParametricTests` were an artifact of the
  redirected path (the test resolves `appsettings.json` 4 levels above the test bin) — resolved by
  copying `appsettings.json` to the expected location; final run **133/133 green in 200 ms**.

### File List

New:
- `Models/Enums/BatteryFunction.cs` (BatteryFunction + BatteryLoadType enums)
- `Models/BatteryConfigurationInput.cs`
- `Models/BatteryAllocation.cs` (BatteryModeAllocation + BatteryLoadAllocation)
- `Models/BatterySettings.cs` (BatterySettings + BatteryLoadPriority)
- `Services/Interfaces/IBatteryAllocationService.cs`
- `Services/BatteryAllocationService.cs`
- `KSailCalc.Tests/Services/BatteryAllocationServiceTests.cs` (9 tests)

Modified (all additive):
- `Models/CalculatorInput.cs` (nullable `Battery` property + deprecation doc on `BatteryCapacity`)
- `Program.cs` (BatterySettings binding + IBatteryAllocationService registration)
- `appsettings.json` (BatterySettings section)
- `KSailCalc.Tests/TestHelpers/CalculatorInputBuilder.cs` (`WithBattery(...)` + using)

### Change Log

| Date | Change |
|---|---|
| 2026-07-13 | Increment A implemented: battery allocation engine (Excel "Load Demands" port), settings, DI registration, 9 unit tests. Full suite 133/133 green. Status → Ready for Review. |

## QA Results

### Review Date: 2026-07-13

### Reviewed By: Quinn (Test Architect)

### Code Quality Assessment

Strong increment overall: the allocation engine is pure, synchronous, well-documented against the
Excel source, and the mode→loads mapping is exhaustive and typed. Requirements traceability is
excellent — every AC row value (H/I/J/L) is pinned to 1e-6 against the workbook. One **latent
high-severity defect** was found and fixed during review (see below); it was unreachable in this
increment (service intentionally uncalled) but would have corrupted results the moment Increment B
wires the service in.

**Defect found (fixed in review): ConfigurationBinder list-append duplication.**
`BatterySettings.LoadPriorities` had 5 code-default rows; `ConfigurationBinder.Bind` **appends**
JSON list items to a pre-populated list instead of replacing them. With the `BatterySettings`
section present in `appsettings.json` (the normal production state), `IOptions<BatterySettings>`
delivered **10 priority rows** — the cascade would process every load twice (e.g. Propulsion would
consume budget a second time from the remaining 686.85 kW), silently producing wrong ΣJ/ΣL. Dev
unit tests could not catch this because they construct `BatterySettings` directly, bypassing
config binding. Caught by the new AC6 config-binding test added in this review.

### Refactoring Performed

- **File**: `Models/BatterySettings.cs`
  - **Change**: `LoadPriorities` default is now an **empty list**; Excel defaults moved to static
    `CreateDefaultLoadPriorities()` (returns a fresh copy).
  - **Why**: eliminate the binder append-duplication defect at its root.
  - **How**: empty default means JSON binding produces exactly the JSON rows; explicit factory
    keeps "works without config section" behaviour and gives tests a mutable copy.
- **File**: `Services/BatteryAllocationService.cs`
  - **Change**: constructor resolves `_loadPriorities` once — bound rows when present, otherwise
    `CreateDefaultLoadPriorities()` fallback.
  - **Why/How**: single decision point, documented; `Allocate` loop unchanged.
- **File**: `KSailCalc.Tests/Services/BatteryAllocationServiceTests.cs`
  - **Change**: (1) added `BatterySettingsConfigurationTests.AppSettings_BatterySettingsSection_MatchesExcelReferenceConstants`
    — binds the real `appsettings.json` and pins all Excel constants + exactly 5 rows (this is the
    test that exposed the defect); (2) renamed misleading
    `Allocate_DpMode_ReserveRowCoversFullRequirement_NotJustVariation` →
    `Allocate_DpMode_MapsExpectedLoadRows_InPriorityOrder` (it verifies DP row mapping; reserve
    semantics are covered by `Allocate_ReserveFunction_UsesFullRequirementAsVariation`);
    (3) three AC4/reserve tests now populate `LoadPriorities` via the factory (empty default).

### Compliance Check

- Coding Standards: ✓ (no `docs/architecture/coding-standards.md` exists; matched surrounding code
  idiom — XML docs, settings pattern, service layout)
- Project Structure: ✓ (Models/Enums/Services/Interfaces placement mirrors existing conventions)
- Testing Strategy: ✓ (xUnit + FluentAssertions, builder pattern, follows existing parametric-test
  config-loading convention for the new binding test)
- All ACs Met: ✓ (AC1–AC5 verified by independent run; AC6 now has an automated test instead of
  manual inspection)

### Improvements Checklist

- [x] Fixed ConfigurationBinder list-append defect (Models/BatterySettings.cs, Services/BatteryAllocationService.cs)
- [x] Added AC6 config-binding regression test (BatteryAllocationServiceTests.cs)
- [x] Renamed misleading DP-mapping test
- [ ] **Increment B**: log effective `BatterySettings` at startup (architecture §6 mitigation — now
      more valuable given the binding gotcha)
- [ ] **Increment B**: decide JSON representation of `OperationalMode` in `RelevantModes` (current
      global options serialize enums as **numbers**; client contract should probably use strings —
      add `JsonStringEnumConverter` or map explicitly)
- [ ] **Increment B**: allocation's Propulsion row uses `EffectivePropulsionPower`; when wiring,
      pass the **sail-adjusted** transit propulsion (mirror the `overridePropulsionKw` flow) or the
      battery will allocate against pre-sail demand
- [ ] Consider Singleton registration for `IBatteryAllocationService` (stateless; Scoped is
      consistent with siblings but not required)

### Security Review

No concerns — pure computation, no I/O, no user-facing surface in this increment.

### Performance Considerations

Negligible — O(5) row cascade per mode; constructor resolves priorities once per scope.

### Files Modified During Review

- `Models/BatterySettings.cs`
- `Services/BatteryAllocationService.cs`
- `KSailCalc.Tests/Services/BatteryAllocationServiceTests.cs`

(Dev: please add the review changes to the story File List on next touch — Quinn is not authorized
to edit that section.)

### Gate Status

Gate: **PASS** → docs/qa/gates/battery.a-allocation-engine.yml
(Full suite after review refactoring: **134/134 green**, 637 ms.)

### Recommended Status

✓ Ready for Done — with the three Increment-B items above carried into the next story's Dev Notes.
(Story owner decides final status.)

## Dev Technical Guidance

### Existing System Context

- .NET 10 Web API, no EF (raw ADO not needed here — this service has **no I/O**).
- Calc services are scoped, constructor-injected, registered in `Program.cs:29-37`.
- Settings pattern: `CalculatorSettings` bound from config (`Program.cs:10`), consumed via
  `IOptions<T>` (see `CalculatorService` ctor).
- Pure-helper precedent: `Services/Helpers/CalculationHelpers.cs`.
- Test stack: xUnit 2.9 + FluentAssertions 7 + Moq; helpers in `KSailCalc.Tests/TestHelpers/`
  (`CalculatorInputBuilder`, `TestServiceFactory`). No mocks needed for this service (no deps
  besides options — construct `Options.Create(settings)` directly).

### Integration Approach

None in this increment — the service is registered but intentionally uncalled. Increment B wires it
into `CalculatorService.CalculateAllVariantsAsync` (dual-scenario rule R3a).

### Technical Constraints

- Deterministic pure math; no DateTime/random/culture-sensitive parsing. Use `double` (project
  convention; SFOC path casts to `decimal` only at the `ISfocService` boundary).
- JSON is camelCase globally — irrelevant here (no API surface yet) but keep property naming
  consistent with future serialization (`PowerKw`, `CapacityKwh`).

### Missing Information (documented, non-blocking)

- **DP redundancy requirement** (`DpReserve` row's kW) has no input field yet — row evaluates to
  zero until a field is added (tracked for Increment B/C; matches Excel example where DP rows are 0).
- **Mission load** similarly has no input — zero for now.
- Open question Q4 (L3 interaction) does not affect this increment.

## Risk Assessment

### Implementation Risks

- **Primary Risk:** silent drift from the Excel semantics (e.g. applying `CoverageFactor` to the
  wrong term, or subtracting `J` instead of `I` from the budget — Excel subtracts **I**).
- **Mitigation:** AC1 pins every intermediate (H/I/J/L per row), not just totals.
- **Verification:** run AC1 against the workbook (`docs/PowerPlantSetupAdvisesIncludingPTIOAndbatteries_test.xlsx`,
  sheet *Load Demands*, rows 5–10) if any number is in doubt.

### Rollback Plan

- Delete the new files, revert the 2 `Program.cs` lines and the `appsettings.json` section. No data
  or contract changes exist to roll back.

### Safety Checks

- [x] Existing behaviour untouched by construction (service uncalled)
- [x] Changes isolated to new files + 2 additive registrations
- [x] Rollback = file deletion

## Definition of Done

- All Acceptance Criteria pass; full test suite green; build clean.
- No changes outside: new files, `Program.cs` (additive), `appsettings.json` (additive),
  `CalculatorInputBuilder` (additive builder method).
