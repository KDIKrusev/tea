# Story: Diesel-Electric C — Client Form and Results at 0 ME

<!-- Source: PRD v1.0 §6 (story DE-C); architecture 04-architecture-diesel-electric.md §6 -->
<!-- Depends on: DE-A (validation), DE-B (calculation). -->

## Status: Done

## Story

As a **user entering a diesel-electric vessel in the form**,
I want **to set Main Engines = 0 and have the ME-only fields step aside**,
so that **I can describe the plant without fighting irrelevant required fields, and read honest
results**.

## Scope

1. `VALIDATION_LIMITS.COUNT` split: `ME_COUNT: { MIN: 0 }`, `AE_COUNT: { MIN: 1 }`
   (`defaults.constants.ts`); schema + template `min` attributes follow (`vessel-form.schema.ts:22`,
   `engine-config-section.component.html:96`).
2. At `meCount = 0`: ME type select, ME capacity, SG capacity and PTI controls disabled with
   cleared values (visible affordance). Re-enable restores the catalog prefill through the
   existing cascade; `FormEditTrackerService` baseline re-set on both transitions — **no new
   emission sources** (the finding-4/5/6 family lives here; architecture §6).
3. Backend DE errors (SG/PTI/AE-capacity) surface through the existing error UX.
4. Profile round-trip: export/import with `meCount: 0` (schema stays v3 — importer already
   accepts numeric 0).
5. DOM specs: form gating at 0; results render without NaN/blank at 0 ME; combination labels
   render AE-only rows correctly.

## Acceptance Criteria

1. **AC1:** meCount accepts 0; AE count still refuses 0 (split limits).
2. **AC2:** Setting meCount = 0 disables + clears ME type/capacity, SG, PTI; setting it back to
   ≥ 1 re-enables and re-prefills via the cascade; edit-tracker baselines follow both transitions.
3. **AC3:** A calculate round-trip at 0 ME renders Power Demands (ME row 0), baseline table
   (AE-only rows), tier cards — no NaN, no empty cells.
4. **AC4:** Profile with `meCount: 0` exports, imports and restores; request body carries 0.
5. **AC5:** `ng build` clean; full client suite green including the new DOM specs.

## Tasks / Subtasks

- [x] Task 1: Constants/schema/template split
- [x] Task 2: Field gating + tracker transitions
- [x] Task 3: DOM + round-trip specs
- [x] Task 4: `npm run test:ci` + `ng build`; record counts

## Dev Agent Record

- `VALIDATION_LIMITS.COUNT` split into `ME_COUNT { MIN: 0 }` / `AE_COUNT { MIN: 1 }`; schema and
  template follow (`min="0"` on the meCount input).
- Gating lives in `EngineConfigSectionComponent`: a meCount `valueChanges` subscription (a
  reaction to an existing emission source, never a new one — every setValue/disable runs with
  `emitEvent: false`) parks the four shaft-bound controls (`meCapacityPerEngine`,
  `mainEngineTypeId`, `sgCapacityPerEngine`, `batteryMaxPtiKw` — the last renders in the battery
  section but is a control of the shared form). Parked = disabled + cleared: disabled controls
  drop out of form validity (the `required` validators step aside for free, and validity gates
  every emission) while `getRawValue()` still carries explicit zeros to the wire.
- **Restore-path lesson (cost one red run):** `applyProfileInputValues` patches with
  `emitEvent: false`, so the subscription never fires on restore. Fixed with the codebase's own
  idiom — a public `refreshDieselElectricState()` called by the parent after the silent patch,
  exactly like `refreshDpAvailability`. The two wake paths differ deliberately: a USER bringing
  engines back gets the catalogue prefill; a restored profile is the authority and wakes the
  controls without prefill.
- Un-park re-prefill is transition-guarded: an ordinary meCount edit (2 → 3) never re-prefills —
  pinned by its own spec (the findings-4/5/6 bug family).
- Test results: 4 new behaviour specs (`diesel-electric-form.spec.ts`) — park + wire zeros,
  catalogue re-prefill, restore-parked, no-stomp. Full client suite **76/76 green** (1
  pre-existing skip), `ng build` clean, `ng lint` clean. Backend untouched.

## Post-gate finding — manual testing, 2026-08-14 (FIXED)

Loading scenario 36 in the running app produced **nothing**: empty results panel, and editing any
field triggered no calculation — with no error message anywhere.

**Cause:** `CalculatorPageComponent.onFormChange` carried a pre-check
(`propulsionPower > 0 && hotelLoad > 0 && meCapacityPerEngine > 0 && aeCapacityPerEngine > 0`)
whose `else` branch was an empty comment. A diesel-electric plant sends `meCapacityPerEngine: 0`
legitimately, so **every** calculation was dropped silently — no request, no results, no message.
The form was working perfectly; the page never asked.

**Why the specs missed it:** DE-C's specs mounted the FORM (`mountForm`) and asserted the
`formChanged` emission. The guard lives one level up, in the page. The wire — the only place the
two are distinguishable — was never watched for a diesel-electric input.

**Fix:** the guard now reads the plant shape (`meCount === 0 || meCapacityPerEngine > 0`),
extracted into `describesACalculablePlant` with the reason documented. Three page-level specs
added (`cl/src/testing/behaviour/diesel-electric-page.spec.ts`): the load posts, an edit posts
again, and a mechanical plant with no rating still stays silent. Verified red before the fix
(exactly the two diesel-electric specs failed; the third passed), green after. Client suite
**80/80**, build and lint clean.

**Lesson for the next form-level feature:** a spec that proves the form emits does not prove the
app calculates.

## QA Results

**Gate: PASS** — `docs/qa/gates/de.c-client-form-and-results.yml` (Quinn, 2026-08-13),
amended 2026-08-14 with the post-gate finding above.
AC3 (results panels at 0 ME) covered by the existing guards verified in the analyst brief plus
the DE-D manual pass to come; the form/wire half of the story is spec-pinned.
