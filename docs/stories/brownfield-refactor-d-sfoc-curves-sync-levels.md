# Story: Refactor D — Pre-resolved SFOC curves; the optimization levels become synchronous

<!-- Source: docs/refactoring/backend-refactor-design.md §5.4 -->
<!-- Context: Brownfield refactoring of KSailCalc.Api. Behaviour-preserving. Sequenced after C. -->

## Status: Ready for Review

## Story

As a **developer reading and profiling the optimization levels**,
I want **the two SFOC curves resolved once per calculation and passed down as data, so Levels 1, 2
and 3 become synchronous pure functions**,
so that **there is no `await` inside the candidate loop, no per-call re-filtering and re-sorting of a
constant curve, and each level can be tested as `input → output` with no mocked async service**.

## Context Source

- Design §5.4, including the numerical-equivalence argument.
- `SfocService.GetSfocForLoadAsync` (`Services/SfocService.cs:44`) executes
  `sfocData.Where(p => p.Load > 0).OrderBy(x => x.Load).ToList()` on **every call** — a fresh
  allocation and sort of constant data — preceded by a linear `FirstOrDefault` over the engine list.
  It is called once per main-engine evaluation and once per aux evaluation inside the Level 1
  candidate loop (`Level1OptimizationService.cs:340, 347`) and twice per generator in Level 3.
- Levels 1, 2 and 3 are `async` **only** because this lookup is `async`, and the lookup is `async`
  only because it reads a memory-cached object. There is no I/O on the path.
- Level 2 already does the right thing: `GetFilteredAeSfocDataAsync` (`Level2OptimizationService.cs:301–305`)
  pre-fetches the curve once and interpolates synchronously through `SfocInterpolationHelper`.

## Scope

### 1. Resolve the curves once

An `EngineFuelCurves` value (main curve + aux curve, each already filtered to `Load > 0` and sorted
ascending) resolved at the start of the calculation and passed down to the levels.

Only **two** curves are ever needed: main by `input.MainEngineTypeId`, aux by `input.AuxEngineTypeId`.
Level 3 maps SG to the *Main* category via `GeneratorType.ToEngineCategory()`, so the SG generator
uses the main curve — no third curve exists on any path.

### 2. Levels become synchronous

`ILevel1OptimizationService`, `ILevel2OptimizationService` and `ILevel3DrcService` lose `async`/`Task`
and take the resolved curves. `ModePipelineRunner` (Story B) calls them synchronously.
`CalculatorService.CalculateAllVariantsAsync` stays `async` — it still awaits the config repository
and the sail service.

### 3. `SfocService` tidy-up (structure only)

Its two lookup paths — the `if/else` in `GetSfocForLoadAsync` (lines 31–40) and the `switch` in
`GetSfocDataAsync` (lines 64–71) — are the same selection written twice in two styles. Collapse to
one. `SfocService` itself stays: it is the composition point over `IAppDataAggregationService`.

**The `catch (Exception)` at lines 52–57 is NOT touched in this story** — narrowing it changes
behaviour and is deferred (design §7.1).

### 4. Numerical equivalence — verify, do not assume

The equivalence rests on two claims the dev must confirm in code before relying on them:

1. **Happy path:** `GetSfocForLoadAsync` filters `Load > 0`, sorts ascending, calls
   `SfocInterpolationHelper.Interpolate` — identical to what a pre-resolved curve does.
2. **Missing-curve path:** `GetSfocForLoadAsync` returns `SfocInterpolationHelper.DefaultSfocFallback`
   (220 g/kWh) when the engine has no data; an **empty** pre-resolved curve makes `Interpolate` return
   the same constant (`SfocInterpolationHelper.cs:19–20`). The fallback must therefore stay reachable
   through the empty-curve path — do not add a guard that throws instead.

## Acceptance Criteria

1. **AC1 — Snapshots frozen (I1).** 18/18 golden scenarios pass; zero modified files under
   `KSailCalc.Tests/Golden/Expected/`; `GOLDEN_UPDATE` never set. This is the story where AC1 carries
   the most weight — it is the proof that pre-resolution is numerically identical.
2. **AC2 — Wire contract frozen (I2).** No model property changed; no file under `cl/` modified.
3. **AC3 — Curves resolved once.** For a single `CalculateAllVariantsAsync` call, the SFOC curve for
   a given engine type is filtered and sorted **exactly once** — asserted via a counting test double
   over `IAppDataAggregationService` (or an equivalent instrumented seam).
4. **AC4 — Levels synchronous.** `ILevel1OptimizationService`, `ILevel2OptimizationService` and
   `ILevel3DrcService` expose no `Task`-returning members. No `await` remains inside the Level 1
   candidate loop or the Level 3 generator loop.
5. **AC5 — Missing-curve behaviour preserved.** A test with an engine type that has no SFOC data
   produces the same numbers as before the change (220 g/kWh fallback), for both ME and AE, at
   Level 1, Level 2 and Level 3.
6. **AC6 — Exception behaviour untouched.** `SfocService`'s `catch (Exception)` is unchanged; the
   deferred item §7.1 remains deferred.
7. **AC7 — Suite green.** Full suite green; no test deleted, skipped or weakened. Existing async
   level tests are converted, not removed. `GoldenScenarioHost.cs` updated for the new composition
   (allowed by I3).

## Tasks / Subtasks

- [x] Task 0: Baseline suite run recorded in the Debug Log.
- [x] Task 1: Confirm in code both equivalence claims in Scope §4; note the confirmation in the
      Completion Notes (this is the story's safety argument, it must be written down).
- [x] Task 2: Introduce `EngineFuelCurves` + its resolution; unify the two `SfocService` lookup paths.
      Suite green.
- [x] Task 3: Convert Level 3 to synchronous. Suite green.
- [x] Task 4: Convert Level 2 to synchronous (it is already curve-based — mostly signature work).
      Suite green.
- [x] Task 5: Convert Level 1 to synchronous; remove the `await` from the candidate loop. Suite green.
- [x] Task 6: Tests for AC3 and AC5.
- [x] Task 7: Verify AC1 explicitly (`git status` on `Expected/`) and record it.

## Dev Notes

- Convert **Level 3 → Level 2 → Level 1**, in that order, with a green suite between each. Level 1 is
  the largest surface and the one with the hot loop; do it last, when the curve plumbing is proven.
- `SfocInterpolationHelper.Interpolate` takes `decimal loadPercentage` while the callers hold
  `double` and cast at the call site (`(decimal)combo.MeLoadPercent`). **Keep the cast exactly where
  it is.** Moving or removing a `double → decimal` cast changes rounding and will move the numbers.
- Level 2 already holds a pre-fetched `List<SfocDataPoint>`; its sweep is the reference shape for what
  Levels 1 and 3 should look like afterwards.
- Level 3's `GeneratorType.GetEngineTypeId(input)` selects the engine type per generator; after this
  change the curve selection must follow the same mapping (SG → main).
- Test-run workaround (I5): `dotnet test -p:BaseOutputPath=<temp>`.

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (claude-opus-5[1m])

### Completion Notes

**Task 1 — the equivalence argument, confirmed in code before relying on it (Scope §4):**

1. *Happy path.* `GetSfocForLoadAsync` selected the engine's `SfocData`, filtered `Load > 0`, sorted
   ascending and called `SfocInterpolationHelper.Interpolate`. `GetCurvesAsync` performs the same
   three steps once per engine type. Confirmed identical.
2. *Missing-curve path.* Confirmed **three** sub-cases, not two: engine not found → null →
   fallback; engine found with an empty curve → fallback; engine found whose points are **all**
   `Load <= 0` → `workingPoints` empty → `Interpolate` returns the fallback. A pre-resolved empty
   curve reproduces all three, because `Interpolate` returns `DefaultSfocFallback` (220) for an
   empty list. No guard was added that would throw instead — that is asserted by
   `AMissingCurve_YieldsTheFallbackSfoc_NotAnException` and by the end-to-end fallback test.

**A third case the story did not anticipate — the exception path.** Deliberately *not* wrapped in a
`try/catch` at the resolution point, and the reasoning matters:

- Today an app-data failure makes `GetSfocForLoadAsync` swallow and return 220 (Level 1 quietly
  produces garbage), but `GetSfocDataAsync` — which Level 2 uses — does **not** catch. Level 2 runs
  for Transit on every calculation, so the request already ends as a 500.
- Adding a catch at the resolution point would have turned that 500 into a **200 with plausible
  wrong numbers** — a behaviour change in the worst possible direction for a calculator.
- Letting it propagate reproduces today's net observable exactly: 500 on an outage, 220 on a
  genuinely missing engine curve.

**§7.1 is now cheaper to decide, and the decision surface shrank.** After this story
`GetSfocForLoadAsync` — the method with the swallow-everything `catch` — is no longer called by any
production path; only tests reach it. Its `catch` block is byte-identical to HEAD (AC6 verified by
diff), so nothing was decided here. But the deferred question is no longer "should the calculator
swallow SFOC failures" — it is now just "delete this method or narrow it". **Flagging for
Winston/Kamen rather than deciding it.**

**Other notes:**

- The cast stayed exactly where the Dev Notes demanded. `EngineFuelCurves.Sfoc` takes a `decimal`
  precisely so every call site still reads `curves.Sfoc((decimal)combo.MeLoadPercent, …)` — verified
  by grep across all three levels. A `double` parameter would have been nicer to look at and would
  have moved the rounding.
- Conversion order was Level 3 → Level 2 → Level 1 as prescribed, with a green suite after each.
- `Level3DrcServiceTests` mocked `ISfocService` with a piecewise-linear **function**. It now supplies
  the identical breakpoints as **data**, which `SfocInterpolationHelper` interpolates the same way.
  This was the one fixture change with real numeric risk (different association order in the two
  interpolation expressions) — the suite confirmed no assertion moved.
- `ModePipelineRunner.RunAllModesAsync` keeps exactly one `await`: the curve resolution. Everything
  below it is synchronous, so `RunOptimizationPipeline` and `RunL1` lost their `Task` wrappers too.
- Scope: 144 call sites across 14 files. All mechanical; no test deleted, skipped or weakened.
  Exception tests moved from `ThrowAsync`/`await` to `Throw` because the lambdas are now
  `Func<Level1Result>`. Build is clean — **0 warnings**.
- Nothing was committed.

### Debug Log References

| Run | Result |
|---|---|
| Baseline (end of story R-C) | **314/314 green** |
| D1 — `EngineFuelCurves` + `GetCurvesAsync` (additive) | **314/314 green**, 296 ms |
| D2 — Level 3 synchronous | **314/314 green**, 296 ms · `Expected/` diff empty |
| D3 — Level 2 synchronous | **314/314 green**, 359 ms |
| D4 — Level 1 synchronous | **314/314 green**, 343 ms · `Expected/` diff empty |
| Task 6 — curve tests added | **318/318 green**, 400 ms — 4 new, all passing first run |

Verified explicitly:
- AC1: `git status --porcelain KSailCalc.Tests/Golden/Expected/` → empty at every checkpoint.
- AC2: `git status --porcelain cl/` → empty.
- AC4: no `Task` on `ILevel1OptimizationService`, `ILevel2OptimizationService`, `ILevel3DrcService`;
  **no `await` anywhere** in the three level implementations.
- AC6: `diff` of the `catch (Exception ex)` block against HEAD → identical.

### File List

New:
- `Models/EngineFuelCurves.cs`
- `KSailCalc.Tests/Services/EngineFuelCurvesTests.cs` (4 tests)

Modified (production):
- `Services/SfocService.cs` (`GetCurvesAsync`; the if/else and switch lookups collapsed into
  `RawCurve` + `WorkingPoints`; `GetSfocForLoadAsync`'s catch untouched)
- `Services/Interfaces/ISfocService.cs`, `ILevel1OptimizationService.cs`,
  `ILevel2OptimizationService.cs`, `ILevel3DrcService.cs`
- `Services/Level1OptimizationService.cs`, `Level2OptimizationService.cs`, `Level3DrcService.cs`
  (synchronous; `ISfocService` dependency removed from all three)
- `Services/ModePipelineRunner.cs` (resolves the curves; pipeline and `RunL1` now synchronous)

Modified (tests — mechanical, allowed by I3):
- `Golden/GoldenScenarioHost.cs`, `TestHelpers/TestServiceFactory.cs` (adds `Curves` / `CurvesFor`)
- `Services/Level1OptimizationServiceTests.cs`, `Level1PtiTests.cs`,
  `Level1RejectionDiagnosticsTests.cs`, `Level1BaselineSelectionTests.cs`,
  `Level2OptimizationServiceTests.cs`, `Level3DrcServiceTests.cs`,
  `Level3ResidualVariationTests.cs`, `CalculationTraceTests.cs`,
  `CalculatorServiceBatteryTests.cs`, `BatteryTestDesignScenarioTests.cs`,
  `BatteryExcelLoadInputTests.cs`, `ModePipelineRunnerTests.cs`

Deleted: none.

### Change Log

| Date | Change |
|---|---|
| 2026-08-03 | R-D implemented: SFOC curves resolved once per calculation; Levels 1, 2 and 3 are now synchronous pure functions with no `ISfocService` dependency. The per-call filter-and-sort is gone from the candidate loop. 318/318 green, golden snapshots byte-identical. Status → Ready for Review. |

## QA Results

### Review Date: 2026-08-03

### Reviewed By: Quinn (Test Architect)

### Code Quality Assessment

The largest and riskiest story of the epic — 144 call sites across 14 files — landed with the
snapshots byte-identical at every one of the four checkpoints. The prescribed conversion order
(L3 → L2 → L1, green between each) was followed literally, which is why a diff this size stayed
reviewable.

**The primary risk I flagged was the `double → decimal` cast, and it was verified directly.** I
extracted every cast from `git show HEAD:` and from the current sources:

| HEAD | Now |
|---|---|
| `(decimal)combo.MeLoadPercent`, `(decimal)combo.AeLoadPercent` | identical |
| `(decimal)loadPctUp`, `(decimal)loadPctDown` | identical |
| `(decimal)loads[i]`, `(decimal)sgLoad`, `(decimal)load` | identical |

Same seven casts, same expressions, same call sites. `SfocInterpolationHelper` is untouched. The
decision to give `EngineFuelCurves.Sfoc` a `decimal` parameter — uglier to read, but it keeps the
cast where it was — is exactly right for this story.

Level 3's SG → main-curve mapping still runs through `GeneratorType.ToEngineCategory()`, so the
shaft generator burns main-engine fuel as before.

### Refactoring Performed

None.

### Compliance Check

- Coding Standards: ✓ · Project Structure: ✓ · Testing Strategy: ✓
- All ACs Met: ✓ AC1–AC7

| AC | Verification |
|---|---|
| AC1 | `Expected/` diff empty at D2, D4 and final. 18/18 scenarios pass. |
| AC2 | `cl/` untouched. |
| AC3 | Two tests: `GetInitialAppDataAsync` called `Times.Once` per calculation (it was once per SFOC lookup before), and a spy proving `GetSfocForLoadAsync`/`GetSfocDataAsync` are `Times.Never` on the calculation path. |
| AC4 | No `Task` on any of the three level interfaces; **no `await` anywhere** in the three implementations. |
| AC5 | Fallback verified at all three levels, plus end-to-end. |
| AC6 | `diff` of the `catch (Exception ex)` block vs HEAD → identical. |
| AC7 | Independent run from a clean output path: **318/318 green**, 572 ms, **0 build warnings**. |

### The dev found a case the story did not anticipate — and got it right

Scope §4 listed two equivalence claims. The dev confirmed **three** missing-data sub-cases (engine
absent, curve empty, all points at `Load <= 0`) and then raised a fourth situation the story never
mentioned: **the exception path.**

The reasoning in the Completion Notes is correct and I verified it against the code. Today an
app-data failure is swallowed by `GetSfocForLoadAsync` (Level 1 silently uses 220) but **not** by
`GetSfocDataAsync`, which Level 2 calls on every Transit calculation — so the request already ends
as a 500. Wrapping the new resolution point in a `try/catch` would have converted that 500 into a
**200 carrying fabricated numbers**. Letting it propagate preserves today's net observable exactly.

This is the judgement call I would have wanted flagged, and it was flagged rather than decided.

### A structural consequence worth surfacing

`GetSfocForLoadAsync` — the method carrying the swallow-everything `catch` — now has **no production
caller**. Only tests reach it. The dev correctly left it byte-identical (AC6 forbade touching it) and
escalated instead of deciding.

The practical effect: deferred item **§7.1 got easier and smaller**. It is no longer "should the
calculator swallow SFOC failures on the hot path" — the hot path no longer goes through that method
at all. What remains is a narrow choice: delete the now-unused method, or narrow its `catch` and keep
it as a public convenience. Either is a small, low-risk change. **Recommend Winston takes this to
Kamen before R-E**, since leaving a production-dead method with a known-bad exception policy is the
kind of thing that gets rediscovered as a bug in six months.

### Improvements Checklist

- [ ] **Medium: decide §7.1 now that it is cheap** — delete `ISfocService.GetSfocForLoadAsync` (and
      its swallow-everything `catch`), or narrow the catch to the no-data case. No production caller
      remains; `SfocServiceTests` would move to `GetCurvesAsync`. Needs Kamen's call, not the dev's.
- [ ] **Low: the DI guard from R-B's gate is now overdue.** R-D changed four constructor signatures
      and both test harnesses compose by hand. `Program.cs` was updated correctly — I re-verified
      every registration resolves — but this is the second consecutive story where a mis-registration
      would have shipped behind a green suite.

### Security / Performance

Clear improvement. The per-call `Where().OrderBy().ToList()` is gone from the Level 1 candidate loop
and the Level 3 generator loop; the engine data is read **once per request** instead of once per SFOC
lookup. No async state machine per candidate. No behaviour change.

### Gate Status

Gate: **PASS** → docs/qa/gates/refactor.d-sfoc-curves-sync-levels.yml
(Independent full-suite run: **318/318 green**, 572 ms, 0 warnings; golden snapshots byte-identical.)

### Recommended Status

✓ Ready for Done — with the §7.1 decision raised to Winston/Kamen as a follow-up, not a blocker.

## Risk Assessment

- **Primary:** a moved or dropped `double → decimal` cast shifts interpolation rounding, changing
  numbers in the 6th decimal — enough to fail AC1, and the failure will look mysterious.
  **Mitigation:** the Dev Note above; AC1 catches it immediately.
- **Secondary:** the missing-curve fallback stops being reachable and a data gap starts throwing
  instead of returning 220. **Mitigation:** AC5.
- **Tertiary:** interface changes ripple into ~10 existing test files. **Mitigation:** mechanical;
  tests are converted rather than rewritten, and AC7 forbids deleting any.
- **Rollback:** single commit, `git revert`. Larger diff than A–C, but self-contained.
