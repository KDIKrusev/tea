# Story: Diesel-Electric C — Client Form and Results at 0 ME

<!-- Source: PRD v1.0 §6 (story DE-C); architecture 04-architecture-diesel-electric.md §6 -->
<!-- Depends on: DE-A (validation), DE-B (calculation). -->

## Status: Approved (ready for Dev after DE-B)

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

- [ ] Task 1: Constants/schema/template split
- [ ] Task 2: Field gating + tracker transitions
- [ ] Task 3: DOM + round-trip specs
- [ ] Task 4: `npm run test:ci` + `ng build`; record counts

## Dev Agent Record

_(pending)_

## QA Results

_(pending)_
