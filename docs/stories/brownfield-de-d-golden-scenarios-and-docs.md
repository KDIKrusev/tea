# Story: Diesel-Electric D — Golden Scenarios and Documentation

<!-- Source: PRD v1.0 §6 (story DE-D); architecture 04-architecture-diesel-electric.md §7 -->
<!-- Depends on: DE-A..DE-C all Done/PASS. -->

## Status: Done

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

- [x] Task 1: Scenario JSONs 36–39
- [x] Task 2: Golden approval + tests
- [x] Task 3: Cards + README + COVERAGE-MATRIX + ORIENTATION note
- [x] Task 4: D-DE5 closure note
- [x] Task 5: Full suite; record counts

## Dev Agent Record

- Two host corrections before generation: `GoldenScenarioHost` now passes `calcOptions` into
  `Level1OptimizationService` (exact Program.cs mirror; behaviour identical — the key is absent
  from appsettings so the factor is 0), and `AssertFixtureCovers` skips the main-engine fixture
  check at `MeCount == 0` (the parked client sends `mainEngineTypeId: 0` and the main curve is
  provably never read).
- Scenarios 36–39 written to the client contract (three legacy fields, 2-space, no BOM), with
  `meCapacityPerEngine: 0` / `mainEngineTypeId: 0` mirroring what the parked DE-C form actually
  sends. Snapshots generated with `GOLDEN_UPDATE=1` on a `--filter GoldenMasterTests` run —
  `git status` showed exactly 4 additions and `git diff --stat` on `Expected/` was **empty**:
  the 35 frozen snapshots stayed byte-identical, which is the only reading of the
  GOLDEN_UPDATE ban this story is allowed.
- Snapshot review against the hand-derivations (all matched): 36 — sole survivor ae=4 @ 68.75 %,
  2.140875 t/h, **IL2 +41.675 t/yr (Level 2 live at 0 ME — the DE-B finding now pinned in a
  golden)**; 37 — DpReserve 800/500/500/300, SR 370 / PS 0, DP AE 4 870, Benefit 92.63 t/yr;
  38 — AE 13 382 @ 83.6375 %, PS 178 / SR 382, Benefit 177.47 t/yr, world gap exactly 178;
  39 — the DE-A 400 text verbatim.
- Docs: cards 36–39; README third-wave section; ORIENTATION Part 3c (what changes / what does
  not at 0 ME) + reverse-index L2 row (19 · 36) + trust table 19–39; COVERAGE-MATRIX
  diesel-electric block incl. the two deliberately-unit-test-only gaps; D-DE5 closed as a
  standing deferral in the decisions log.
- Full backend suite: **472/472 green** (460 + the new golden/contract rows). Client untouched
  this story (76/76 from DE-C stands).

## QA Results

**Gate: PASS** — `docs/qa/gates/de.d-golden-scenarios-and-docs.yml` (Quinn, 2026-08-13).
Epic E1 complete: DE-A/B/C/D all Done/PASS.
