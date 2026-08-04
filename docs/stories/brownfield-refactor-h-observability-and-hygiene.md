# Story: Refactor H — Observability and hygiene

<!-- Source: Architect review after R-G. Kamen's ask — "лесно да дебъгваме отделните неща" -->
<!-- Context: Brownfield. No calculation changes; golden snapshots stay frozen. -->

## Status: Ready for Review

## Story

As a **developer answering "why did this vessel get these numbers?"**,
I want **the calculation pipeline to leave a trace, every type to be findable by filename, and the
zero-warning state to be enforced**,
so that **a support question is a log grep instead of a debugging session, and the structure the
epic built cannot quietly erode**.

## Scope

### H1 — The pipeline was completely silent

Zero `ILogger` in `Services/Calculation/`, `Services/Battery/` or `Services/Results/`. A Debug-level
trace now records, per mode: hours, the chosen combination, the baseline and its index, the candidate
count; plus the battery allocation, the Transit L2/L3 outcome, and one summary line per calculation.

### H2 — Types that could not be found by filename

Split the multi-type files: `LevelDetails.cs` (4 types), `SailContributionModels.cs` (5, and it mixed
a wire type with internal ones), `AppData.cs` (4), `OperationalProfile.cs` (5), `PowerDemands.cs`,
`BatteryAllocation.cs`, `IRepositories.cs`, `IVesselResolutionService.cs`.

### H3 — No warning policy

Only `Nullable` was set. Enable `latest-recommended` analyzers and `TreatWarningsAsErrors`.

## Acceptance Criteria

1. **AC1** — Golden snapshots byte-identical; `cl/` untouched.
2. **AC2** — The trace is Debug level and is not built when Debug is off.
3. **AC3** — Every remaining multi-type file is a cohesive pair (enum + extensions, parent + row).
4. **AC4** — Build passes with `TreatWarningsAsErrors`; suppressions are individually justified.
5. **AC5** — Suite green, no test deleted or weakened.

## Tasks / Subtasks

- [x] Task 1: H2 — split the multi-type files.
- [x] Task 2: H1 — add the trace, with a test that proves it.
- [x] Task 3: H3 — analyzers, measure, fix the real findings, justify the suppressions.
- [x] Task 4: Verify AC1 explicitly.

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (claude-opus-5[1m])

### Completion Notes

- **The logging test earned its place immediately.** `TheTraceIsDebugLevel_SoItCostsNothingWhenDisabled`
  failed on the first run and was right to: `LogModeOutcome` was guarded with `IsEnabled`, but the
  Transit L2/L3 line and the summary line were not. That is exactly what CA1873 had flagged and I had
  suppressed. **I removed the suppression and fixed the code instead** — the analyzer was right and I
  was wrong to silence it.
- **The analyzer pass found a real defect, not just style.** CA1001: `HybridConfigRepository` and
  `SailContributionRepository` each own a `SemaphoreSlim` and were never disposable. Both are now
  `sealed` + `IDisposable`. Minor in practice (they are singletons for the process lifetime) but it
  is a genuine resource-ownership bug, and no human review had caught it across the whole epic.
- **CA1305** flagged `IntegrationLevelId.ToString()` building the tier keys — the keys are compared
  against the literals `"1"/"2"/"3"`, so a locale with digit substitution could break tier lookup.
  Now `ToString(CultureInfo.InvariantCulture)`.
- **68 warnings, honestly triaged.** 44 were CA1848 (LoggerMessage delegates) — suppressed with a
  written reason; a source-generated delegate per statement is not worth ~50 declarations here.
  CA1859 (prefer concrete `Dictionary` over `IReadOnlyDictionary`) suppressed — the read-only
  abstraction is the better contract. Everything else was fixed: CA1001 ×2, CA1305, CA1860 ×2,
  CA1822, CA1051, CA1873 ×4.
- **H2 stopped where the files became cohesive.** What remains multi-type is enum + extension methods,
  a response envelope named after its root type, or a parent + its row type. Splitting those would
  scatter things that belong together.
- Nothing was committed.

### Debug Log References

| Run | Result |
|---|---|
| Baseline (end of R-G) | **336/336 green** |
| After H2 (file splits) | **336/336 green** · `Expected/` empty |
| After H1 (trace) | **336/336 green** |
| Logging test added | **337/338 — one FAIL**, which found the unguarded log sites |
| After fixing the guards | **338/338 green**, 0 warnings with TreatWarningsAsErrors |

### File List

New: `Models/Level1Details.cs`, `Level2Details.cs`, `Level3Details.cs`, `ValidCombinationDto.cs`,
`ModePowerBreakdown.cs`, `BatteryModeAllocation.cs`, `AppInitialData.cs`, `FullVesselData.cs`,
`SailContributionResult.cs`, `Models/Domain/SailContributionLookup.cs`, `BatteryL1Adjustment.cs`,
`VesselResolution.cs`, `Repositories/Interfaces/IKSailCalcConfigRepository.cs`,
`ISailContributionRepository.cs`, `KSailCalc.Tests/Services/CalculationTraceLoggingTests.cs`

Modified: `CalculatorService.cs`, `ModePipelineRunner.cs` (trace), `AppDataAggregationService.cs`,
`Program.cs` (log guards), `HybridConfigRepository.cs`, `SailContributionRepository.cs` (IDisposable),
`BaseRepository.cs`, `CalculatorController.cs`, `ValidationService.cs`, `PowerInterpolationHelper.cs`,
`KSailCalc.Api.csproj` (analyzers), `TestServiceFactory.cs` (injectable loggers), 4 test files

Deleted: `Models/LevelDetails.cs`, `SailContributionModels.cs`, `AppData.cs`, `BatteryAllocation.cs`,
`Repositories/Interfaces/IRepositories.cs`

## QA Results

### Reviewed By: Quinn (Test Architect) — 2026-08-03

**Gate: PASS** → docs/qa/gates/refactor.h-observability-and-hygiene.yml

All 5 ACs verified: 338/338 green, 0 warnings under `TreatWarningsAsErrors`, golden snapshots
byte-identical, `cl/` untouched.

The story's own test caught the story's own defect — the unguarded log call sites — and the dev
responded by **removing a suppression they had just added** rather than keeping the test quiet. That
is the correct instinct and the reason the analyzer pass was worth doing at all.

Two findings in this story are defects the entire A–G epic missed: the undisposed `SemaphoreSlim` in
both repositories, and the culture-dependent tier key. Neither was reachable by the golden snapshots.
Worth noting for the record: **static analysis found what seven stories of human review did not.**

**Recommendation:** apply the same `AnalysisLevel` + `TreatWarningsAsErrors` to
`KSailCalc.Tests.csproj` — it is currently unanalyzed, and test code is where sloppiness hides.
