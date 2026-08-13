# Story: Diesel-Electric D — Golden Scenarios and Documentation

<!-- Source: PRD v1.0 §6 (story DE-D); architecture 04-architecture-diesel-electric.md §7 -->
<!-- Depends on: DE-A..DE-C all Done/PASS. -->

## Status: Approved (ready after DE-C)

## Story

As **the team and the client verifying diesel-electric behaviour**,
I want **frozen golden scenarios and per-scenario calculation cards for the 0-ME plant family**,
so that **future changes are detected and the client can verify the new numbers against his own
references**.

## Scope

1. New import-ready scenarios (36+), written to the client contract (2-space JSON, no BOM, the
   three legacy fields `hotelLoad`/`batteryCapacity`/`sailInstalled` present — CR3):
   - **36 — diesel-electric transit**: AE-only plant, no battery (DE-B AC2 shape).
   - **37 — diesel-electric DP** (doc mode 6 analogue): DP + redundancy, battery DP-only —
     DpReserve lands on the AE side.
   - **38 — diesel-electric with battery in Transit** (DE-B AC4/AC5 shape, Benefit non-zero).
   - **39 — infeasible AE plant**: expect the new 400 text (DE-A AC5), NOT results.
2. Golden snapshots approved for 36–39; goldens 01–35 remain byte-for-byte untouched.
3. Calculation cards in `docs/qa/manual-test-scenarios/calculations/` (one per scenario, existing
   pattern), marked **"characterisation — pending reference verification"** until the client
   confirms at least one against his workbook (then promoted to proof, like 01–18).
4. `docs/qa/manual-test-scenarios/README.md` + `COVERAGE-MATRIX.md` updated; `00-ORIENTATION.md`
   gains a short "0 ME" note (distribution rule, L2/L3 limitations).
5. D-DE5 closure: decide/implement the cosmetic "diesel-electric" hint in Power Demands, or record
   the explicit deferral.

## Acceptance Criteria

1. **AC1:** All four scenario files import in the UI without edits (client contract respected).
2. **AC2:** Golden tests cover 36–39 and pass; scenario 39 pins the 400 + its error text.
3. **AC3:** `ScenarioImportContractTests` still green (contract untouched — schema stays v3).
4. **AC4:** Cards + README + ORIENTATION updated; each new number traceable to a card formula.
5. **AC5:** Goldens 01–35 byte-identical (diff of `Golden/Expected/` shows only additions).

## Tasks / Subtasks

- [ ] Task 1: Scenario JSONs 36–39
- [ ] Task 2: Golden approval + tests
- [ ] Task 3: Cards + README + COVERAGE-MATRIX + ORIENTATION note
- [ ] Task 4: D-DE5 closure note
- [ ] Task 5: Full suite; record counts

## Dev Agent Record

_(pending)_

## QA Results

_(pending)_
