# Story: Battery Increment G — PTI Donor Set = Installed Machines (AUDIT-1 fix)

<!-- Source: 07-excel-fidelity-audit.md §5b AUDIT-1; owner decision 2026-07-13: option 3 (union) -->

## Status: Done

## Story

As a **user modelling hybrid plants**, I want **PTI capacity to come from ALL installed shaft
machines (union of Excel's idle-donor model and real-world running-shaft boost)**, so that
**Excel's "electric second shaft" setups (row 59: idle G6's machine drives its propeller with
991 kW) are feasible in the app, alongside boost-mode setups**.

## Scope & AC

1. `TryApplyPtiAssist`: PTI capacity basis `ActiveMeCount → MeCount` (installed).
2. **AC-G1 (Excel row-59 analog):** plant ME 2×5000, SG 500/e, AE 3×1000, propulsion 5200, SM 0,
   hotel 2000, MaxPti 500: the {1 ME, SG, 3 AE} combo has deficit **700** — was invalid (active
   cap 500), now valid via both machines' PTI (cap 1000): Pti=**700**, AvailablePti=**300**,
   ME pinned to 5000 (100 %), AE = 1500 + 735 = **2235** (74.5 %).
3. **AC-G2:** zero regression — full suite green (verified: all C/H tests use 2-active combos
   where installed = active, or deficits beyond even the widened cap).

## Dev Agent Record

- One-line capacity-basis change + doc comment citing Excel N-column semantics; new test
  `Level1PtiTests.G_IdleMachinePti_EnablesExcelRow59StyleCombination`.
- Decision recorded as D5 in the decisions log; AUDIT-1 in the fidelity audit marked resolved.

| Date | Change |
|---|---|
| 2026-07-13 | PTI donor set = installed machines (union model); row-59 analog test. |

## QA Results

### Review Date: 2026-07-13 · Reviewed By: Quinn (Test Architect)

Monotonic feasibility widening (previously valid combos stay valid) with the row-59 analog pinned
first-run; predicted zero test churn confirmed across the 207-test suite. Verified as part of the
**final full-initiative QA sweep**, including a live runtime smoke test (own API instance on
:5999): Excel scenario returned SR 444.7475 / PS 204.4025, 5 combos, baseline index 2, string
enums on the wire, L3 shaved 3.8/496.2, benefit 173.66 t/yr; no-battery request returned
batteryDetails=null with baseline index 4; invalid battery → HTTP 400 with the exact validation
message; tiny-PTI → HTTP 500 (QA-C-1, known).

Gate: **PASS** → docs/qa/gates/battery.g-pti-donors.yml · Status: Done.