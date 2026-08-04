# Story: Refactor C — Level 1 internal structure (deduplicate the candidate loop, name the baseline policy)

<!-- Source: docs/refactoring/backend-refactor-design.md §5.3 -->
<!-- Context: Brownfield refactoring of KSailCalc.Api. Behaviour-preserving. Independent of A/B but sequenced after them. -->

## Status: Ready for Review

## Story

As a **developer reading why a given engine combination was accepted or rejected**,
I want **the candidate evaluation to compute each quantity once, and the baseline-selection policy to
have a name of its own**,
so that **the plant arithmetic cannot be corrected in one place and left stale in the other, and the
D1 "third highest" battery rule is directly testable instead of buried in an inline ternary**.

## Context Source

- Design §5.3.
- `Level1OptimizationService.IsValid` (lines 190–193) computes `sgCapacity`, `aeCapacity`, `sgPower`,
  `aePower`; `DistributeLoad` (lines 213–227) then computes **the same four expressions again** for
  every candidate that passed. This is both duplicated knowledge and duplicated work in the hot loop.
- The baseline choice (lines 112–119) mixes three rules — the default "last (highest FOC)", the D1
  battery "third highest, clamped for small lists", and the user-pinned `baselineIndex` with its
  range check — in six lines of nested ternaries.

## Scope

### 1. Merge validity and distribution into one evaluation

Replace `IsValid(...)` + `DistributeLoad(...)` with a single `TryEvaluate(...)` that computes
`sgCapacity`, `aeCapacity`, `sgPower`, `aePower`, `meCapacity` **once** and either rejects the
candidate (recording the reason) or fills the combination.

**Rejection order is load-bearing and must be preserved exactly.** `RejectionTally.ExplainFor`
produces the user-facing "No feasible engine configuration: …" message, and which sentence the user
sees depends on which counter fired. The current order is:

1. structural — ME=0 in Transit/Maneuvering; SG installed but off; SG on without ME; SG on with zero
   SG capacity; the "SG variant would be valid" operational-realism rule; hotel not coverable by
   SG+AE; AE on but idle
2. PTI assist failure
3. AE above `MaxAuxLoadFraction`
4. ME insufficient power (and the ME=0-with-power structural case)
5. battery PTI discharge gate (also updates `BestAvailablePtiKw`)

`TryApplyPtiAssist` runs **after** distribution and **before** the AE-overload check — keep that
position.

### 2. Extract the baseline policy

`SelectBaseline(IReadOnlyList<EngineCombination> sorted, int? requestedIndex, bool hasBatteryAdjustment)`
returning the effective index. Encodes, unchanged:

- default with battery: `Math.Max(0, sorted.Count - 3)` (D1)
- default without battery: `sorted.Count - 1`
- user-pinned wins **only** when `requestedIndex is >= 0 and < sorted.Count`, otherwise the default

### 3. Explicitly not in scope

- The candidate **ordering** — `OrderBy(c => c.FocTonPerHour).ThenBy(c => c.ActiveMeCount + c.ActiveAeCount)` —
  must not change, including its stability. The baseline index is an index **into this list**; any
  reordering silently repoints every pinned baseline.
- `GenerateCombinations` — leave as is.
- Making `EngineCombination` immutable — deferred (design §7.4).
- The `async` signature — Story D handles that.

## Acceptance Criteria

1. **AC1 — Snapshots frozen (I1).** 18/18 golden scenarios pass; zero modified files under
   `KSailCalc.Tests/Golden/Expected/`; `GOLDEN_UPDATE` never set.
2. **AC2 — Wire contract frozen (I2).** No model property changed; no file under `cl/` modified.
   `Level1Details.ValidCombinations` (index, order, `ptiKw`) is identical.
3. **AC3 — Single computation.** `sgCapacity`, `aeCapacity`, `sgPower` and `aePower` are each computed
   exactly once per candidate. `IsValid` and `DistributeLoad` no longer both exist as separate passes
   over the same arithmetic.
4. **AC4 — Diagnostics preserved.** For each of the five rejection categories there is a test
   asserting the exact user-facing message from `RejectionTally.ExplainFor` — in particular the
   battery-PTI-gate message with its `required`/`available`/`configured` kW values and its
   `CultureInfo.InvariantCulture` formatting.
5. **AC5 — Baseline policy named and tested.** `SelectBaseline` exists with direct unit tests for:
   no battery + no pin; battery + no pin (incl. the clamp when `sorted.Count < 3`); valid pin;
   negative pin; pin ≥ `sorted.Count`.
6. **AC6 — Ordering unchanged.** A test asserts the sort expression is `FOC` then
   `ActiveMeCount + ActiveAeCount`, and that a pinned `baselineIndex` selects the same combination as
   before the change.
7. **AC7 — Suite green.** Full suite green; no test deleted, skipped or weakened.

## Tasks / Subtasks

- [x] Task 0: Baseline suite run recorded in the Debug Log.
- [x] Task 1: Add the diagnostics tests (AC4) **first**, against the current code — they are the
      characterization net for the rejection-order change.
- [x] Task 2: Extract `SelectBaseline` + its unit tests (AC5). Suite green.
- [x] Task 3: Merge `IsValid` + `DistributeLoad` into `TryEvaluate`, preserving the rejection order.
      Suite green.
- [x] Task 4: Verify AC1 explicitly (`git status` on `Expected/`) and record it.

## Dev Notes

- Task 1 before Task 3 is not optional. The golden snapshots cover the *successful* paths well; the
  rejection messages are covered only by the infeasible-plant scenarios. Write the message tests
  first, watch them pass on unchanged code, then refactor.
- The "SG OFF but the SG=ON variant would be valid" rule (lines 180–188) is operational-realism
  policy with a real comment explaining it. Move it as a block; do not restate the condition.
- `PlantLimits.PowerToleranceKw` appears in five comparisons with different directions
  (`>` capacity + tol, `<` requirement − tol). Preserve each direction exactly — flipping one turns a
  boundary configuration from feasible to infeasible.
- `TryApplyPtiAssist` mutates the combination on success (`PtiPowerKw`, `AvailablePtiKw`, `MePowerKw`,
  `AePowerKw`, both load percents) and sets `AvailablePtiKw` even on the early-return paths. Keep the
  early returns and the assignment order.
- Test-run workaround (I5): `dotnet test -p:BaseOutputPath=<temp>`.

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (claude-opus-5[1m])

### Completion Notes

- **Task ordering was followed literally.** The six diagnostics tests were written and run against
  the untouched code first (6/6 green before anything moved), then the loop was changed. That net
  asserts the **exact** message string for all four `ExplainFor` branches plus the precedence rule,
  where the pre-existing coverage was substring-based and reached only two of the four.
- **`IsValid` + `DistributeLoad` → `TryEvaluate`.** The four shared quantities (`sgCapacity`,
  `aeCapacity`, `sgPower`, `aePower`) are now computed once per candidate instead of twice. Every
  structural check kept its original position and still returns `false`, so the caller still counts
  them all as `Structural` — the rejection order the diagnostics depend on is unchanged.
- **Disclosed: `meCapacity` is still computed on three separate paths** — in `TryEvaluate`, in the
  loop's power-sufficiency check, and in `TryApplyPtiAssist`'s deficit branch. Each is a different
  code path with a different lifetime, and unifying them would mean threading state out of
  `TryEvaluate`. AC3 names the four quantities that were genuinely duplicated; `meCapacity` was not
  one of them, and I chose not to widen the story to chase it.
- **`SelectBaseline`** is `internal static` so it can be tested directly. The out-of-range guard is
  written as `requestedIndex is int index && index >= 0 && index < sorted.Count` rather than relying
  on nullable lifting — same result, no lifted-comparison subtlety to reason about.
- **Two of my own tests were wrong, not the code.** `ValidCombinations_AreOrderedBy…`,
  `TheOptimumIsTheFirstEntry…` and `APinnedBaselineSelects…` initially used the default builder,
  which yields a **single** valid combination — making every ordering assertion vacuously true and
  the pinned-baseline test impossible. Replaced with a plant that offers four combinations, plus
  explicit `Count > 1` and `SelectedBaselineIndex != 0` guards so the tests cannot silently become
  vacuous again.
- **Not touched, as scoped:** candidate ordering, `GenerateCombinations`, `EngineCombination`
  mutability, the `async` signature (story R-D).
- Nothing was committed.

### Debug Log References

| Run | Result |
|---|---|
| Baseline (end of story R-B) | **294/294 green** |
| Task 1 — diagnostics tests vs **unchanged** code | **6/6 green** — the characterization net holds |
| After Tasks 2–3 (`SelectBaseline` + `TryEvaluate`) | **300/300 green**, 366 ms · `Expected/` diff empty |
| After the baseline-selection tests | **314/314 green**, 330 ms |

AC1 verified explicitly: `git status --porcelain KSailCalc.Tests/Golden/Expected/` → empty.
AC2 verified: `git status --porcelain cl/ Models/` → empty.
AC3 verified: `sgCapacity`/`aeCapacity`/`sgPower`/`aePower` each appear once in the evaluation path;
`IsValid` and `DistributeLoad` no longer exist.

### File List

New:
- `KSailCalc.Tests/Services/Level1RejectionDiagnosticsTests.cs` (6 tests — exact messages + precedence)
- `KSailCalc.Tests/Services/Level1BaselineSelectionTests.cs` (14 tests — policy + ordering)

Modified:
- `Services/Level1OptimizationService.cs` (`IsValid` + `DistributeLoad` → `TryEvaluate`;
  baseline block → `SelectBaseline`)

Deleted: none.

### Change Log

| Date | Change |
|---|---|
| 2026-08-03 | R-C implemented: candidate validity and load distribution merged into one pass (four quantities computed once instead of twice); baseline policy extracted as `SelectBaseline` with the D1 rule named and tested. 314/314 green, golden snapshots byte-identical. Status → Ready for Review. |

## QA Results

### Review Date: 2026-08-03

### Reviewed By: Quinn (Test Architect)

### Code Quality Assessment

The riskiest story of the epic so far, executed with the right discipline. The Dev Notes said "write
the message tests first, watch them pass on unchanged code, then refactor" — and that is exactly
what the Debug Log records: **6/6 green against untouched code**, then the merge.

**The rejection order was verified mechanically, not by inspection.** Three diffs against
`git show HEAD:Services/Level1OptimizationService.cs`:

- *Structural checks* — identical conditions in identical order. The only added lines are
  `meCapacity` and `mePower`, i.e. the two values that legitimately migrated in from `DistributeLoad`.
- *Distribution assignments* — `combo.SgPowerKw`, `AePowerKw`, `MeLoadPercent`, `AeLoadPercent`
  identical; the computations that "disappeared" from the tail are the ones that moved earlier into
  the merged body, confirmed by the first diff.
- *Rejection counter sequence* — `grep -o 'rejections\.[A-Za-z]*++'` on both versions: **identical**.
  This is the direct evidence that AC4's precondition holds; the diagnostics tests then prove the
  observable result.

The `remainingHotel` local was inlined into `Math.Min(hotel - sgPower, aeCapacity)` — that is exactly
how `IsValid` already expressed it, so no arithmetic changed.

### Refactoring Performed

None.

### Compliance Check

- Coding Standards: ✓ · Project Structure: ✓ · Testing Strategy: ✓ (characterization-first)
- All ACs Met: ✓ AC1–AC7

| AC | Verification |
|---|---|
| AC1 | `Expected/` diff empty; 18/18 scenarios pass. |
| AC2 | `cl/` and `Models/` untouched. `Level1Details.ValidCombinations` shape unchanged. |
| AC3 | The four quantities each appear once in the evaluation path; `IsValid`/`DistributeLoad` gone. |
| AC4 | Exact-string tests for all four `ExplainFor` branches plus a precedence test — written pre-change and green pre-change. |
| AC5 | 8 direct `SelectBaseline` tests: no-battery default, D1 default, clamp across counts 1–4, valid pin, negative pin, over-range pin, pin-of-zero. |
| AC6 | Ordering asserted as `FOC` then `ActiveMe+ActiveAe` with `WithStrictOrdering`, plus a pinned-index test. |
| AC7 | Independent run from a clean output path: **314/314 green**, 566 ms. |

### The dev caught their own vacuous tests — and that matters beyond this story

Three of the new tests initially used `CalculatorInputBuilder.Default()`, which yields **exactly one**
valid combination. `BeInAscendingOrder` over a one-element list passes trivially; so does
"optimum is first and baseline is last" when they are the same object. The dev noticed, replaced the
fixture with a four-combination plant, and added explicit `Count > 1` and
`SelectedBaselineIndex != 0` guards so the tests cannot quietly become vacuous again.

That guard pattern is worth propagating: **49 assertion sites across 7 test files touch
`AllValidCombinations`**, and any of them built on the default input carries the same silent-vacuity
risk. Not a defect in this story — raising it as a cheap, high-value audit.

### Improvements Checklist

- [ ] **Medium / future: audit the other `AllValidCombinations` assertions for vacuity.** 49 sites
      across `Level1OptimizationServiceTests`, `Level1PtiTests`, `CalculationTraceTests`,
      `CalculatorServiceBatteryTests` and `BatteryTestDesignScenarioTests`. Any built on the
      single-combination default input asserts nothing. Cheap to check, and the fix is the guard the
      dev already demonstrated.
- [ ] **Low / future: `meCapacity` is still computed on three paths** — `TryEvaluate`, the loop's
      sufficiency check, and `TryApplyPtiAssist`. Disclosed in the Completion Notes and correctly
      excluded from AC3's four named quantities. Unifying it means threading state out of
      `TryEvaluate`; the right time is whenever `TryApplyPtiAssist` is next opened.

### Security / Performance

Improvement, not a regression: the four shared quantities are computed once per candidate instead of
twice, inside the loop that dominates Level 1's cost. No behaviour change.

### Gate Status

Gate: **PASS** → docs/qa/gates/refactor.c-level1-structure.yml
(Independent full-suite run: **314/314 green**, 566 ms; golden snapshots byte-identical.)

### Recommended Status

✓ Ready for Done.

## Risk Assessment

- **Primary:** rejection order changes and a user sees a different (less useful, or plain wrong)
  explanation for an infeasible plant. **Mitigation:** AC4's message tests, written before the change.
- **Secondary:** an unintended reorder repoints every pinned `baselineIndex`, changing the baseline
  FOC for every scenario that pins one. **Mitigation:** AC6 plus the golden scenarios that carry a
  pinned baseline.
- **Tertiary:** a merged tolerance comparison flips direction at a boundary. **Mitigation:** the
  golden scenarios include tight-capacity and PTI-gate configurations.
- **Rollback:** single commit, `git revert`.
