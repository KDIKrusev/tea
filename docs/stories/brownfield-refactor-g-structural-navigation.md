# Story: Refactor G — Structural navigation (know which class is for what)

<!-- Source: Architect review after the A–F epic; Kamen's ask — "да си знаем кой клас или сървис за какво е" -->
<!-- Context: Brownfield refactoring of KSailCalc.Api. File and namespace moves only. No logic changes. -->

## Status: Ready for Review

## Story

As a **developer opening this codebase for the first time, or debugging it under time pressure**,
I want **the folder structure to say what each class is responsible for**,
so that **finding the right file is navigation rather than search, and the wire contract is visibly
separate from what is free to change**.

## Context Source

Architect review of the post-epic code. Three findings:

1. `Level1OptimizationService` had become the largest class (365 lines) with the same shape
   `CalculatorService` had before R-A — its `#region` markers again marking the seams.
2. The battery domain was split across two namespaces (`Services/BatteryAllocationService.cs` in
   `…Services`, `Services/Battery/BatteryModeAdapter.cs` in `…Services.Battery`) — an oversight in R-B.
3. `Models/` mixed five kinds of type with no way to tell the wire contract from the internals.

## Scope

### G1 — Split Level 1 into its four named parts

- `Level1CandidateBuilder` — which plant states exist (`Generate`) and how load distributes over
  them (`TryEvaluate`)
- `Level1PtiAssist` — shaft-motor assist physics (Excel I4, decisions D5 / D-C2)
- `Level1RejectionTally` — why nothing survived, in words the user can act on (a *presentation*
  concern that was living inside the optimizer)
- `Level1OptimizationService` — the pipeline only: resolve demand → generate → filter → cost → rank

### G2 — Group the services by responsibility

```
Services/Calculation/  CalculatorService, ModePipelineRunner, Level1*, Level2, Level3,
                       SailContributionService, SfocService
Services/Battery/      BatteryAllocationService, BatteryModeAdapter
Services/Catalog/      AppDataAggregationService, VesselResolutionService
Services/Validation/   ValidationService
Services/Results/      (unchanged)
Services/Helpers/      (unchanged)
Services/Interfaces/   (unchanged — one place to read every service contract)
```

### G3 — Separate the wire contract from the internals in `Models/`

```
Models/           the WIRE CONTRACT — serialized to the client; renaming a property breaks cl/
Models/Domain/    internal calculation types — never serialized, free to change
Models/Settings/  appsettings.json-bound configuration
Models/Enums/     (unchanged)
```

## Acceptance Criteria

1. **AC1 — Snapshots frozen.** 18/18 golden scenarios pass; `Expected/` diff empty.
2. **AC2 — Wire contract frozen.** No property added, removed, renamed or retyped; `cl/` untouched.
   Namespaces are not serialized, so a move cannot reach the wire.
3. **AC3 — Level 1 is a pipeline.** `Level1OptimizationService` contains no combination generation,
   no PTI physics and no message formatting.
4. **AC4 — One namespace per responsibility.** No service file remains in the bare
   `KSailCalc.Api.Services` namespace.
5. **AC5 — The `Models/` split is derived, not guessed.** Every type left in `Models/` is reachable
   from a controller request or response; every type moved to `Domain/` is not.
6. **AC6 — Suite green**, no test deleted, skipped or weakened, 0 build warnings.

## Tasks / Subtasks

- [x] Task 1: G1 — split Level 1. Suite green.
- [x] Task 2: G2 — group the services; move `BatteryAllocationService` beside its adapter. Suite green.
- [x] Task 3: G3 — derive the wire/internal split by reachability, then move. Suite green.
- [x] Task 4: Verify AC1 and AC2 explicitly.

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (claude-opus-5[1m])

### Completion Notes

- **G1: Level 1 went 365 → 201 lines**, with three named collaborators (115 / 61 / 64). The service
  now reads as *resolve demand → generate → filter → cost → rank*; `ResolveDemand` and
  `EvaluateCandidates` were extracted so the top-level method fits on one screen.
- **G3: my initial classification was wrong, and tracing reachability corrected it.** I had assumed
  the wire contract was the minority of `Models/`. Following what is actually reachable from the
  controllers showed the opposite: `BatteryModeAllocation`, `BatteryLoadAllocation`,
  `GeneratorSetpoint`, `EngineType`, `VesselType` and `OperationalProfile` are **all serialized** —
  via `BatteryDetails.ModeAllocations`, `Level2Details.OptimalSetpoints` and the app-data endpoints.
  Only 7 types are genuinely internal. So the split inverted: the wire contract **stays** in
  `Models/` as the default, and the internals moved out. Had I shipped the original plan, the folder
  names would have actively lied about which types are safe to change.
- **`Models/SailContributionModels.cs` is deliberately left in place** even though it mixes a wire
  type (`SailContributionResult`) with three internal ones. Splitting the file is churn for a marginal
  gain; noting it instead.
- **Churn was kept low with global usings.** `Models.Domain` and `Models.Settings` are imported
  globally in both projects, and the four service namespaces globally in the test project. The
  signal this story is about — "am I editing a wire type?" — comes from the *file's folder*, which is
  what the person editing it sees; it does not need to be repeated in every consumer's using block.
  Net effect: the test files now have **fewer** using lines than before.
- Nothing was committed.

### Debug Log References

| Run | Result |
|---|---|
| Baseline (end of story R-F) | **336/336 green** |
| After G1 (Level 1 split) | **336/336 green**, 392 ms · `Expected/` empty |
| After G2 (service grouping) | **336/336 green**, 517 ms |
| After G3 (Models split) | **336/336 green**, 438 ms · `Expected/` and `cl/` empty |
| Independent run, clean output path | **336/336 green**, 0 warnings |

### File List

New:
- `Services/Calculation/Level1CandidateBuilder.cs`, `Level1PtiAssist.cs`, `Level1RejectionTally.cs`
- `GlobalUsings.cs` (API project)

Moved (namespace changes only):
- 10 files → `Services/Calculation/`; `BatteryAllocationService` → `Services/Battery/`;
  2 files → `Services/Catalog/`; `ValidationService` → `Services/Validation/`
- 7 files → `Models/Domain/`; 2 files → `Models/Settings/`

Modified:
- `Services/Calculation/Level1OptimizationService.cs` (365 → 201 lines)
- `Program.cs`, `KSailCalc.Tests/GlobalUsings.cs`, and the test files' using blocks

Deleted: none.

### Change Log

| Date | Change |
|---|---|
| 2026-08-03 | R-G implemented: Level 1 split into four named parts; services grouped by responsibility; `Models/` separated into wire contract vs internal domain vs settings, with the split derived from reachability rather than assumed. 336/336 green, golden snapshots byte-identical. |

## Risk Assessment

- **Primary:** a mis-classified type in `Models/` would make the folder names lie. **Mitigation:** the
  split was derived by tracing reachability from the controllers, which corrected the initial plan.
- **Secondary:** namespace moves are broad. **Mitigation:** the compiler verifies every reference,
  and namespaces are not serialized — the wire cannot be affected. AC1/AC2 confirm.
- **Rollback:** file moves; `git revert` restores both paths and namespaces.
