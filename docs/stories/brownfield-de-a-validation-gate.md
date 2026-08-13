# Story: Diesel-Electric A — Validation Opens the 0-ME Door

<!-- Source: PRD v1.0 §6 (story DE-A); architecture 04-architecture-diesel-electric.md §4 -->
<!-- Context: first story of Epic E1. Backend validation only. The calculation pipeline still
     rejects every 0-ME combination until DE-B lands — a validated 0-ME input therefore returns
     the L1 no-valid-combination 400 in this intermediate state. That is expected and temporary;
     these tests exercise ValidationService directly. -->

## Status: Done

## Story

As a **user modelling a diesel-electric vessel**,
I want **`meCount = 0` to pass input validation with clear rules for what a 0-ME plant may not
have (shaft generators, PTI)**,
so that **the plant can be described at all, with actionable errors instead of a blanket "must be
at least 1"**.

## Scope (all in `Services/Validation/ValidationService.cs`)

1. `MeCount < 1` → `MeCount < 0`, message "Number of main engines cannot be negative".
2. `MeCapacityPerEngine > 0` and `MainEngineTypeId > 0` required **only when `MeCount ≥ 1`**.
3. New blocking errors at `MeCount == 0` (appended at the end of `ValidatePlantAndFinancials` —
   append-only, the pinned 400 order for existing inputs must not move):
   - SG: "Shaft generators require a main engine. Set shaft generator capacity to 0 for a
     diesel-electric plant."
   - PTI: "PTI requires a main engine shaft. Clear the PTI capacity for a diesel-electric plant."
4. `ValidateSystemCapacity`: at `MeCount == 0` the ME-utilization / hotel-load / shaft-capacity
   blocks are skipped; one diesel-electric check replaces them:
   `EffectivePropulsionPower + TransitHotelPowerKW > TotalAeCapacity` → Error-severity warning
   "Auxiliary engine capacity cannot carry propulsion and hotel load. Consider reducing propulsion
   power, decreasing sea margin, reducing hotel/mission load or increasing auxiliary engine
   capacity." Battery advisories and the DP-redundancy warning keep running.

## Acceptance Criteria

1. **AC1:** `meCount = 0`, AE plant sufficient, SG = 0, PTI = 0 → `Valid == true`, no errors.
2. **AC2:** `meCount = -1` → "Number of main engines cannot be negative".
3. **AC3:** `meCount = 0` + `sgCapacityPerEngine > 0` → the SG error; `meCount = 0` +
   `maxPtiPerEngineKw > 0` → the PTI error.
4. **AC4:** `meCount = 0` with `meCapacityPerEngine = 0` and `mainEngineTypeId = 0` produces **no**
   ME-capacity / ME-type errors; with `meCount = 1` both errors still fire (regression pin).
5. **AC5:** `meCount = 0`, propulsion 11 463 + hotel 3 800 vs AE 2×4 000 (8 000 kW) → the
   AE-capacity error (as Error-severity warning promoted to error); no ME-utilization or
   hotel-load message co-fires.
6. **AC6:** Full suite via `-p:BaseOutputPath=<temp>\` green; goldens 01–35 (incl. 35's pinned
   error list — its input has `meCount: 2`) byte-for-byte unchanged.

## Tasks / Subtasks

- [x] Task 1: ValidatePlantAndFinancials edits (items 1–3)
- [x] Task 2: Conditional `MainEngineTypeId` in ValidateOperationalModes (item 2)
- [x] Task 3: ValidateSystemCapacity diesel-electric branch (item 4)
- [x] Task 4: New tests in `KSailCalc.Tests/Validation/ValidationServiceTests.cs` (AC1–AC5)
- [x] Task 5: Full suite run; record counts

## Dev Agent Record

- Implemented exactly per architecture §4: append-only in the plant slice; the DE capacity branch
  short-circuits the three ME-shaped capacity checks and keeps battery/DP advisories live.
- 8 new validation tests (AC1–AC5 incl. the meCount=1 regression pins).
- Full suite: **449/449 green** (441 + 8), goldens untouched — see QA Results.

## QA Results

**Gate: PASS** — `docs/qa/gates/de.a-validation-gate.yml` (Quinn, 2026-08-13).
