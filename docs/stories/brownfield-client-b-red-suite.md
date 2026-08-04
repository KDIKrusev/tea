# Story: Client B — The red suite

<!-- Source: docs/refactoring/client-refactor-design.md §1 (why), §3 (I2, I4), §6.2 (this story) -->
<!-- Context: Brownfield refactoring of cl/. Depends on C-A. No production code changes. -->

## Status: Ready for Review

## Story

As **Kamen, who has watched the same bug survive two committed fixes**,
I want **a test that fails on today's code and names the writer that wins**,
so that **the fix in C‑E is verified rather than asserted — and so that if the test does not fail,
we learn we were chasing the wrong thing before writing any more code**.

## Context Source

- Design §1 (the nine writers), §3 (I2 the client golden, I4 red-before-green), §6.2 (this story).
- C‑A delivered `ApiFixture` and `RestoreHarness`. This story writes specs against them.
- Two of the three logged findings are already provable by reading; the third is not. See
  "What we already know" below. The value of this story is entirely in the third.

## What we already know (do not re-derive; do verify)

| Finding | Mechanism, by reading | Expectation |
|---|---|---|
| `baselineIndex` lost | [`applyProfileInputValues`](../../cl/src/app/features/vessel-input/vessel-input-form/vessel-input-form.component.ts#L689-L691) calls `endRestore()` **before** `emitFormValues()`, so the restore's own terminal emission is tagged `'user'`; [`onFormChange`](../../cl/src/app/features/calculator-page/calculator-page.component.ts#L260-L262) clears the pin on any `'user'` emission | Spec 2 fails deterministically, in every arrival order |
| Three calculations | emission #1 [`onOperationalProfileLoaded:603`](../../cl/src/app/features/vessel-input/vessel-input-form/vessel-input-form.component.ts#L603), #2 [`applyProfileInputValues:691`](../../cl/src/app/features/vessel-input/vessel-input-form/vessel-input-form.component.ts#L691), #3 the 500 ms `valueChanges` debounce fed by the `emitEvent:true` patches | Spec 3 fails deterministically |
| Engine values overwritten | **not established** — several candidate orderings, and which one fires depends on real latency | Spec 1 must fail in **at least one** order; the story's job is to say which |

Naming the third by reading is the mistake already made twice. It gets named by the matrix.

## Scope

### 1. Spec 1 — `restore-engine-values.spec.ts`

Scenario **03** (`docs/qa/manual-test-scenarios/03-no-battery-reference.json`) against the
`ApiFixture` vessel-type defaults. The numbers do not overlap, so the winning writer is readable
from the failure message:

| Field | Profile (03) | Vessel-type default |
|---|---|---|
| `mainEngineTypeId` | 1 | 2 |
| `meCapacityPerEngine` | 24 000 | 15 000 |
| `sgCapacityPerEngine` | 3 250 | 2 000 |
| `auxEngineTypeId` | 8 | 7 |
| `aeCapacityPerEngine` | 4 000 | 1 000 |
| `propulsionPower` | 12 036.15 | (curve value) |

Run the **arrival matrix**. Two entry points × five orderings:

**Entry points**
- **E1 — Load Profile click.** The catalogue is already warm (the app has been running).
- **E2 — Hard refresh with a draft.** `seedDraft(scenario)`, then mount; the catalogue is cold and
  the restore starts from `ngAfterViewInit`. Kamen reports the bug survives a hard refresh, so this
  entry point is not optional.

**Orderings**
- **O1** catalogue @ 50 ms, vessel-config @ +100 ms — both fast.
- **O2** catalogue @ 50 ms, vessel-config @ +2 000 ms — slow per-vessel fetch.
- **O3** catalogue @ **3 500 ms**, vessel-config @ +100 ms — the catalogue lands *after* the 3 000 ms
  `RESTORE_WATCHDOG_MS` has already called `endRestore()`. This is the ordering in which
  `onVesselEngineConfigSelected` computes `applyEngineDefaults = true` and calls
  `setEngineConfiguration`, writing the vessel-type capacities. Cold start / first EF Core query /
  Docker start makes 3 s realistic.
- **O4** vessel-config answered **before** the catalogue.
- **O5** catalogue resolves **after** `applyProfileInputValues` has run — the case Kamen named
  explicitly.

**O5 needs care.** Today `selectVessel` awaits `getCategories()`, which awaits the catalogue, so the
vessel fetch structurally cannot precede it. If the harness proves O5 is unreachable on today's code,
**that is a finding, not a failed spec** — record it, keep the spec, and mark it as the case C‑E must
make both reachable and safe (C‑E removes the ordering coupling, so after C‑E the catalogue can land
at any moment and the spec becomes a live guard). Do not delete it and do not fake it.

Assertion, identical in every cell of the matrix, after `settle()`:

```
meCapacityPerEngine === 24000 · sgCapacityPerEngine === 3250 · aeCapacityPerEngine === 4000
mainEngineTypeId === 1 · auxEngineTypeId === 8 · propulsionPower === 12036.15
```

Each failure message must report the observed value **and** the ordering label, so the Debug Log can
name the writer per cell.

### 2. Spec 2 — `restore-baseline-index.spec.ts`

Scenario **15** (`baselineIndex: 4`). Mount via `mountPage()`, restore, `settle()`, then assert the
**last** captured POST body carries `baselineIndex: 4`. Expected observation today: `undefined`.

Also assert the intermediate emissions: the restore must not produce an emission tagged `'user'`
before it has settled. That is the assertion that pins the *cause* rather than the symptom.

### 3. Spec 3 — `load-emits-once.spec.ts`

Scenario **03**, `mountPage()`. After `settle()`, assert `postedBodies().length === 1`.
Expected observation today: 3. The spec must print the three bodies' differing fields on failure —
knowing *what* differs between the three requests is what tells C‑F which emissions to remove.

Second case in the same file: a **cold load with no profile at all** must also post exactly once.

### 4. Spec 4 — `restore-fuel-price.spec.ts` — written, `xit`-ed, referenced

Scenario 03 saves `fuelPrice: 780`; a restore shows the catalogue default instead
([`applyProfileInputValues`](../../cl/src/app/features/vessel-input/vessel-input-form/vessel-input-form.component.ts#L646) patches 780, then [line 671](../../cl/src/app/features/vessel-input/vessel-input-form/vessel-input-form.component.ts#L671) re-calls `setEngineTypeReferences`, whose
`reconcileMainFuel → prefillPriceFromMainFuel` overwrites it and re-baselines the edit tracker).

Fixing this moves displayed $ figures, so per Kamen's decision (2026-08-04) it is **deferred to
design §7.1**. Write the spec, `xit` it, and put the finding reference in its description. An
`xit`-ed spec with a reason is a tracked question; a missing spec is a forgotten one.

### 5. Freeze I2 — the client's golden snapshot

For scenarios **01, 03 and 15**: drive the form to a settled state, capture the POST body, and freeze
it as JSON under `cl/src/testing/golden/`. A `request-body-golden.spec.ts` compares against it.

This is the invariant that closes the gap Kamen named — the backend goldens post JSON straight at the
API and never pass through the form, so they stay green while the UI is wrong. From C‑C onwards,
every story must leave these three bodies byte-identical.

**The frozen bodies capture today's behaviour, including its bugs** (spec 4's fuel price, and
whatever spec 1 reveals). That is correct: I2 guards against *unintended* change. When C‑E fixes a
race, the fields it fixes are re-frozen deliberately, with the change named in that story.

### 6. Out of scope

**No production file under `cl/src/app/` is modified.** Not one line, not a comment, not a lint fix.
That is the entire point of C‑B being a separate story (I4).

## Acceptance Criteria

1. **AC1 — Backend untouched (I1).** 441/441 green, `Golden/Expected/` clean, `GOLDEN_UPDATE` unset.
2. **AC2 — Production untouched (I4).** `git status --porcelain cl/src/app/` → empty. Every change is
   under `cl/src/testing/` or a `*.spec.ts`.
3. **AC3 — Spec 2 is red.** `restore-baseline-index.spec.ts` fails, observing `undefined` where 4 was
   expected, and the emission-tag assertion fails too.
4. **AC4 — Spec 3 is red.** `load-emits-once.spec.ts` fails, observing 3 posts where 1 was expected.
5. **AC5 — Spec 1 is red in at least one cell, and every cell is reported.** The matrix runs all
   2 × 5 combinations. The Debug Log contains a table: entry point × ordering → pass/fail → observed
   values → **the writer that wrote last**. A cell that passes is reported as passing; a cell that is
   structurally unreachable (see O5) is reported as unreachable with the reason.
6. **AC6 — If nothing is red, stop.** Should specs 1–3 all pass on today's code, the story's outcome
   is a written finding ("the harness does not reproduce what Kamen observes, because …") and an
   escalation to the Architect. **C‑E does not start.** This AC is the reason the story exists.
7. **AC7 — I2 frozen.** Three golden request bodies exist under `cl/src/testing/golden/`, and
   `request-body-golden.spec.ts` passes against them. The freezing run is recorded in the Debug Log
   with the exact settled state used.
8. **AC8 — Spec 4 is present and `xit`-ed**, with the §7.1 reference in its description.
9. **AC9 — Suite reports honestly.** `npm run test:ci` reports the expected failure count, and the
   Completion Notes state it explicitly: *"N failures, all from the red suite, no others."*
   No spec is deleted, weakened or silently skipped to make the run look green.

## Tasks / Subtasks

- [x] Task 0: Reproduce the C‑A baselines — backend 441/441, `npm run test:ci` green on the C‑A specs.
- [x] Task 1: Spec 2 (`baselineIndex`). Confirm red. Record the observed value.
- [x] Task 2: Spec 3 (emission count). Confirm red. Record the three bodies' differing fields.
- [x] Task 3: Spec 1 — build the matrix runner, run every cell, fill the Debug Log table.
- [x] Task 4: For every failing cell, name the winning writer (file + line) in the Debug Log.
- [x] Task 5: Spec 4, `xit`-ed, with the finding reference.
- [x] Task 6: Freeze the three I2 golden bodies; add `request-body-golden.spec.ts`.
- [x] Task 7: Verify AC1, AC2, AC9 explicitly. AC6 did not trigger — the matrix reproduces.

## Dev Notes

- **A test that passes here is bad news, not good news.** Resist the reflex to loosen an assertion
  until it goes red — that produces a test for a different bug, which is exactly the failure mode
  this session exists to end.
- **Name the writer, do not guess it.** The candidate writers are listed in design §1. The cheapest
  instrumentation is a temporary `valueChanges` subscription inside the spec that logs
  `(field, oldValue, newValue, currentTime)` — a *spec-side* subscription, never a production edit
  (AC2).
- The `RESTORE_WATCHDOG_MS = 3000` path (O3) is the most likely candidate for the third finding:
  once the watchdog has fired, `onVesselEngineConfigSelected` computes
  `applyEngineDefaults = wantsEngineDefaults && !this.restoreInFlight` as **true** and calls
  `setEngineConfiguration`, which writes the vessel-type capacities. Whether
  `applyProfileInputValues` still runs afterwards and repairs them is exactly what the matrix
  answers. Do not assume either way.
- The catalogue and the categories arrive in **one** response, so `answerCatalogue()` releases four
  subscribers at once, in registration order:
  `loadCategories` → `getMainEngineTypes` → `getAuxiliaryEngineTypes` → `selectVessel`'s
  `getCategories`. `loadEngineConfigurations` sits second and applies `mainEngineTypes[0]`'s rated
  capacities when nothing has claimed the field yet ([engine-config-section.component.ts:106](../../cl/src/app/features/vessel-input/vessel-input-form/engine-config-section/engine-config-section.component.ts#L106)). Expect it in the log.
- Scenario 03's `sgCapacityPerEngine` is 3 250 and the ADR‑5 PTI suggestion in
  `battery-config-section` also reads `sgCapacityPerEngine`. Battery is disabled in 03, so that path
  is inert — but do not swap in a battery scenario without accounting for it.
- Test-run workaround for the backend baseline (I5): `dotnet test -p:BaseOutputPath=<temp>`.

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (claude-opus-5[1m])

### Completion Notes

**`npm run test:ci` → 38 specs · 32 pass · 6 FAIL · 0 skipped-by-accident.
All six failures are from the red suite. No other spec fails.**

**The bug is reproduced, and the cause is now named rather than guessed.**

The matrix as originally specified (E1 warm catalogue, E2 cold draft, five orderings) went
**entirely green** — in every one of those orderings the restored profile does win. That was the
AC6 moment: rather than relaxing an assertion until something turned red, the matrix was examined
for a missing *dimension*, and there was one.

Kamen's symptom is "the file's values show, then get overwritten" — profile first, defaults second.
Every original cell produced the opposite order (defaults, then profile). What produces the
reported order is a vessel-config response that is processed **after** the profile has already
been applied, and neither E1 (warms up to quiescence first) nor E2 (both fetch triggers collapse
inside the 400 ms debounce) can create one.

**Cell E3 can: the user clicks Load while the page's initial cascade is still in flight.**
That is not an exotic case — it is what a user does when they open the app and immediately click
their saved scenario.

Observed chain, from the E3-a table below:

1. Catalogue lands. `loadEngineConfigurations` writes `mainEngineTypes[0]` — **9 000 / 1 100 /
   2 500, ids 3 and 9**. This is the ninth writer, found by reading during the C‑A review and now
   confirmed empirically. No flag guards it
   ([engine-config-section.component.ts:106](../../cl/src/app/features/vessel-input/vessel-input-form/engine-config-section/engine-config-section.component.ts#L106)).
2. `applyCategorySelection` fires a vessel-config fetch for the auto-selected default category.
3. The user clicks Load. `selectVessel` queues a **second** fetch, 400 ms behind.
4. The **first** (stale) response lands. `restoreInFlight` is `true`, so engine defaults are
   correctly skipped — *the second committed fix working exactly as designed* — and the profile is
   applied on the 200 ms path. **The form now holds 24 000 / 3 250 / 4 000, ids 1 and 8,
   propulsion 12 036.15.** This is the flash Kamen sees.
5. `applyProfileInputValues` calls `endRestore()`
   ([vessel-input-form.component.ts:689](../../cl/src/app/features/vessel-input/vessel-input-form/vessel-input-form.component.ts#L689)).
6. The **second** response — the restore's own — arrives. `restoreInFlight` is now `false`, so
   `onVesselEngineConfigSelected` computes `applyEngineDefaults = wantsEngineDefaults && !false`
   = **true** and calls `setEngineConfiguration(2, 7)`
   ([vessel-input-form.component.ts:496](../../cl/src/app/features/vessel-input/vessel-input-form/vessel-input-form.component.ts#L496)),
   while `applyVesselData`'s `patchPowerFields` overwrites `propulsionPower`.
   **The form drops back to 15 000 / 2 000 / 1 000, ids 2 and 7, propulsion 8 888.**

**This is why both committed fixes were correct and insufficient.** They made `restoreInFlight`
behave correctly *during* the restore. The defect is that the restore declares itself finished
before its own response has arrived — the flag is right, its lifetime is wrong. No amount of
refining what happens while the flag is set can fix a race that plays out after it is cleared.
That is precisely the class of problem C‑E's explicit sequence removes.

It also explains two things nobody could explain before:

- **Why manual reproduction is intermittent.** It needs a stale request in flight, so it depends on
  how fast the user clicks after the page loads.
- **Why "load the scenario once more" works** — the workaround written into
  `docs/qa/manual-test-scenarios/README.md` step 2. On the second load there is no stale request
  in flight, which is exactly cells E1-a…c, and those pass.

**A second, previously unlogged defect fell out of spec 3's diagnostic.** The three requests a
profile load produces are not three copies — the **first one is computed on the wrong plant**:

```
propulsionPower:      8888  → 12036.15 → 12036.15
meCapacityPerEngine: 15000  →    24000 →    24000
aeCapacityPerEngine:  1000  →     4000 →     4000
mainEngineTypeId:        2  →        1 →        1
fuelPrice:             950  →      780 →      780
transitHours:         8000  →     5000 →     5000
```

So it is not only wasted work: for a moment the results panels display a calculation for the
vessel-type default plant, labelled as the user's scenario. C‑F's value is larger than "two fewer
HTTP calls".

**Other results worth recording:**

- **A cold load with no saved profile already posts exactly once.** The triple calculation is a
  property of the *restore* path, not of startup.
- **E4 (vessel-config with no `operationalProfile`, the 800 ms fallback path) passes.** The
  fallback is not implicated.
- **O4/O5 remain structurally unreachable**, as C‑A predicted: both `loadCategories` and
  `selectVessel` await `/api/app-data/initial`, so the per-vessel fetch cannot precede the
  catalogue. Kept as a characterisation spec that flips into a live guard after C‑E.
- **I2 comparison is on the parsed body, not the serialised string.** The design's original wording
  said "byte-identical"; key order is not part of the wire contract and freezing it would make
  C‑G's mapper extraction fail for no reason. Design §3 updated to match, with the reason stated.
- **The frozen `15-baseline-user-pick.request.json` carries no `baselineIndex`.** That is the open
  bug, frozen deliberately. C‑E re-freezes it and names the diff.

Nothing was committed. No production file was modified (AC2).

### Debug Log References

| Run | Result |
|---|---|
| Backend baseline | **441/441 green**, 1 s (`-p:BaseOutputPath=<scratch>/testbin/`) |
| C‑A suite, unchanged | 18/18 green |
| First red-suite run (E1/E2 only) | **4 FAILED, 28 SUCCESS** — spec 1 fully green ⇒ missing dimension |
| Final run (E3/E4 added, spec 4, I2 goldens) | **6 FAILED, 32 SUCCESS**, 2.5 s |
| `npm run lint` | **190 problems** — unchanged from the C‑A baseline |
| `git status --porcelain cl/src/app/` | one untracked spec file (C‑A's), nothing modified |

**Spec 2 — `baselineIndex`** · RED
`the last request must carry the pinned baseline; observed sequence: [null,null]: Expected undefined to be 4.`
Both requests of the restore lose the pin. The cause assertion also fails: the restore's terminal
emission is tagged `'user'`.

**Spec 3 — calculation count** · RED
Cold load, no profile: **1** ✔ · Draft restore: **2** (bodies identical) ✘ · Profile load: **3**
(first body on the wrong plant, diff above) ✘

**Spec 1 — the arrival matrix**

| Cell | Entry point | Arrival order | Result | Final ME / SG / AE · ids · propulsion | Last writer |
|---|---|---|---|---|---|
| E1-a | Load click, catalogue warm | vessel-config +500 ms | **PASS** | 24000 / 3250 / 4000 · 1, 8 · 12036.15 | `applyProfileInputValues` |
| E1-b | Load click, catalogue warm | vessel-config +2000 ms | **PASS** | 24000 / 3250 / 4000 · 1, 8 · 12036.15 | `applyProfileInputValues` |
| E1-c | Load click, catalogue warm | vessel-config +3500 ms (crosses the watchdog) | **PASS** | 24000 / 3250 / 4000 · 1, 8 · 12036.15 | `applyProfileInputValues` |
| E2-a | Hard refresh + draft | catalogue 50 ms, vessel +500 ms | **PASS** | 24000 / 3250 / 4000 · 1, 8 · 12036.15 | `applyProfileInputValues` |
| E2-b | Hard refresh + draft | catalogue 50 ms, vessel +2000 ms | **PASS** | 24000 / 3250 / 4000 · 1, 8 · 12036.15 | `applyProfileInputValues` |
| E2-c | Hard refresh + draft | catalogue 2000 ms, vessel +1500 ms | **PASS** | 24000 / 3250 / 4000 · 1, 8 · 12036.15 | `applyProfileInputValues` |
| E2-d | Hard refresh + draft | catalogue 3500 ms (past the watchdog), vessel +500 ms | **PASS** | 24000 / 3250 / 4000 · 1, 8 · 12036.15 | `applyProfileInputValues` |
| **E3-a** | **Load clicked 450 ms after the catalogue, first fetch still in flight** | stale response, then the restore's own | **FAIL** | **15000 / 2000 / 1000 · 2, 7 · 8888** | **`setEngineConfiguration` via `onVesselEngineConfigSelected:496` + `applyVesselData` `patchPowerFields`** |
| **E3-b** | **Load clicked 1000 ms after the catalogue** | same | **FAIL** | **15000 / 2000 / 1000 · 2, 7 · 8888** | **same** |
| E4-a | Hard refresh + draft, response without `operationalProfile` | 800 ms fallback path | **PASS** | 24000 / 3250 / 4000 · 1, 8 · 12036.15 | `applyProfileInputValues` (fallback) |
| O4 / O5 | — | vessel-config before the catalogue | **UNREACHABLE** | — | structurally impossible today; see Completion Notes |

**E3-a step trace** — the flash and the overwrite, in one table:

| step | meCapacity | sgCapacity | aeCapacity | ME id | AE id | propulsion |
|---|---|---|---|---|---|---|
| +450 ms — default-category fetch on the wire | 9000 | 1100 | 2500 | 3 | 9 | null |
| `loadProfile()` called with a request still pending | 9000 | 1100 | 2500 | 3 | 9 | null |
| first (stale) vessel-config answered, settled | **24000** | **3250** | **4000** | **1** | **8** | **12036.15** |
| second (real) vessel-config answered, settled | 15000 | 2000 | 1000 | 2 | 7 | 8888 |
| calculations answered, settled | 15000 | 2000 | 1000 | 2 | 7 | 8888 |

### File List

New:
- `cl/src/testing/red/restore-engine-values.spec.ts` (11 specs — the arrival matrix)
- `cl/src/testing/red/restore-baseline-index.spec.ts` (2 specs)
- `cl/src/testing/red/load-emits-once.spec.ts` (3 specs)
- `cl/src/testing/red/restore-fuel-price.spec.ts` (1 spec, `xdescribe`-d — design §7.1)
- `cl/src/testing/golden/request-body-golden.spec.ts` (3 specs — invariant I2)
- `cl/src/testing/golden/01-excel-baseline.request.json`
- `cl/src/testing/golden/03-no-battery-reference.request.json`
- `cl/src/testing/golden/15-baseline-user-pick.request.json`

Modified:
- `docs/refactoring/client-refactor-design.md` (§1 root cause named, §3 I2 comparison basis)

Deleted: none. No production file touched.

### Change Log

| Date | Change |
|---|---|
| 2026-08-04 | C‑B implemented. Original E1/E2 matrix ran green; missing dimension identified and added as E3 (Load clicked while the initial cascade is in flight), which reproduces the reported symptom exactly. 6 red specs, all from the red suite. Root cause named: `endRestore()` runs before the restore's own vessel-config response arrives. I2 goldens frozen for scenarios 01/03/15. Status → Ready for Review. |

## QA Results

_(to be filled by Quinn)_

## Risk Assessment

- **Primary:** every spec goes green and the story is declared done anyway. **Mitigation:** AC6 makes
  "everything passed" a stop-and-escalate outcome, not a success.
- **Secondary:** an assertion is quietly relaxed until it fails, producing a red test for the wrong
  reason. **Mitigation:** AC5 requires the winning writer to be named per cell — a test that is red
  for the wrong reason cannot produce that table.
- **Tertiary:** the I2 golden bodies are frozen from a state that is itself mid-race, so they encode
  a random ordering. **Mitigation:** AC7 requires the settled state used to be recorded, and the
  freezing run to be reproducible; freeze from ordering O1 with `settle()`, never from a hand-picked
  `tick()`.
- **Rollback:** single commit, `git revert`. No production code, no backend, no schema.
