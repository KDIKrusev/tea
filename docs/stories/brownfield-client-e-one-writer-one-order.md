# Story: Client E — One writer, one order

<!-- Source: docs/refactoring/client-refactor-design.md §1.1 (reproduced cause), §5.1, §6.5 -->
<!-- Context: Brownfield refactoring of cl/. Turns the C-B red suite green. -->

## Status: Ready for Review

## Story

As **Kamen, who has watched the same bug survive two committed fixes**,
I want **the restore to be an explicit sequence that ends when the work it started has finished**,
so that **no response, timer or arrival order can decide which value the user ends up looking at**.

## Context Source

- Design **§1.1** — the cause, reproduced by C‑B cell E3, not inferred.
- C‑B left six red specs and three frozen request bodies (invariant I2).
- The two already-committed fixes govern what happens *while* `restoreInFlight` is set. The race
  plays out *after* it is cleared. A boolean cannot express "not finished until the response I
  asked for has arrived"; a sequence with a declared set of awaited sources can.

## Scope

### 1. A response only counts for the selection it was asked for

`applyVesselData` currently applies whatever arrives. In cell E3 a response fetched for the
*previous* category completes the restore, `endRestore()` runs, and the restore's own response then
lands with `restoreInFlight` already false — and overwrites the profile with the vessel type's
defaults.

`FetchRequest` gains the selection it was issued for. A response whose selection no longer matches
the current one is **dropped before it touches the form**. `switchMap` already cancels an in-flight
request when a new trigger fires; this closes the window before the new trigger clears its 400 ms
debounce, which is exactly the window E3 lives in.

### 2. One event instead of two, so the order is not guessed

`vessel-config-section` today emits `vesselEngineConfigSelected` and then, conditionally,
`operationalProfileLoaded`. The form cannot know inside the first handler whether the second will
follow, which is the only reason the 800 ms fallback exists.

Both collapse into a single `vesselDataApplied` event carrying the operational profile as a
nullable field. One handler, one order, no fallback. A third output, `vesselDataFailed`, lets the
form end a sequence whose fetch errored — today that case is covered by the 1500 ms timer.

### 3. The load sequence

A **load sequence** is started by app startup and by a profile/draft restore. It is *not* started
by a size/speed edit — that path keeps today's behaviour exactly.

```
idle → loading(source, awaited selection) → settled → emit ONCE with that source
```

- While a sequence is active, `emitFormValues()` is **suppressed**, not tagged.
- A restore supersedes a startup sequence in flight.
- The sequence ends synchronously inside the `vesselDataApplied` handler, after the profile's
  values have been applied. No timer: `applyVesselData` calls the handler synchronously from the
  HTTP response, so "the cascade has settled" is a fact at that point, not a hope.

### 4. Emissions are deduplicated by value

`emitFormValues` skips when the built `CalculatorInput` is deep-equal to the last emitted one. This
is what absorbs the trailing 500 ms `valueChanges` debounce that the sequence's own
`emitEvent: true` patches queued — without having to flip every programmatic write to
`emitEvent: false`, which would silently unsubscribe half a dozen listeners across four components.

Sequence suppression removes the *stale* emissions; dedup removes the *redundant* ones. Together:
one load, one calculation.

### 5. Deleted

`restoreInFlight` · `RESTORE_WATCHDOG_MS` + watchdog · `pendingProfileInput` ·
`componentsLoaded` · `initialEmissionScheduled` · `scheduleInitialEmission` (1500 ms) ·
the 200 ms delay before `applyProfileInputValues` · the 800 ms fallback ·
`engine-config-section`'s zero-delay `setTimeout` · `AppDataService`'s hand-rolled
`loadingPromise` + `new Observable(observer => promise.then(...))` wrapper.

Surviving timing constants: **400 ms** (size/speed fetch debounce) and **500 ms** (form input
debounce). Both are deliberate UX, both keep a comment saying so.

### 6. Deliberately NOT in scope

- `loadEngineConfigurations`' catalogue-`[0]` pre-fill stays. It writes before any vessel data and
  is overwritten by it; under the sequence its write never escapes as an emission. Removing it
  would change what the dropdowns show during the first 400 ms. Logged for C‑I.
- `pendingEngineConfig` stays. It is unreachable in practice (the engine arrays are always
  populated before any vessel-config response can arrive) but proving that is C‑D's job, not a
  behaviour-changing story's.
- The §7.1 fuel-price overwrite stays exactly as it is. Deferred by Kamen's decision.

## Acceptance Criteria

1. **AC1 — Backend frozen (I1).** 441/441, `Golden/Expected/` clean, `GOLDEN_UPDATE` unset.
2. **AC2 — The red suite goes green.** All six C‑B failures pass, cells E3-a and E3-b included, and
   every cell that already passed (E1-a…c, E2-a…d, E4-a) still passes.
3. **AC3 — One load, one calculation.** Cold start: 1 POST. Draft restore: 1. Profile load: 1.
4. **AC4 — The pinned baseline survives.** Scenario 15 restores with `baselineIndex: 4` on the
   wire, and no emission during a restore is tagged `'user'`.
5. **AC5 — I2 holds, with one named exception.** `01-excel-baseline` and
   `03-no-battery-reference` are **unchanged**. `15-baseline-user-pick` is re-frozen to add
   `"baselineIndex": 4` — the deliberate fix — and nothing else in it changes. The diff is quoted
   in the Debug Log.
6. **AC6 — The timers are gone.** `grep` for `setTimeout` under `cl/src/app/` returns only what
   §5 permits. No `restoreInFlight`, `pendingProfileInput`, `componentsLoaded` or
   `initialEmissionScheduled` remains.
7. **AC7 — No new lint problems** beyond the 190 baseline, and `ng build --configuration production`
   green.
8. **AC8 — The C‑A suite still passes**, including the O4/O5 characterisation. If C‑E makes that
   ordering reachable, the spec is updated to assert the new, safe behaviour rather than deleted.

## Tasks / Subtasks

- [x] Task 0: Reproduce the baselines — backend 441/441, client 6 red / 32 green.
- [x] Task 1: `AppDataService` → `shareReplay`. Suite unchanged (6 red / 32 green), as expected.
- [x] Task 2: Selection-scoped fetch responses.
- [x] Task 3: Collapse the two outputs into `vesselDataApplied` + `vesselDataFailed`; delete the
      800 ms fallback and the 200 ms delay.
- [x] Task 4: The load sequence; delete `restoreInFlight`, the watchdog, `componentsLoaded`,
      `initialEmissionScheduled`, the 1500 ms timer.
- [x] Task 5: Emission dedup.
- [x] Task 6: No re-freeze needed — see AC5 note below. Added five behaviour specs for the
      user-edit paths instead, which nothing covered.
- [x] Task 7: Verify AC1, AC6, AC7 explicitly.

## Dev Notes

- **The order of Tasks 2 and 5 is the story's own safety net.** Task 2 should fix spec 1 alone;
  Task 5 should fix specs 2 and 3 alone. If a task fixes something it was not supposed to, the
  model of the bug is wrong — stop and re-read before continuing.
- `applyVesselData` runs synchronously from the HTTP response, and it calls the form's handler
  synchronously. Everything the old 200 ms delay was waiting for has therefore already happened.
  This is the claim the deletion rests on; if a spec disagrees, believe the spec.
- Emission dedup must compare the **built `CalculatorInput`**, not the raw form value. The raw
  value contains view-only controls (`vesselCategory`, `vesselSize`) that do not reach the wire.
- `calculator-page`'s `source === 'user'` check is left alone. With the tag finally correct it does
  the right thing; replacing it as well would make it impossible to tell which change fixed AC4.

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (claude-opus-5[1m])

### Completion Notes

**`npm run test:ci` → 43 specs, 43 green.** All six C‑B failures pass, every previously-passing cell
still passes, and five new behaviour specs cover the user-edit paths.

- **Task 2 alone did not fix cell E3** — contrary to the Dev Note's prediction. Selection matching
  drops the stale response, but on today's structure the restore was then completed by the *correct*
  response and the rest of the emission noise remained. The specs went green only once the load
  sequence (Task 4) and the dedup (Task 5) were in place. The Dev Note said "if a task fixes
  something it was not supposed to, the model is wrong" — this is the mirror case: a task fixed
  *less* than expected, and the reason is benign. Selection matching is still the right guard and is
  kept: it removes the class of defect, not just the instance.
- **AC5 needed no re-freeze, and the reason is a finding.** All three golden bodies are unchanged,
  `15-baseline-user-pick` included — it still carries no `baselineIndex`. The goldens are captured
  through the **auto-draft** path, and `startAutoDraft` builds its payload with
  `buildCalculatorInput()`, which has no `baselineIndex` (it is not a form control). Only the
  explicit Save path uses `getCurrentInputSnapshot(baselineIndex)`. **An auto-draft therefore never
  carried a pinned baseline in the first place** — pre-existing, untouched by this story, logged as
  design §7.10. The pin surviving a *profile* load is what AC4 asserts, and that now passes.
- **Two of the five new behaviour specs failed on first run, and both times the spec was wrong:**
  1. "an undone edit is deduped" — it is not, and must not be. Dedup compares against the **last**
     emission, not the whole history. After editing sea margin to 15 the panels show that result, so
     returning to 0 is new information. A history-wide cache would leave wrong numbers on screen.
     Spec rewritten to assert 2 calculations, with the reason.
  2. "a failed vessel fetch still calculates" — it does not, and did not before either. Without a
     vessel response `propulsionPower` is never filled and `CalculatorPageComponent.onFormChange`
     correctly declines to calculate a plant with no propulsion demand. The property worth asserting
     is that the form is not *frozen*, i.e. it still emits. Spec rewritten accordingly.
- **`onCategoryChange` and size/speed edits now recalculate 500 ms later than before**, via the
  debounce rather than from the response handler, and once instead of twice. Same numbers, one
  request. Locked by `user-edits.spec.ts`.
- **Bundle got smaller** (978.02 → 977.57 kB) and **lint went down by one** (190 → 189). Nothing was
  added to compensate for what was deleted.
- Nothing was committed.

### Debug Log References

| Run | Result |
|---|---|
| Baseline | backend 441/441 · client **6 FAILED, 32 SUCCESS** · lint 190 · `main-ZS6ZQ45V.js` 978.02 kB |
| After Task 1 (`shareReplay`) | client **6 FAILED, 32 SUCCESS** — unchanged, as designed |
| After Tasks 2–5 | client **38 SUCCESS, 0 FAILED** |
| After the five behaviour specs, first run | **2 FAILED** — both my own specs, both wrong (see above) |
| Final | client **43 SUCCESS** · backend **441/441** · `Golden/Expected/` clean · lint **189** · `main-46KT7PII.js` **977.57 kB** |

Explicit AC verification:

- **AC1** — `dotnet test -p:BaseOutputPath=<scratch>/testbin/` → 441/441.
  `git status --porcelain KSailCalc.Tests/Golden/Expected/` → empty. `GOLDEN_UPDATE` never set.
- **AC2/AC3/AC4** — the six C‑B specs pass; `restore-engine-values.spec.ts` reports all eleven cells
  green, E3-a and E3-b included.
- **AC5** — `request-body-golden.spec.ts` green against the bodies frozen in C‑B, all three
  unmodified. See the note above for why no re-freeze was needed.
- **AC6** — `grep -rn "setTimeout\|setInterval" cl/src/app --include=*.ts` returns only the two
  chart-rendering timers in `savings-chart.component.ts` (unrelated, logged for C‑I) and the 30 s
  auto-draft `setInterval`. `grep` for `restoreInFlight`, `pendingProfileInput`,
  `initialEmissionScheduled`, `RESTORE_WATCHDOG` → no hits. (`componentsLoaded` survives only as an
  unrelated, unread field on `operational-modes-section` — dead code for C‑D.)
- **AC7** — lint **189** (one below the 190 baseline); `ng build --configuration production` green,
  bundle 0.45 kB smaller. The budget warning is the same pre-existing one.
- **AC8** — the C‑A suite passes unchanged, including the O4/O5 characterisation. C‑E did **not**
  make that ordering reachable: the vessel fetch is still downstream of the catalogue, because
  `selectVessel` needs the category list. Removing that coupling is not required by any red spec and
  was left alone.

### File List

Modified (production):
- `cl/src/app/core/app-data.service.ts` — `loadingPromise` + hand-rolled Observable wrapper →
  `shareReplay({bufferSize:1, refCount:false})`, with a `catchError` that clears the cache so a
  failed load can be retried
- `cl/src/app/features/vessel-input/vessel-input-form/vessel-config-section/vessel-config-section.component.ts` —
  `Selection` identity + `sameSelection` guard; `vesselEngineConfigSelected` + `operationalProfileLoaded`
  → `vesselDataApplied` + `vesselDataFailed`; `VesselDataApplied` exported
- `cl/src/app/features/vessel-input/vessel-input-form/vessel-input-form.component.ts` —
  `LoadSequence`, `beginLoad`/`endLoad`, `emitFormValues(source, {force})`, `sameInput`,
  `onVesselDataApplied` (five numbered steps), `onVesselDataFailed`,
  `onOperationalProfileLoaded` → private `applyOperationalProfile`
- `cl/src/app/features/vessel-input/vessel-input-form/vessel-input-form.component.html` — new bindings
- `cl/src/app/features/vessel-input/vessel-input-form/engine-config-section/engine-config-section.component.ts` —
  zero-delay `setTimeout` around `setupValueChangeTracking` removed

New (tests):
- `cl/src/testing/behaviour/user-edits.spec.ts` (5 specs)
- `ApiFixture.failVesselConfig()`

### Change Log

| Date | Change |
|---|---|
| 2026-08-04 | C‑E implemented. The restore is an explicit sequence that ends when the response it asked for has been applied; responses are matched to the selection that requested them; emissions are suppressed during a load and deduplicated after it. Nine writers still write, but only one decides when. 43/43 client, 441/441 backend, I2 bodies unchanged, bundle and lint both down. Status → Ready for Review. |

## Risk Assessment

- **Primary:** suppressing emissions during a sequence also suppresses a legitimate one, and a user
  edit silently stops recalculating. **Mitigation:** a sequence is started only by startup and
  restore; size/speed edits keep today's path, and the C‑A/C‑B suites cover both.
- **Secondary:** dedup hides a real change because two different form states build the same input.
  That is by definition invisible on the wire, so it cannot change a number.
- **Tertiary:** a sequence never ends (fetch error, no response) and the app stops calculating
  forever. **Mitigation:** `vesselDataFailed` ends it explicitly — the case the 1500 ms timer used
  to cover.
- **Rollback:** single commit, `git revert`. No backend, no schema, no wire-contract change.
