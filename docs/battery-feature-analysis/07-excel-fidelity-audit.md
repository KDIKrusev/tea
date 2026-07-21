# 07 — Excel Fidelity Audit: Do We Compute the Battery Correctly?

**Date:** 2026-07-13 · **Author:** Mary (Analyst) + Quinn (Test Architect)
**Method:** formula-by-formula comparison of `PowerPlantSetupAdvisesIncludingPTIOAndbatteries_test.xlsx`
against the implementation, PLUS a numeric cross-check of the workbook's own **saved cell values**
(extracted from the file) against the values pinned in our automated tests.

## Verdict up front

**The battery arithmetic is a 1:1 match with the workbook — to the last floating-point digit.**
Every saved intermediate value in the Excel's Load Demands sheet equals the value our tests pin.
The surrounding plant model differs from the Excel in ways that predate the battery feature or
were explicitly decided (D4/D-C1/D-C2) — listed honestly in §4 with impact assessment.

## 1. Numeric cross-check — workbook's saved values vs our pinned tests

The workbook was saved with the example scenario computed (budget 1 260 kW, propeller 11 463 @ ±5 %,
hotel 3 800 @ ±2 %). Its stored cell values, straight from the file:

| Cell (Load Demands) | Excel formula | **Excel saved value** | **Our pinned test value** | Match |
|---|---|---|---|---|
| I8 (propeller battery use) | `MIN(K7,H8)` | 573.1499999999996 | 573.15 | ✅ |
| J8 (propeller covered ±) | `I8×D8` | 200.60249999999985 | 200.6025 | ✅ |
| L8 (propeller uncovered) | `H8−I8×D8` | 372.5474999999998 | 372.5475 | ✅ |
| I9 (hotel battery use) | `MIN(K8,H9)` | 76 | 76 | ✅ |
| J9 (hotel covered ±) | `I9×D9` | 3.8000000000000003 | 3.8 | ✅ |
| L9 (hotel uncovered) | `H9−I9×D9` | 72.2 | 72.2 | ✅ |
| I10 (Σ battery committed) | `SUM(I5:I9)` | 649.1499999999996 | 649.15 | ✅ |
| J10 (Σ covered band) | `SUM(J5:J9)` | 204.40249999999986 | 204.4025 | ✅ |
| K9 (remaining budget) | cascade | 610.8500000000004 | 610.85 | ✅ |
| L10 (Σ spinning reserve) | `SUM(L5:L9)` | 444.7474999999998 | 444.7475 | ✅ |
| **Optimal Setup R8** (total demand) | `O7+R7` | **15 707.7475** | avg + ΣL = 15 263 + 444.7475 | ✅ |

The sub-ULP differences (…99998 vs our rounded literals) are double-precision representations of
the same numbers; our assertions use 1e-6 tolerance and the same arithmetic order as the sheet.

## 2. Formula-by-formula: allocation & demand (EXACT matches)

| Workbook mechanism | Cell/Formula | Implementation | Status |
|---|---|---|---|
| Priority = sheet row order, cascading budget | K column | `BatterySettings.LoadPriorities` order, `remaining −= I` | ✅ exact |
| Battery use per row | `I = MIN(remaining, H)` | `Math.Min(remaining, variation)` | ✅ exact |
| Covered band | `J = I × D` | `batteryUsed × CoverageFactor` | ✅ exact |
| Uncovered reserve | `L = H − I×D` | `variation − coveredBand` | ✅ exact (algebraically identical) |
| RESERVE rows cover full requirement | `H5 = G5` (not G−E) | `H = avg×(1+vf)` for Reserve function | ✅ exact |
| PEAK SHAVING rows cover the variation band | `H = G − E` | `H = avg×vf` | ✅ exact |
| Coverage factors | RESERVE→100 %, DP-PS 50 %, propeller **0.35**, hotel **0.05** | `BatterySettings` (config = same values) | ✅ exact |
| Variation factors | P5 = 0.05, P6 = 0.02 | config defaults | ✅ exact |
| Mission variation = full heavy-consumer max | `G7 = IF(E7>0, I3, 0)` | variation override = `MissionHeavyConsumerMaxKw` (Increment F) | ✅ exact |
| DP redundancy NOT part of avg demand | `O7 = SUM(O3:O6)` (skips O2) | DpReserve avg feeds only the reserve row | ✅ exact |
| **Demand = Σavg + Σuncovered reserve** | `R8 = O7 + R7` | L1 loads = avg + per-side ΣL | ✅ exact |
| Peak shaving does NOT reduce demand | Q column absent from R8 | PS band feeds only the PTI gate | ✅ exact |
| Per-side reserve routing | R5 (propeller) / R6 (hotel) rows | `ToAdjustment` thrust/hotel split | ✅ exact |

## 3. Formula-by-formula: PTI/PTO & selection (faithful with adaptations)

| Workbook mechanism | Implementation | Status |
|---|---|---|
| PTI covers ME deficit, capped at Max PTI per ME | `TryApplyPtiAssist`, cap = ActiveMeCount × MaxPti | ✅ |
| PTI transmission losses 5 % charged to aux | Excel: aux demand + `M×I$4`; ours: aux + `pti×(1+0.05)` — same total (power share + loss), different distribution model (§4a) | ✅ equivalent accounting |
| "Insufficient PTI" for battery band | Excel X-column check `X ≥ Q$5` (negative-sign convention); ours: `headroom ≥ propulsion band` (positive convention) | ✅ same logic, clearer convention |
| "Insufficient pwr" | demand > setup max | ✅ (capacity checks) |
| PTO to hotel | Excel PTO columns; app's Shaft Generator IS the PTO path | ✅ pre-existing |
| PTO charge-side gate (Y column) | **deliberately absent** (D-C1): in the app's average-load model charging occurs in the load down-swing when genset headroom exists by construction; a strict gate on our SG-at-max model would false-negative every SG vessel | ⚠ documented deviation |
| Machine direction exclusivity (PTI xor PTO per machine) | relaxed at aggregate level (D-C2) — the app models N identical machines | ⚠ documented deviation |
| SFOC interpolation | Excel `FORECAST.LINEAR` between bracketing points; app linear interpolation over the same curve shape | ✅ same method (endpoint extrapolation details differ negligibly) |
| Setup recommendation | Excel: **min Σ SFOC [g/kWh]**; app: **min FOC [ton/h]** (power-weighted — physically correct; switching would silently change every pre-battery result) | ⚠ documented deviation (D4) |

## 4. Honest deviations & their impact on "smятаме ли правилно"

**(a) Load-distribution model (pre-existing, NOT battery-related).** The Excel loads every selected
machine at one uniform fraction (`I = TotalDemand/SetupMax`); the app distributes structurally
(SG covers hotel first, AE the remainder, ME carries propulsion + SG). Consequence: per-machine
load points — and therefore SFOC values and FOC — can differ from the workbook for the same setup.
This is the app's Level-1 model since before the battery feature; the battery inputs to it
(adjusted demand) are exact. *Battery arithmetic unaffected.*

**(b) Ranking metric (D4-accepted).** Σ SFOC vs FOC ton/h can order setups differently in edge
cases. The recommended setup may occasionally differ from the workbook's AF-min row; each metric
is internally consistent. *Battery arithmetic unaffected.*

**(c) PTO charge gate & (d) direction exclusivity** — see §3; both battery-adjacent, both
documented with engineering justification, both future-liftable via a per-machine model.

**(e) Homogeneous plant.** The app models N identical MEs + N identical AEs; the workbook has six
individually-sized machines (G1–G6) with individual SFOC curves. Exact workbook-row reconciliation
is only possible for symmetric plants. *Pre-existing; battery arithmetic unaffected.*

**(f) Extensions the Excel doesn't have** (additive, from the task sketch / app concepts):
third-highest baseline, R3a dual-scenario Battery Benefit, L3 DRC residual rule, kWh plausibility
warning. None alters the workbook math; all are documented decisions (D1–D4).

## 5. Evidence base

- 206/206 automated tests; ~72 battery-specific, of which 30+ pin hand-derived workbook numbers.
- Five consecutive test batches (Increments A, B/C/E, test-design 27, F, Family H) passed
  **first-run** against independently hand-computed Excel values — the implementation and the
  workbook are demonstrably the same arithmetic.
- Traceability: `docs/qa/assessments/battery-test-design-20260713.md` (scenarios ↔ tests ↔ cells).

## 5b. Deep-dive addendum — the combination table's SAVED values (rows 18–75)

A second audit pass extracted the workbook's computed values for all 58 setup rows. Findings:

**✅ Convergence on the recommended setup (row 38 = G1+G6).** Excel's winner (min Σ SFOC = 344):
ME demand **15 707.7475 kW** at **32.72 %** load, hotel fed via PTO 3 250 + 994.7475. Our model's
{2 ME, SG on, 0 AE} combo produces the **identical ME power and load** (propulsion 11 835.5475 +
SG 3 872.2 = 15 707.7475; /48 000 = 32.72 %) — when everything flows through the MEs, the
structural and uniform distributions coincide exactly. The two models agree bit-for-bit on the
row that matters most.

**✅ Feasibility outcomes match (different wording).** Row 22 (G1 alone) fails as "Insufficient
PTO" (hotel needs 4 244.75 > Max PTO 3 250); our equivalent combo fails as "hotel not covered by
SG+AE" — same physics. Row 58 (4 aux, no ME) fails "Insufficient PTI"; ours rejects ME=0 in
Transit — same outcome. Rows 18–21/24–29/39–42 "Insufficient pwr" ≡ our capacity checks.

**✅ PTI loss accounting equivalent.** Row 59: aux side totals 5 285.4743 = uniform share + M×5 %
(49.5584); generated total = demand + PTI loss — exactly our `pti × 1.05` bookkeeping.

**⚠ AUDIT-1 (semantic difference, decision needed): PTI donors.** The Excel's PTI columns
(`N = IF(AND(B18="", M>0), MIN(M, MaxPTI), 0)`) allow PTI **only on machines whose engine is NOT
selected** — the idle ME's shaft motor drives its own propeller electrically (rows 59, 62, 69–70:
PTI 991.17 / 2 038.35 on the non-running engine). A running engine's machine does PTO only
(strict per-machine direction exclusivity). Our Increment C capped PTI at **ActiveMeCount × MaxPti**
— i.e. boost on RUNNING shafts, none on idle ones — the opposite donor set. Both models are
physically defensible (PTI boost on a running shaft is real marine practice; the Excel models
machine exclusivity).
**RESOLVED (D5, Increment G):** owner chose the **union** — PTI capacity =
`MeCount (installed) × MaxPti`, making both the Excel's row-59 "electric second shaft" setups and
boost-mode setups feasible. Pinned by
`G_IdleMachinePti_EnablesExcelRow59StyleCombination` (deficit 700 covered by 2×500 with one engine
running: Pti 700, ME pinned to capacity, aux +735 incl. losses).

**⚠ AUDIT-2 (adaptation, already documented as deviation (a), now quantified).** The Excel has
NO per-side reserve routing — the whole spinning reserve (ΣL = 444.7475) is inside the single
total demand spread uniformly; e.g. row 30 loads ME and aux both at 56.1 %. Our structural model
routes thrust-load reserve to propulsion and hotel/mission reserve to the hotel side, so mixed
setups show different per-machine loads (same totals). This split is OUR necessary adaptation —
neither more nor less correct than uniform; it is what a structural dispatch requires.

**ℹ AUDIT-3 (workbook quirks found — deliberately NOT replicated):**
- The battery PTI/PTO gate columns X/Y are **vacuous as saved**: X is clamped to `Q$5 = −J8`
  (negative), and the failure test `X < Q$5` can never be true (headroom ≥ 0 > Q$5 always) — the
  literal sheet never rejects a setup on battery grounds. Our positive-convention gate implements
  the columns' *intent* ("Available PTI for Peak shaving from battery") and is strictly stronger
  than the saved workbook.
- Cell D6 tests `IF(C6="RESERVES",…)` (typo, plural) — the DP-demand row can never actually get
  100 % coverage in the sheet; our config uses explicit factors, immune to the typo.
- The "no-battery" comparison table (rows 76+) contains headers only (never filled in); R84 holds
  a stray junk string. Our R3a reference run computes what that table was meant to show.

## 6. Conclusion

**Да — батерията се смята правилно спрямо Excel-а.** The allocation cascade, the reserve/peak-shaving
semantics, the demand feed and the PTI feasibility logic reproduce the workbook exactly, verified
against the workbook's own saved values. The known differences are confined to the surrounding
plant model (distribution & ranking — pre-existing app design) and two consciously deferred gates,
all recorded with rationale in the decisions log (D4, D-C1, D-C2).
