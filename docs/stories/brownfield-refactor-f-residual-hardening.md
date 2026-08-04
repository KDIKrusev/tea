# Story: Refactor F — Residual hardening (the follow-ups the A–E gates carried)

<!-- Source: the "future" recommendations in docs/qa/gates/refactor.{a..e}-*.yml -->
<!-- Context: Brownfield refactoring of KSailCalc.Api. Behaviour-preserving. Closes the epic. -->

## Status: Ready for Review

## Story

As a **developer who will change this backend after the refactoring epic**,
I want **the three hardening items the A–E gates kept carrying forward to be closed**,
so that **the safety properties the epic relied on are enforced by the suite instead of by whoever
happens to remember them**.

## Context Source

- `docs/qa/gates/refactor.b-*.yml` and `refactor.d-*.yml`: no test validates the DI container.
- `docs/qa/gates/refactor.c-*.yml`: `AllValidCombinations` assertions may be silently vacuous.
- `docs/qa/gates/refactor.e-*.yml`: the repository's ordinal-based reader mapping.
- **Explicitly excluded:** deferred item §7.1 (`GetSfocForLoadAsync`'s catch-all). That is a
  behaviour change and Kamen's standing instruction for this epic is that no calculation or
  behaviour logic changes. It stays deferred.

## Scope

### F1 — A guard for the composition root

Both existing harnesses (`GoldenScenarioHost`, `TestServiceFactory`) build the service graph by hand,
so a missing or mis-scoped registration in `Program.cs` reaches production behind a green suite. The
epic changed constructor signatures in four stories and added one service.

A test that mirrors `Program.cs`' registrations and builds the container with `ValidateOnBuild` and
`ValidateScopes`, plus resolution checks for every registered service.

### F2 — Close the vacuity question, with the actual numbers

The R-C gate estimated "49 assertion sites". The real picture, measured:

- `OnlyContain` **fails** on an empty collection in FluentAssertions, so no assertion was vacuous.
- 12 sites can be weakened by a small candidate set; 8 already carry an explicit count.
- 4 had no count guard. Measured: two run against a **1-combination** plant, two against a
  **2-combination** plant.

Add the measured count to each, and say so in a comment where the surviving set is a single item —
so a test named "…AreExcluded" cannot read as stronger than it is.

### F3 — Read result columns by name

`reader.GetInt32(6)` is correct only until someone edits the SELECT list; insert a column in the
middle and every later read silently shifts. This repository has **no automated coverage** (the
loaders need a live SQL Server), so that failure mode is silent and permanent.

Equivalence argument: all three SELECT statements list plain column names with no aliases or
expressions, so `GetOrdinal("X")` with those same strings is provably equivalent — a wrong name
would already have broken the query.

## Acceptance Criteria

1. **AC1 — Snapshots frozen (I1).** 18/18 golden scenarios pass; `Expected/` diff empty.
2. **AC2 — Wire contract frozen (I2).** No model property changed; no file under `cl/` modified.
3. **AC3 — Container validated.** A test builds the service collection with `ValidateOnBuild` and
   `ValidateScopes` and resolves `ICalculatorService` with its whole graph.
4. **AC4 — Counts measured, not invented.** Every added count assertion reflects the value the
   current code produces, obtained by running, and the two single-survivor cases carry a comment
   saying so.
5. **AC5 — No ordinal reads remain** in `HybridConfigRepository`, and every column name used is one
   the corresponding SELECT lists.
6. **AC6 — §7.1 untouched.** `GetSfocForLoadAsync` and its `catch` are unchanged.
7. **AC7 — Suite green**, no test deleted, skipped or weakened, 0 build warnings.

## Tasks / Subtasks

- [x] Task 1: F2 — measure the real counts, then assert them.
- [x] Task 2: F1 — DI container guard.
- [x] Task 3: F3 — name-based reader mapping, with the equivalence argument verified mechanically.
- [x] Task 4: Verify AC1, AC5 and AC6 explicitly and record it.

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (claude-opus-5[1m])

### Completion Notes

- **F2: the R-C gate's estimate was too pessimistic, and I corrected it rather than acting on it.**
  "49 sites" counted every `AllValidCombinations` reference, including explicit `.Count` assertions.
  `OnlyContain` also fails on an empty collection, so **nothing was vacuous** — the real weakness was
  four assertions with no count guard. I measured the counts by temporarily asserting an impossible
  value and reading the failures (1, 1, 2, 2), restored the files, then encoded the measured values.
  The two single-survivor cases carry a comment saying the `OnlyContain` beneath them checks exactly
  one combination.
- **F1** mirrors `Program.cs` rather than importing it — the API is a top-level program with no
  reusable composition method. The test says so and doubles as the reminder to keep the two in sync.
  It found no defect: the container was already valid.
- **F3: the equivalence was verified mechanically, not asserted.** Extracted every column name read
  in code (35) and every name listed in the three SELECTs, and diffed: **every name read appears in
  a SELECT**. My first check was wrong — `Get[A-Za-z]*` does not match `GetInt32` because of the
  digits — which silently excluded eight names. Re-ran with `[A-Za-z0-9]*`.
- **A caveat that must travel with F3:** the repository has no automated coverage, so this change is
  green-by-construction rather than green-by-test. The argument above is strong (a wrong name would
  break the SELECT), but **a smoke check against the dev database before deploy is still warranted** —
  one `GET /api/app-data/initial` is enough.
- §7.1 untouched, per Kamen's standing instruction.
- Nothing was committed.

### Debug Log References

| Run | Result |
|---|---|
| Baseline (end of story R-E) | **322/322 green** |
| After F2 + F1 | **336/336 green**, 433 ms |
| After F3 | **336/336 green**, 333 ms · `Expected/` and `cl/` diffs empty |

Verified explicitly:
- AC5: no `reader.GetXxx(<int>)` or `reader.IsDBNull(<int>)` remains; 35/35 names read appear in a SELECT.
- AC6: `GetSfocForLoadAsync` and its `catch` unchanged.
- Build: **0 warnings**.

### File List

New:
- `Repositories/SqlDataReaderExtensions.cs`
- `KSailCalc.Tests/Services/DependencyInjectionTests.cs` (14 tests)

Modified:
- `Repositories/HybridConfigRepository.cs` (all three mappers read by name)
- `KSailCalc.Tests/Services/Level1OptimizationServiceTests.cs` (3 count assertions)
- `KSailCalc.Tests/Services/Level1PtiTests.cs` (1 count assertion)

Deleted: none.

### Change Log

| Date | Change |
|---|---|
| 2026-08-03 | R-F implemented: DI container guard, measured count assertions closing the vacuity question, and name-based result-column mapping. 336/336 green, golden snapshots byte-identical. Epic follow-ups closed except §7.1, which remains deferred by instruction. |

## Risk Assessment

- **Primary:** F3 has no automated coverage. **Mitigation:** the mechanical name-vs-SELECT check
  above, plus a pre-deploy smoke check against the dev database (`GET /api/app-data/initial`).
- **Secondary:** F1 mirrors `Program.cs` and can drift from it. **Mitigation:** stated in the test's
  own doc comment; drift shows up as a resolution failure only if the mirrored list is updated, so
  this guards additions to the graph rather than removals.
- **Rollback:** three independent commits' worth of change; revert per item or as a whole.
