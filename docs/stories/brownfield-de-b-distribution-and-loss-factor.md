# Story: Diesel-Electric B — Distribution Branch and the Electric Loss Factor

<!-- Source: PRD v1.0 §6 (story DE-B); architecture 04-architecture-diesel-electric.md §1–3, §7 -->
<!-- Context: the heart of Epic E1. After this story a 0-ME input calculates end-to-end. -->

## Status: Done

## Story

As a **user modelling a diesel-electric vessel**,
I want **the whole demand (propulsion + hotel, including battery-uncovered reserve) distributed
over the auxiliary engines**,
so that **Level 1 optimisation, baselines and the Battery Benefit work for a 0-ME plant exactly
as they do for a conventional one**.

## Scope

1. `Services/Helpers/PlantShape.cs` (new): `IsDieselElectric(input) => input.MeCount == 0`.
   A static helper — **NOT** a property on `CalculatorInput` (JSON-drift risk, architecture §1).
2. `Level1CandidateBuilder.TryDistribute`: early diesel-electric branch per architecture §2 —
   only `ActiveMeCount == 0 && !SgEnabled` states survive;
   `aePower = hotel + propulsion × (1 + factor)`; `MePowerKw = SgPowerKw = 0`.
3. `CalculatorSettings.ElectricPropulsionLossFactor` (default **0**, D-DE2), bound from
   `Calculator:ElectricPropulsionLossFactor`; threaded
   `Level1OptimizationService` → `TryDistribute` as a parameter (builder stays pure).
4. `Level1RejectionTally.ExplainFor`: diesel-electric sentence for the no-survivor case (names AE
   count/capacity and the 90 % ceiling).
5. Characterization test: Level 2 at zero SG returns an empty redistribution (logged limitation,
   architecture §8.3).

## Hand-derived Acceptance Criteria (AE 4×4 000, SM 0, no battery unless stated)

1. **AC1 (survivor space):** propulsion 8 000, hotel 3 000 → demand 11 000. ae=1 (4 000) and
   ae=2 (8 000) insufficient; ae=3 → 11 000/12 000 = **91.7 % > 90 %** rejected (AuxOverloaded);
   ae=4 → **68.75 %** sole survivor ⇒ baseline = optimal, L1 savings 0. No combination carries
   `MePowerKw > 0` or `SgEnabled`.
2. **AC2 (two survivors, ranking):** propulsion 5 000, hotel 2 600 → demand 7 600. ae=2 rejected
   (95 %); ae=3 (63.3 %) and ae=4 (47.5 %) survive; optimal is the lower-FOC of the two; with no
   battery, baseline = `count − 1` (the worse row).
3. **AC3 (loss factor):** same as AC2 with factor 0.05 (via options in the unit test) →
   demand = 2 600 + 5 000 × 1.05 = **7 850**; AE loads recompute accordingly. Config default test:
   unset key ⇒ factor **0** (pattern of `BatterySettingsConfigurationTests`).
4. **AC4 (battery, one world):** propulsion 10 000, hotel 3 000, battery 800 kW Transit.
   Cascade (unchanged code): Propulsion H = 500, I = 500, J = 175, L = 325; Hotel H = 60, I = 60,
   J = 3, L = 57. Demand on AE = (10 000 + 325) + (3 000 + 57) = **13 382**. Tiles SR = 382 /
   PS = 178.
5. **AC5 (battery, two worlds):** same input — Benefit = FOC(budget 0 world) − FOC(budget 800
   world) > 0, computed through the unchanged `ModePipelineRunner` path; world-B demand =
   world-A + 178 (the covered J, per side: propulsion +175, hotel +3).
6. **AC6 (DP at 0 ME):** DP mode with RequiredDPPowerKW 1 000, DP hotel 3 500 → both land on AE
   (demand 4 500 at factor 0); DpReserve L (uncovered) raises the AE demand — no PTI gate fires
   (`MaxPti = 0` by DE-A validation).
7. **AC7 (L2 characterization):** Transit L1 optimum at 0 ME → `Level2Result` empty/zero benefit,
   no exception.
8. **AC8 (regression):** full suite green via `-p:BaseOutputPath=<temp>\`; goldens 01–35
   byte-for-byte unchanged; every `MeCount ≥ 1` code path provably untouched (the branch is the
   only edit inside `TryDistribute`).

## Tasks / Subtasks

- [x] Task 1: PlantShape helper + settings key + options threading
- [x] Task 2: TryDistribute branch (architecture §2 verbatim)
- [x] Task 3: RejectionTally diesel-electric wording
- [x] Task 4: Tests AC1–AC7 (`Level1CandidateBuilder`/`Level1OptimizationService`/battery/L2)
- [x] Task 5: Full suite + golden byte check; record counts

## Dev Agent Record

- `Services/Helpers/PlantShape.cs` (new), `CalculatorSettings.ElectricPropulsionLossFactor`
  (default 0; note: the real appsettings section is `CalculatorSettings`, not `Calculator` as the
  architecture doc first wrote — key is `CalculatorSettings:ElectricPropulsionLossFactor`),
  DE branch at the top of `TryDistribute` with the factor passed as an optional parameter
  (builder stays pure; existing call sites compile unchanged), optional
  `IOptions<CalculatorSettings>` on the `Level1OptimizationService` constructor (DI supplies it;
  the many direct test constructions keep compiling), DE-aware AuxOverloaded sentence in
  `Level1RejectionTally` (existing pinned texts untouched), `TestServiceFactory` wires the
  settings through.
- **AC7 finding — architecture assumption corrected:** L2 at 0 ME is NOT empty. It sweeps
  unequal splits across the active AEs and beat Level 1's equal split by ~0.0009 t/h on the test
  plant. Recorded in the design doc §8.3 and the decisions log; DE-D must present L2 as live for
  diesel-electric. The characterization test now pins the real figure.
- Test results: 11 new tests (AC1–AC7 + rejection wording), full suite **460/460 green** via the
  BaseOutputPath redirect; `git status` shows no `Golden/Expected/` entries — CR1 held.

## QA Results

**Gate: PASS** — `docs/qa/gates/de.b-distribution-and-loss-factor.yml` (Quinn, 2026-08-13).
