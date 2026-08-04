# Story: Refactor A — Extract the pure result builders out of CalculatorService

<!-- Source: docs/refactoring/backend-refactor-design.md §5.1 (target structure §4) -->
<!-- Context: Brownfield refactoring of KSailCalc.Api. Behaviour-preserving. First of five (A→E). -->

## Status: Ready for Review

## Story

As a **developer maintaining the savings calculator**,
I want **the presentation and aggregation math extracted out of `CalculatorService` into named,
pure, independently testable builders**,
so that **a change to tier pricing, mode aggregation or the power-demands panel is a change to one
small file instead of a change somewhere inside a 534-line orchestrator**.

## Context Source

- Design §2 (non-goals), §3 (invariants I1–I6), §4 (target structure), §5.1 (this story).
- `Services/CalculatorService.cs` carries seven responsibilities; its `#region` markers map 1:1 onto
  the classes it should have been split into. This story extracts the three that are pure functions
  over data.
- Deliberate design choice (§4): these are `internal static` classes, **not** interfaces. They will
  never be substituted or mocked, and `CalculatorService` already has six constructor dependencies.

## Scope

### 1. Move — verbatim, no arithmetic retyped

| From `Services/CalculatorService.cs` | To |
|---|---|
| region *Power Demands*, lines 396–499 | `Services/Results/PowerDemandsBuilder.cs` |
| region *FOC & Savings Aggregation*, lines 265–293 | `Services/Results/SavingsAggregator.cs` |
| region *Result Building*, lines 295–394 (incl. `FinancialMetrics`, `CalculateFinancials`, `Co2ForEngines`, `Co2ForMainEngines`, `Co2ForAuxEngines`) | `Services/Results/TierResultBuilder.cs` |

`ModePipelineResult` and `FocBreakdown` become shared internal records under `Services/Results/`
(they are the builders' input contract). `CalculatorService` keeps producing them.

### 2. Collapse the duplication the move exposes

- **`TierSavings` → `SavingsBreakdown(double L1, double L2, double L3)`** with a
  `TotalUpTo(int highestLevel)` member. `Advanced == L1`, `Pro == L1+L2`, `Premium == L1+L2+L3` are
  **derived, not stored** — the current record stores three derived fields alongside their own inputs.
- **The triple copy-paste** at lines 151–159 (three near-identical
  `BuildVariantResult(new(input, foc, _settings, transitOptimal, new TierPlan(...)))` blocks) becomes
  a tier table plus one call per tier. A tier is `(string ConfigKey, int HighestLevel)`; which detail
  panels travel with it follows from `HighestLevel` (L2 details from level ≥ 2, L3 details from
  level 3) rather than being hand-passed per tier.
- `BuildResultContext` and `TierPlan` shrink to whatever the builder signature actually needs, or
  disappear if the signature reads clearly without them.

### 3. Out of scope for this story

Battery logic, the mode pipeline, Level 1/2/3 internals, `EngineCapacities`, the `configMap["1"]`
magic keys — all belong to stories B, C, D, E. Do not touch them here beyond what a mechanical move
requires.

## Acceptance Criteria

1. **AC1 — Snapshots frozen (I1).** All 18 golden scenarios pass. `git status` shows **zero**
   modified files under `KSailCalc.Tests/Golden/Expected/`. `GOLDEN_UPDATE` is never set.
2. **AC2 — Wire contract frozen (I2).** No property added, removed, renamed or retyped in
   `CalculatorInput`, `AllVariantsCalculationResult`, `VariantResult`, `PowerDemands` or
   `Level*Details`. No file under `cl/` is modified.
3. **AC3 — Extraction complete.** `Services/CalculatorService.cs` no longer contains the regions
   *Power Demands*, *FOC & Savings Aggregation* or *Result Building*. The three new files exist under
   `Services/Results/` and contain the moved logic.
4. **AC4 — Derived savings.** `TierSavings` no longer exists. `SavingsBreakdown` stores exactly three
   numbers; the per-tier totals are computed, and the three tiers are built from a table rather than
   three copy-pasted expressions.
5. **AC5 — Arithmetic untouched.** Every moved expression is byte-identical to its original apart
   from renamed parameters/receivers. Reviewer check: a diff of the moved bodies shows no changed
   operator, constant, cast, comparison or order of operations.
6. **AC6 — Suite green.** Full backend suite green — baseline **269/269**, verified 2026-08-03.
   No test is deleted, skipped or weakened. `GoldenScenarioHost.cs` may be updated if the
   `CalculatorService` constructor changes (allowed by I3).
7. **AC7 — Unit tests for the extracted units.** At least one direct unit test per new builder
   (`PowerDemandsBuilder`, `SavingsAggregator`, `TierResultBuilder`) — the point of extracting them is
   that they are now testable without the orchestrator.

## Tasks / Subtasks

- [x] Task 0: Record the pre-change baseline — run the full suite with the `BaseOutputPath`
      workaround (I5), note the pass count in the Debug Log.
- [x] Task 1: Create `Services/Results/` with the shared records (`ModePipelineResult`, `FocBreakdown`).
- [x] Task 2: Move region *Power Demands* → `PowerDemandsBuilder`. Suite green.
- [x] Task 3: Move region *FOC & Savings Aggregation* → `SavingsAggregator`. Suite green.
- [x] Task 4: Move region *Result Building* → `TierResultBuilder`. Suite green.
- [x] Task 5: Replace `TierSavings` with `SavingsBreakdown`; replace the triple copy-paste with the
      tier table. Suite green.
- [x] Task 6: Add the per-builder unit tests (AC7).
- [x] Task 7: Verify AC1 explicitly (`git status` on `Expected/`) and record it in the Debug Log.

## Dev Notes

- **Read `docs/refactoring/backend-refactor-design.md` §3 before starting.** I1 is not negotiable: if
  a snapshot changes, the refactor was not behaviour-preserving — stop and escalate to the Architect
  rather than re-approving the snapshot.
- The `meRatio` fallback chain in `BuildVariantResult` (lines 306–311) and the AE-load override from
  L2 setpoints (lines 324–331) are subtle and load-bearing. Move them as a block; do not "simplify"
  the nested conditionals.
- `CalculateWeightedLoadPercent` (441–467) deliberately weights only by the hours an engine type is
  actually active. The two near-identical ME/AE loops may be unified **only** if the result is
  provably identical for the zero-hours and zero-active-count cases.
- Test-run workaround (I5): `dotnet test -p:BaseOutputPath=<temp>`; the API locks `bin\`.

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (claude-opus-5[1m])

### Completion Notes

- **`CalculatorService`: 534 → 261 lines.** What remains is orchestration, the battery region and
  the sail/capacity helpers — all three are the subject of stories B and E, as designed.
- **The move was mechanical.** Every extracted expression was relocated without retyping: the
  `meRatio` fallback chain, the L2 AE-load override, `CalculateWeightedLoadPercent`'s two separate
  ME/AE loops and the `PowerDemands` assembly are byte-identical to their originals. The two
  near-identical load-percent loops were **deliberately left unmerged** — the Dev Notes allowed
  merging only if provably identical for the zero-hours/zero-active cases, and proving that was not
  worth the risk in a story whose entire value is "nothing changed".
- **`TierSavings` → `SavingsBreakdown(L1, L2, L3)`.** `TotalUpTo` is written as an explicit
  `switch` (`1 => L1`, `2 => L1 + L2`, `_ => L1 + L2 + L3`) rather than a conditional sum, so the
  floating-point association is identical to the old `l1 + l2 + l3` — a `+ 0.0` term would have been
  harmless here but the switch removes the question entirely.
- **Tier table.** `IntegrationTier` holds `(ConfigKey, HighestLevel)` with three static instances.
  Which per-level savings a tier reports and which detail panels travel with it now *derive* from
  `HighestLevel` instead of being hand-passed per tier — that is what removed the triple copy-paste.
- **`Co2Calculator`** was split out as its own `internal static` class inside `TierResultBuilder.cs`
  because the baseline CO2 fields in `CalculateAllVariantsAsync` need the same three helpers.
  Calling them as `TierResultBuilder.Co2For…` for a *baseline* figure would have read wrong.
- **`InternalsVisibleTo`** added to `KSailCalc.Api.csproj`. The builders are `internal` by design
  (§4: never substituted, no interface), and AC7 requires direct tests. This keeps the production
  surface unchanged rather than making the builders `public` to satisfy the test project.
- **`GoldenScenarioHost.cs` needed no change** — the `CalculatorService` constructor is untouched by
  this story. I3's allowance was not used.
- Nothing was committed.

### Debug Log References

| Run | Result |
|---|---|
| Baseline, before any change | **269/269 green**, 360 ms |
| After Tasks 1–5 (extraction complete) | **269/269 green**, 283 ms · `Expected/` diff empty |
| After Task 6 (builder tests added) | **282/282 green**, 394 ms — 13 new, all passing first run |

AC1 verified explicitly: `git status --porcelain KSailCalc.Tests/Golden/Expected/` → empty output.
AC2 verified: `git status --porcelain cl/ Models/` → empty output.

Note on the test-run workaround (I5): the three path-relative tests need a copy of
`appsettings.json` two levels above the redirected `BaseOutputPath`. Without it exactly those three
fail with `FileNotFoundException` and the other 266 pass — not a regression.

### File List

New:
- `Services/Results/ModeResults.cs` (`ModePipelineResult`, `BatteryModeOutcome`)
- `Services/Results/PowerDemandsBuilder.cs`
- `Services/Results/SavingsAggregator.cs` (`FocBreakdown`, `SavingsBreakdown`, `SavingsAggregator`)
- `Services/Results/TierResultBuilder.cs` (`IntegrationTier`, `TierInputs`, `TierResultBuilder`, `Co2Calculator`)
- `KSailCalc.Tests/Services/ResultBuildersTests.cs` (13 tests)

Modified:
- `Services/CalculatorService.cs` (534 → 261 lines; regions *Power Demands*, *FOC & Savings
  Aggregation* and *Result Building* removed; tier table replaces the triple copy-paste)
- `KSailCalc.Api.csproj` (`InternalsVisibleTo` → `KSailCalc.Tests`)

Deleted: none.

### Change Log

| Date | Change |
|---|---|
| 2026-08-03 | R-A implemented: three pure builders extracted to `Services/Results/`; `TierSavings` replaced by `SavingsBreakdown`; tier table replaces the triple copy-paste. `CalculatorService` 534 → 261 lines. 282/282 green, golden snapshots byte-identical. Status → Ready for Review. |

## QA Results

### Review Date: 2026-08-03

### Reviewed By: Quinn (Test Architect)

### Code Quality Assessment

A textbook behaviour-preserving extraction. `CalculatorService` drops from 534 to 261 lines and the
three regions become three named, pure, independently testable units.

**AC5 was verified mechanically, not accepted on assertion.** I diffed the moved bodies against
`git show HEAD:Services/CalculatorService.cs` with receivers normalised (`ctx.Tier.TotalSavingsTon`
→ `TOTAL`, `ctx.Foc` → `FOC`, and so on):

- *Power Demands* — every logic line identical; the only diffs are `private`/`internal` on the
  signatures and the method rename `BuildPowerDemands` → `Build`.
- *FOC & Savings* — all five FOC sums and all three savings sums identical; the only diff is the
  intended `TierSavings(6 fields)` → `SavingsBreakdown(3 fields)` construction.
- *Result Building* — the entire body between the tier derivation and the `VariantResult` initializer
  matched line for line: the `meRatio` fallback chain, both CO2 computations, the load percentages,
  the L2 AE override and all 20 property assignments. The only real additions are the six lines that
  derive `S1/S2/S3/TOTAL/L2D/L3D` from `HighestLevel` — which is the story's point.

No arithmetic was retyped. Nothing was "simplified while in the area".

### Refactoring Performed

None. Nothing warranted it.

### Compliance Check

- Coding Standards: ✓ · Project Structure: ✓ (new `Services/Results/` namespace is coherent)
- Testing Strategy: ✓ — 13 new unit tests across the three builders, all passing first run
- All ACs Met: ✓ AC1–AC7

| AC | Verification |
|---|---|
| AC1 | `git status --porcelain KSailCalc.Tests/Golden/Expected/` → empty. 18/18 scenarios pass. |
| AC2 | `git status --porcelain cl/ Models/` → empty. |
| AC3 | Regions *Power Demands* / *FOC & Savings* / *Result Building* absent; only *Battery*, *Engine Capacities*, *Sail* remain (stories B and E). |
| AC4 | `grep` for `TierSavings`, `BuildResultContext`, `TierPlan` across the solution → no hits. |
| AC5 | Normalised diff against HEAD, above. |
| AC6 | Independent run from a clean output path: **282/282 green**, 910 ms. Zero warnings on build. |
| AC7 | `ResultBuildersTests.cs` — 13 tests: 3 aggregator + 2 power demands + 8 tier builder. |

### Improvements Checklist

- [ ] **Low / future:** no test asserts directly that `IntegrationTier.Advanced/Pro/Premium` map to
      config keys `"1"/"2"/"3"`. The mapping *is* covered — the golden fixture's three pricing rows
      carry distinct prices (1.0M/100k, 1.205M/200k, 1.8M/300k), so a swapped key changes
      `totalInvestment`, `paybackPeriod` and `roi` in every snapshot. A direct assertion would only
      make the failure cheaper to diagnose, not catch anything new.
- [x] `InternalsVisibleTo` in the API csproj — reviewed and accepted. It is an assembly attribute, it
      ships no behaviour, and it is the right trade against making pure helpers `public` purely to
      satisfy the test project.

### Notable Judgement Calls (endorsed)

- **Leaving the two ME/AE loops in `CalculateWeightedLoadPercent` unmerged.** They look mergeable;
  proving equivalence at the zero-hours and zero-active-count boundaries was not worth the risk in a
  story whose entire value proposition is "nothing changed". Correct call.
- **`TotalUpTo` written as an explicit `switch`** rather than a conditional sum, preserving the
  floating-point association of the original `l1 + l2 + l3`. The `+ 0.0` variant would very likely
  have been harmless — but "very likely" is the wrong standard when 18 exact-match snapshots are the
  safety case.
- **`Co2Calculator` split out of `TierResultBuilder`.** A deviation from the letter of Scope §1,
  disclosed in the Completion Notes, and the better design: the baseline CO2 fields need the same
  three helpers, and `TierResultBuilder.Co2For…` would have read wrong at that call site.

### Security / Performance

No concerns. No new I/O, no new allocation in any loop, no surface change. `GoldenScenarioHost` did
not need touching — the `CalculatorService` constructor is unchanged, so I3's allowance went unused.

### Gate Status

Gate: **PASS** → docs/qa/gates/refactor.a-pure-builders.yml
(Independent full-suite run: **282/282 green**, 910 ms; golden snapshots byte-identical.)

### Recommended Status

✓ Ready for Done.

## Risk Assessment

- **Primary:** a "while I'm here" simplification silently changes a number. **Mitigation:** AC5 (no
  retyped arithmetic) plus AC1 (frozen snapshots) — the 18 scenarios cover battery, PTI, sail, DP,
  multi-mode and infeasible-plant paths.
- **Secondary:** the tier table changes which detail panel a tier reports. **Mitigation:** the golden
  snapshots contain `level2Details`/`level3Details` per variant, so a mis-wired tier fails loudly.
- **Rollback:** single commit, `git revert`. No schema, no config, no client change.
