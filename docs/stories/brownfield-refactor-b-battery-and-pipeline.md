# Story: Refactor B — Battery adaptation and the mode pipeline out of the orchestrator

<!-- Source: docs/refactoring/backend-refactor-design.md §5.2 (target structure §4) -->
<!-- Context: Brownfield refactoring of KSailCalc.Api. Behaviour-preserving. Depends on Story A. -->

## Status: Ready for Review

## Story

As a **developer debugging a battery-equipped vessel calculation**,
I want **the battery adaptation logic to live next to `BatteryAllocationService`, and the per-mode
L1→L2→L3 pipeline to live in its own runner**,
so that **I can read and test "what the battery does to Level 1" without reading the orchestrator,
and "what one mode goes through" without reading the battery**.

## Context Source

- Design §4 (target structure), §5.2 (this story), §3 (invariants I1–I6).
- Today `CalculatorService` region *Battery* (lines 183–263) holds battery **domain** knowledge —
  how an allocation becomes an L1 adjustment, which side of the plant carries which reserve, how the
  R3a benefit is derived — while `BatteryAllocationService` sits next door owning the allocation
  itself. The same quantity is summed in two places: `HotelPeakShavingKw`
  (`CalculatorService.cs:212`) and `PeakShavingBandKw` (`BatteryAllocationService.cs:54`) differ only
  by the thrust-side filter.
- Depends on Story A (the pure builders are already out; what remains in `CalculatorService` is
  orchestration + battery + pipeline).

## Scope

### 1. `Services/Battery/BatteryModeAdapter.cs` (new)

Moves, verbatim, from `CalculatorService`:

- `ToAdjustment(BatteryModeAllocation)` → `BatteryL1Adjustment` (lines 223–239)
- `HotelPeakShavingKw(BatteryModeAllocation)` (lines 212–215)
- `BuildBatteryDetails(input, modes)` (lines 241–261)

The thrust-side split (`IsThrustSide()`), the `BatteryFunction.PeakShaving` filter and the
`Math.Max(0, …)` clamp on the R3a benefit all move unchanged.

### 2. `Services/ModePipelineRunner.cs` (new)

Owns `ILevel1OptimizationService`, `ILevel2OptimizationService`, `ILevel3DrcService` and
`IBatteryAllocationService`. Moves:

- `RunOptimizationPipelineAsync` (lines 171–181) — the full Transit path
- `RunL1Async` (lines 191–209) — including the battery branch and the R3a reference run
- the per-mode invocation currently inlined in `CalculateAllVariantsAsync` (lines 96–115)

Registered in `Program.cs` as `Scoped`, matching the level services it wraps.

### 3. What `CalculatorService` keeps

Only orchestration: fetch pricing configs, resolve sail, run Transit through the runner, run the
other active modes through the runner, hand results to the Story-A builders, attach warnings.
Target: **~110 lines**.

### 4. Order-sensitive details that must not drift

- The **R3a reference run** (battery budget 0 ⇒ full variation carried by gensets) keeps the same
  call order, the same `baselineIndex: null` on the reference run, the same
  `Math.Max(0, referenceL1.OptimalFocTonPerHour - l1.OptimalFocTonPerHour) * modeHours`.
- The **user-pinned baseline applies to Transit only** — non-Transit modes pass `baselineIndex: null`
  (`CalculatorService.cs:113`). Preserve exactly.
- **Non-Transit modes get L1 only** (D4/Q5) — empty `Level2Result`/`Level3Result`, no L2/L3 call.
- **Mode iteration order** follows `OperationalModes.ExceptTransit`, with Transit first in the list.
  `PowerDemands.ModeBreakdowns` is serialized in that order and is snapshot-compared.
- The **empty-L2/L3 instances** are currently shared across non-Transit modes (`CalculatorService.cs:104–105`).
  Sharing or not sharing them must not change serialization.

## Acceptance Criteria

1. **AC1 — Snapshots frozen (I1).** 18/18 golden scenarios pass; zero modified files under
   `KSailCalc.Tests/Golden/Expected/`; `GOLDEN_UPDATE` never set.
2. **AC2 — Wire contract frozen (I2).** No model property changed; no file under `cl/` modified.
3. **AC3 — Battery logic relocated.** `CalculatorService` contains no `BatteryL1Adjustment`
   construction, no `BatteryFunction`/`IsThrustSide` filtering and no `BatteryDetails` assembly.
   `BatteryModeAdapter` does.
4. **AC4 — Pipeline relocated.** `CalculatorService` no longer references
   `ILevel1/2/3OptimizationService` or `IBatteryAllocationService`; `ModePipelineRunner` does and is
   DI-registered.
5. **AC5 — Orchestrator size.** `Services/CalculatorService.cs` is under 150 lines.
6. **AC6 — Battery-off path bit-identical.** Scenarios without a battery exercise the exact same code
   path as today (no allocation call, no reference run) — verified by the existing no-battery golden
   scenarios plus a direct assertion that `IBatteryAllocationService.Allocate` is never invoked when
   `input.Battery?.AppliesTo(mode) != true`.
7. **AC7 — Suite green.** Full suite green, no test deleted, skipped or weakened.
   `GoldenScenarioHost.cs` updated for the new composition (allowed by I3).
8. **AC8 — Unit tests.** Direct unit tests for `BatteryModeAdapter` (thrust vs hotel split; the
   hotel-band sum excluding thrust-side loads) and for `ModePipelineRunner` (non-Transit runs L1 only
   and receives `baselineIndex: null`).

## Tasks / Subtasks

- [x] Task 0: Baseline suite run recorded in the Debug Log.
- [x] Task 1: Extract `BatteryModeAdapter`; rewire `CalculatorService` to call it. Suite green.
- [x] Task 2: Extract `ModePipelineRunner`; move `RunOptimizationPipelineAsync` + `RunL1Async`.
      Suite green.
- [x] Task 3: Move the per-mode loop into the runner; `CalculatorService` down to orchestration only.
      Suite green.
- [x] Task 4: Register `ModePipelineRunner` in `Program.cs`; update `GoldenScenarioHost.cs`.
- [x] Task 5: Unit tests per AC6 and AC8.
- [x] Task 6: Verify AC1 explicitly (`git status` on `Expected/`) and record it.

## Dev Notes

- **The R3a benefit is the single most fragile number in this story.** It is a difference of two
  full Level 1 runs; a changed argument on either run moves it. Copy both calls as a block.
- `BuildBatteryDetails` returns **null** (not an empty panel) when the battery is inactive or no mode
  contributed — decision G2/B10, `CalculatorService.cs:241–247`. A null-vs-empty slip is a visible
  contract change and will fail AC1.
- `BatteryModeAdapter` is pure and synchronous — make it `internal static`, consistent with the
  Story-A builders. No interface (design §4).
- Do **not** merge `HotelPeakShavingKw` into `BatteryModeAllocation.PeakShavingBandKw`: they are
  different quantities (the former excludes thrust-side loads). Move it; do not unify it.
- Test-run workaround (I5): `dotnet test -p:BaseOutputPath=<temp>`.

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (claude-opus-5[1m])

### Completion Notes

- **`CalculatorService`: 261 → 129 lines** (534 before story R-A). Four dependencies instead of
  seven. What remains is exactly orchestration: pricing configs, sail, run the modes, hand the
  results to the builders, attach warnings. AC5's ceiling was 150.
- **Disclosed design decision — `ModePipelineResult` and `BatteryModeOutcome` became `public`.**
  R-A made them `internal` because they were an internal detail; R-B turns them into the return type
  of a DI-registered service. A public constructor cannot accept a less-accessible parameter type
  (CS0051), so the choice was: public contract, or an internal `CalculatorService` with a
  non-public constructor that DI cannot activate. Public is also what every other service contract
  in this codebase does. No wire impact — neither type is serialized into any response.
- **The R3a reference run was moved as one block**, per the Dev Notes. Its three fragile properties
  are now each pinned by a direct test: zero budget override, `baselineIndex: null` on the reference
  run, and the `Math.Max(0, …) * modeHours` clamp reaching `BatteryModeOutcome`.
- **`HotelPeakShavingKw` was moved, not unified** with `BatteryModeAllocation.PeakShavingBandKw`.
  They are different quantities — the former excludes thrust-side loads — and the new
  `HotelPeakShavingKw_CountsOnlyElectricalPeakShavingRows` test makes that difference explicit
  rather than implicit.
- **`BatteryModeAdapter` is `internal static`**, consistent with the R-A builders: it is pure, it
  will never be substituted, and it is not a DI service.
- Test-infrastructure updates (allowed by I3): `GoldenScenarioHost` and `TestServiceFactory` now
  compose the runner; `EngineFuelCo2Tests` uses `factory.PipelineRunner`. No test was weakened —
  the changes are constructor plumbing only.
- Nothing was committed.

### Debug Log References

| Run | Result |
|---|---|
| Baseline (end of story R-A) | **282/282 green** |
| After Tasks 1–4 (extraction + rewiring) | **282/282 green**, 373 ms · `Expected/` diff empty |
| After Task 5 (runner + adapter tests) | **294/294 green**, 374 ms — 12 new, all passing first run |

AC1 verified explicitly: `git status --porcelain KSailCalc.Tests/Golden/Expected/` → empty.
AC2 verified: `git status --porcelain cl/ Models/` → empty.
AC3 verified: no `BatteryL1Adjustment`, `IsThrustSide`, `BatteryFunction` or `BatteryDetails`
construction remains in `CalculatorService` — only the `BatteryModeAdapter.BuildBatteryDetails` call.
AC4 verified: no `ILevel1/2/3OptimizationService` or `IBatteryAllocationService` reference remains
in `CalculatorService`.

### File List

New:
- `Services/Battery/BatteryModeAdapter.cs` (`ToAdjustment`, `HotelPeakShavingKw`, `BuildBatteryDetails`)
- `Services/ModePipelineRunner.cs`
- `Services/Interfaces/IModePipelineRunner.cs`
- `KSailCalc.Tests/Services/ModePipelineRunnerTests.cs` (12 tests: 7 runner + 5 adapter)

Modified:
- `Services/CalculatorService.cs` (261 → 129 lines; battery region and pipeline removed)
- `Services/Results/ModeResults.cs` (`ModePipelineResult`, `BatteryModeOutcome` → public)
- `Program.cs` (`IModePipelineRunner` → `ModePipelineRunner`, Scoped)
- `KSailCalc.Tests/Golden/GoldenScenarioHost.cs` (composes the runner — I3)
- `KSailCalc.Tests/TestHelpers/TestServiceFactory.cs` (exposes `PipelineRunner`)
- `KSailCalc.Tests/Services/EngineFuelCo2Tests.cs` (two constructor call sites)

Deleted: none.

### Change Log

| Date | Change |
|---|---|
| 2026-08-03 | R-B implemented: battery translation → `BatteryModeAdapter`, per-mode pipeline → `ModePipelineRunner`. `CalculatorService` 261 → 129 lines, 7 → 4 dependencies. 294/294 green, golden snapshots byte-identical. Status → Ready for Review. |

## QA Results

### Review Date: 2026-08-03

### Reviewed By: Quinn (Test Architect)

### Code Quality Assessment

The story's stated goal is met and then some: `CalculatorService` is 129 lines against an AC ceiling
of 150, with four dependencies instead of seven, and it now reads top-to-bottom as a description of
what a calculation *is*.

**"Moved verbatim" was verified mechanically.** I diffed both extractions against
`git show HEAD:Services/CalculatorService.cs` (HEAD still holds the pre-refactor 534-line original,
since nothing has been committed):

- *Battery region* → `BatteryModeAdapter`: zero logic differences. The only diff lines are the
  method signatures' `private` → `internal`.
- *`RunOptimizationPipelineAsync` + `RunL1Async`* → `ModePipelineRunner`: zero logic differences.
  The only diffs are the signature lines and the removed `#region` marker.

The R3a reference run — the number I flagged as most fragile in the Risk Assessment — survived
intact and is now pinned by a direct test rather than only by snapshots.

### Refactoring Performed

None. Nothing warranted it.

### Compliance Check

- Coding Standards: ✓ · Project Structure: ✓ (`Services/Battery/` and the runner sit where a reader
  would look for them)
- Testing Strategy: ✓ — 12 new tests, all passing first run
- All ACs Met: ✓ AC1–AC8

| AC | Verification |
|---|---|
| AC1 | `git status --porcelain KSailCalc.Tests/Golden/Expected/` → empty. 18/18 scenarios pass. |
| AC2 | `git status --porcelain cl/ Models/` → empty. |
| AC3 | No `BatteryL1Adjustment`, `IsThrustSide`, `BatteryFunction` or `BatteryDetails` construction in `CalculatorService`; only the adapter call remains. |
| AC4 | No `ILevel1/2/3OptimizationService` or `IBatteryAllocationService` in `CalculatorService`; `ModePipelineRunner` holds them and is registered Scoped. |
| AC5 | **129 lines** (ceiling 150). |
| AC6 | `RunAllModes_NeverTouchesTheBattery_WhenNoBatteryIsConfigured` — `Allocate` verified `Times.Never`, plus all Level 1 calls verified to receive a null adjustment. Positive control included (`Times.Exactly(2)` for Transit: real + reference). |
| AC7 | Independent run from a clean output path: **294/294 green**, 616 ms. |
| AC8 | Adapter: thrust/hotel split, hotel band excluding thrust-side, reserve rows excluded, null-vs-empty panel. Runner: L2/L3 Transit-only, pinned baseline Transit-only. |

### The public-visibility change — reviewed and accepted

`ModePipelineResult` and `BatteryModeOutcome` went from `internal` (R-A) to `public`. The Completion
Notes disclose it and the reasoning holds: a public constructor cannot take a less-accessible
parameter (CS0051), and the alternative — an internal `CalculatorService` with a constructor DI
cannot activate — is strictly worse. Neither type is serialized, so there is no wire impact, and
public service contracts are this codebase's existing convention. Accepted.

### Improvements Checklist

- [ ] **Low / future: no test validates the DI container.** The golden host and `TestServiceFactory`
      both compose services by hand, so a missing or mis-scoped registration in `Program.cs` would
      reach production as a startup failure or a 500 with a green suite behind it. This story added
      a registration and got it right — I verified all four of `ModePipelineRunner`'s dependencies
      are registered and that Scoped→Singleton (`IBatteryAllocationService`) is the safe direction —
      but the *class* of bug is now live and worth a cheap guard: either
      `ValidateOnBuild`/`ValidateScopes` in `Program.cs`, or one test that builds the collection and
      resolves `ICalculatorService`. Not a defect in this story; raising it because R-D will change
      these signatures again.

### Notable Judgement Calls (endorsed)

- **`HotelPeakShavingKw` moved, not unified** with `PeakShavingBandKw`. The Dev Notes explicitly
  forbade unifying them and the dev complied — then went further and added a test that makes the
  distinction (thrust-side exclusion) explicit. That test is worth more than the extraction.
- **`BatteryModeAdapter` kept `internal static`** while the runner became a public DI service. The
  line is drawn in the right place: pure translation stays internal, orchestrated behaviour becomes
  a contract.

### Security / Performance

No concerns. Same call sequence, same number of service invocations, no new allocation. Scoped →
Singleton dependency direction is safe (no captive dependency).

### Gate Status

Gate: **PASS** → docs/qa/gates/refactor.b-battery-and-pipeline.yml
(Independent full-suite run: **294/294 green**, 616 ms; golden snapshots byte-identical.)

### Recommended Status

✓ Ready for Done.

## Risk Assessment

- **Primary:** the R3a reference run drifts (different baseline index, different override, different
  clamp) and the battery benefit changes. **Mitigation:** AC1 — the battery golden scenarios pin
  `batteryDetails.benefitFocTonPerYear` exactly.
- **Secondary:** mode iteration order changes and `modeBreakdowns` reorders in the response.
  **Mitigation:** the array order is inside the snapshots.
- **Rollback:** single commit, `git revert`.
