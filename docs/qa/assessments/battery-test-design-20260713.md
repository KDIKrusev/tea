# Test Design — Battery Calculations vs. the Excel Reference Models

**Author:** Quinn (Test Architect) · **Date:** 2026-07-13 · **Status:** Ready for test implementation
**Sources:** `docs/PowerPlantSetupAdvisesIncludingPTIOAndbatteries_test.xlsx` (primary — all battery math),
`docs/MachCalcTool-20200821-v2.43 linear 1 (1).xlsm` (constants), `docs/battery-feature-analysis/02-excel-model-analysis.md`.

Every scenario below carries **hand-computed expected values from the Excel formulas** so it can be
pinned as a backend test to 1e-6. The traceability column says whether an automated test already
exists (from Increments A–E) or is **NEW** (to be written next).

---

## 0. The formulas under test (from the workbook)

**Allocation cascade** (*Load Demands* sheet, rows 5→9, strict priority order):

| # | Load | Function | Coverage D | Variation factor |
|---|---|---|---|---|
| 1 | DpReserve | RESERVE | 1.00 | 0.00 |
| 2 | DpDemand | PEAK SHAVING | 0.50 | 0.00 |
| 3 | Mission | PEAK SHAVING | 0.50 | 0.00 |
| 4 | Propulsion | PEAK SHAVING | 0.35 | **0.05** |
| 5 | Hotel | PEAK SHAVING | 0.05 | **0.02** |

Per row: `H = avg×vf` (PS) or `avg×(1+vf)` (RESERVE); `I = min(remaining, H)`; `J = I×D`;
`L = H − J`*; `remaining −= I`.  *(strictly `L = H − I×D`; identical when written via J.)*

**Plant model** (*Optimal Setup*): total demand = Σavg + ΣL (peak shaving does **not** reduce
demand — it only feeds the PTI/PTO feasibility gates); PTI covers ME deficit ≤ Max PTI, aux side
carries `pti × 1.05`; combo invalid when the battery's propulsion-side band exceeds PTI headroom.

**Key derived invariants worth testing explicitly:**
- **INV-1:** `I ≤ H` and `Σ(J + L) = ΣH` always (covered + uncovered = total variation).
- **INV-2:** for budget ≥ ΣH, `L = H×(1−D)` per row — **peak shaving never eliminates reserve**
  (coverage < 100%), so ΣL is *independent of budget size* beyond saturation.
- **INV-3:** the battery scenario's L1 demand ≤ the reference scenario's (avg+ΣL ≤ avg+ΣH) ⇒
  `BenefitFocTonPerYear ≥ 0` structurally.

---

## Family A — Allocation engine (pure unit level, `BatteryAllocationService`)

Base loads unless stated: Transit, Propulsion avg = 11 463 (sea margin 0), Hotel avg = 3 800.
ΣH = 573.15 + 76 = **649.15**.

| ID | Scenario | Budget | Expected (H / I / J / L per row; totals) | Trace |
|---|---|---|---|---|
| A1 | Excel saved state | 1260 | Prop: 573.15 / 573.15 / **200.6025** / **372.5475** · Hotel: 76 / 76 / **3.8** / **72.2** · ΣI 649.15, ΣJ **204.4025**, ΣL **444.7475**, rem 610.85 | ✅ covered (`BatteryAllocationServiceTests` AC1) |
| A2 | Budget exhausts mid-row | 300 | Prop: I=300, J=**105**, L=**468.15**, rem 0 · Hotel: I=0, J=0, L=**76** · ΣJ 105, ΣL **544.15** | **NEW** |
| A3 | Budget exactly = first row's H | 573.15 | Prop fully covered (J 200.6025, L 372.5475), rem 0 · Hotel: I=0, L=76 · ΣL **448.5475** | **NEW** |
| A4 | Zero budget | 0 | ΣJ=0, ΣL=ΣH=**649.15** | ✅ covered (AC3) |
| A5 | Saturated budget (INV-2) | 10 000 | ΣI=649.15, rem **9350.85**; ΣJ/ΣL **identical to A1** (204.4025 / 444.7475) | **NEW** — pins the "more battery ≠ less reserve beyond saturation" property |
| A6 | Port mode, small hotel (live user case) | 70 | Hotel only: avg 155 ⇒ H=**3.1**, I=3.1, J=**0.155**, L=**2.945** | **NEW** (validates the screenshot) |
| A7 | DP mode | 500 | Rows DpReserve(0)/DpDemand(H=0, vf=0)/Mission(0)/Hotel(dp hotel 1500): H=**30**, I=30, J=**1.5**, L=**28.5** | partially (mapping test) — **NEW** for numbers |
| A8 | Custom coverage (config) | 1260 | Prop D=1.0 ⇒ J=**573.15**, L=**0** | ✅ covered (AC4) |
| A9 | RESERVE semantics | 10 000 | DpDemand retagged RESERVE, D=1, vf=0.10, thrust 4000 ⇒ H=**4400**=I=J, L=**0** | ✅ covered |
| A10 | Negative budget clamp | −50 | = A4 | ✅ covered |
| A11 | Sail-adjusted propulsion override | 1260, override 10 000 | Prop avg=10 000 ⇒ H=**500**, I=500, J=**175**, L=**325**; Hotel unchanged | partially — **NEW** for full numbers |
| A12 | Custom priority order (config) | 100, Hotel first | Hotel: I=**76** · Prop: I=**24**, J=8.4, L=564.75 | ✅ covered (order) — **NEW** for J/L values |
| A13 | Invariant Σ(J+L)=ΣH (INV-1) | any (e.g. 300) | 105+544.15 = 649.15 ✓ property-style assert across A1–A12 | **NEW** |

## Family B — Pipeline: demand adjustment, baseline rule, dual-scenario benefit

**Reference plant "EP":** ME 2×24 000, SG 1 000/engine, AE 3×800, propulsion 11 463, SM 0,
Transit 5 000 h, hotel 3 800. **Rich plant "RP":** EP with AE 3×2 000.

| ID | Scenario | Expected | Trace |
|---|---|---|---|
| B1 | EP + battery 1260/2000 Transit — **full e2e pin** | Adjusted loads: propulsion **11 835.5475**, hotel **3 872.2** ⇒ single valid combo {2 ME, SG on, 3 AE}: MePower **13 835.5475** (load 28.824 %), SgPower 2 000, AePower **1 872.2** (load **78.0083 %**); baselineIdx = max(0, 1−3) = **0**; BatteryDetails: SR **444.7475**, PS **204.4025** | partially (AC5 totals) — **NEW** for combo powers/loads |
| B2 | B1 reference scenario (internal) | ref loads: propulsion **12 036.15**, hotel **3 876** ⇒ AePower **1 876** (78.167 %); refOptimalFoc > batteryOptimalFoc ⇒ **benefit > 0** | ✅ covered (benefit>0) — **NEW** for ref-load pinning via benefit formula |
| B3 | RP + battery: third-highest default | ≥3 combos ⇒ `SelectedBaselineIndex = n−3`; explicit index wins; no battery ⇒ n−1 | ✅ covered |
| B4 | Battery null vs PowerKw=0 vs modes=[] | identical results, BatteryDetails null | ✅ covered |
| B5 | Port-only battery, Port hours > 0 | Port L1 hotel demand = avg + L (155 ⇒ **157.945**); ModeAllocations = [Port]; Transit numbers unchanged vs no-battery | **NEW** |
| B6 | Multi-mode battery (Transit + Port) | ModeAllocations.Count = 2; SR/PS = **sums across modes** (documented semantics); benefit = Σ per-mode benefits | **NEW** |
| B7 | Battery + DP (dpEnabled, DP hours) | DP allocation present; DP L1 demand hotel += 28.5 (A7); no L2/L3 for DP (unchanged rule) | **NEW** |
| B8 | Battery + sail (Transit) | allocation Propulsion row uses **sail-adjusted** propulsion (A11 numbers), not raw EffectivePropulsionPower | **NEW** — QA carry-over #3 end-to-end |
| B9 | BenefitCost = benefit × fuelPrice | exact product | ✅ covered |
| B10 | Battery active but relevant mode has 0 hours | BatteryDetails **null**, no crash | ✅ covered |

## Family C — PTI (opt-in Excel-fidelity)

**Deficit plant "DP1":** ME 2×5 000, SG 500/engine, AE 3×800, propulsion 9 200, SM 0, hotel 2 000
⇒ sg = 1 000, ME needed 10 200 > 10 000 ⇒ deficit **200**.

| ID | Scenario | Expected | Trace |
|---|---|---|---|
| C1 | DP1 + MaxPti 500/engine | combo {2 ME, SG, 3 AE}: Pti **200**, MePower **10 000** (100 %), AE = 1 000 + 200×1.05 = **1 210** (load **50.4167 %**), AvailablePti **800** | ✅ covered |
| C2 | Deficit > PTI capacity | propulsion 11 500 ⇒ deficit 2 500 > 1 000 ⇒ **no valid combos** | ✅ covered |
| C3 | PTI aux overload | AE 2×800, propulsion 9 700 ⇒ deficit 700, aux PTI load **735** > headroom 600 ⇒ invalid | ✅ covered |
| C4 | Discharge gate boundary | headroom 800: band 900 ⇒ gated; band 700 ⇒ kept; **NEW**: band exactly 800 ⇒ kept (tolerance) | partially — **NEW** boundary case |
| C5 | Gate off when MaxPti = 0 | band 5 000, MaxPti 0 ⇒ no gating (bus-level, ADR-5) | ✅ covered |
| C6 | e2e allocation→gate wiring | EP + battery 1260 + MaxPti 500 ⇒ ok (headroom 1000 ≥ 200.6025); MaxPti 50 ⇒ headroom 100 < 200.6025 ⇒ throws | ✅ covered |
| C7 | PTI loss precision | pti 200 ⇒ aux extra exactly **210.0** (0.05 from BatterySettings) | ✅ covered via C1 — **NEW**: custom PtiLossFactor (e.g. 0.10 ⇒ 220) proves config-driven |
| C8 | PTI enters `ValidCombinationDto` | ptiKw = 200 in the combos list; null when unused | **NEW** |
| C9 | MaxPti > 0 without battery | PTI assist works standalone (DP1 valid); no gate (no band) | partially (C1 has no battery) — mark ✅ |
| C10 | Negative MaxPti | validation error | ✅ covered |

## Family D — L3 DRC residual variation (anti double-counting)

| ID | Scenario | Expected | Trace |
|---|---|---|---|
| D1 | V=500, hotel band 200 (unit) | variation **300**, shaved **200**, savings < no-band savings | ✅ covered |
| D2 | Band ≥ V | V=500, band 800 ⇒ variation **0**, shaved **500** (clamped), savings **0** | ✅ covered |
| D3 | Band 0 / battery off | identical to pre-change | ✅ covered |
| D4 | e2e Excel battery | hotel band **3.8** ⇒ Premium L3 variation **496.2**, `batteryShavedVariationKw` **3.8** | ✅ covered |
| D5 | Only hotel-side band offsets DRC | propulsion band (200.6) must NOT reduce L3 variation — Excel battery: shaved = 3.8, not 204.4 | implicitly in D4 — **NEW** explicit assert |
| D6 | Vessel-type lookup + band | no explicit HotelLoadVariationKw, VesselTypeName "Container" (1500) + band 3.8 ⇒ variation **1496.2** | **NEW** |

## Family E — Validation & UX guards

| ID | Scenario | Expected | Trace |
|---|---|---|---|
| E1 | PowerKw < 0 · CapacityKwh < 0 · Power>0 & Capacity=0 | 3 distinct errors | ✅ covered |
| E2 | Mode ∉ {Transit, DP, Port} (Anchor) | error | ✅ covered |
| E3 | DP mode without DpEnabled | error | ✅ covered |
| E4 | Power > 0, no modes | **warning**, valid=true | ✅ covered |
| E5 | Capacity < 0.5 × Power (30-min) | 1000/400 ⇒ warning; **NEW**: boundary 1000/500 ⇒ no warning | partially |
| E6 | User's live case 70/60 | 60 ≥ 35 ⇒ **no** warning | **NEW** (regression for the screenshot config) |
| E7 | MaxPti < 0 | error | ✅ covered |
| E8 | Well-formed battery | zero battery-typed errors/warnings | ✅ covered |

## Family F — MachCalcTool constants & explicitly-out-of-scope behaviours

| ID | Scenario | Expected | Trace |
|---|---|---|---|
| F1 | Effective settings = workbook constants | PtiLoss 0.05, η_chg 0.97, η_dis 0.97, η_motor 0.965; exactly 5 priority rows | ✅ covered (config binding test) |
| F2 | ConfigurationBinder list-append guard | bound rows = 5, not 10 (QA-A-1 regression) | ✅ covered |
| F3 | **Documented N/A (no test possible yet):** charge/discharge energy flows (0.97×0.97 round-trip = 0.9409), SoC simulation, `ηBatteryEndOfLifeMargin`, PTO charge gate (D-C1), per-machine PTI xor PTO (D-C2), Σ-SFOC ranking (§4.4) | assert absence is intentional — no silent behaviour | doc-only |

## Family G — Zero-regression invariants (guard rails for every future change)

| ID | Scenario | Expected | Trace |
|---|---|---|---|
| G1 | No battery, no PTI ⇒ full legacy suite | byte-identical behaviour (124 pre-battery tests) | ✅ standing |
| G2 | Battery for mode with 0 hours | Transit L1 optimum identical to no-battery | ✅ covered |
| G3 | MaxPti = 0 ⇒ pre-PTI Level 1 | identical optimum & combos, PtiPowerKw=0 everywhere | ✅ covered |
| G4 | Legacy `batteryCapacity` field | any value, battery=null ⇒ zero effect (dead stub stays dead) | **NEW** cheap guard |

---

## Coverage summary & the NEW-test worklist

Existing automated coverage: **~34 battery-related tests** across Increments A–E.
**NEW tests to write (17):** A2, A3, A5, A6, A7, A11, A12(values), A13(property), B1(combo pin),
B5, B6, B7, B8, C4(boundary), C7(custom loss), C8, D5, D6, E5(boundary), E6, G4.
Priority (risk × Excel-fidelity): **P0:** A2, A5, A13, B1, B8, D5 · **P1:** B5–B7, C4, C8, D6, A6, E6 ·
**P2:** останалите.

> **Addendum 2 (Family H — e2e effects of the Increment F inputs):** three pipeline-level
> scenarios added after gap analysis of the new inputs' wiring:
> **H1** DP redundancy covered 1:1 ⇒ SR 28.5 / PS 1.5 and a strictly positive benefit (the
> reference scenario carries the uncovered 400 kW as genset reserve);
> **H2** the covered redundancy (J = 400, thrust side) flows through the PTI discharge gate —
> MaxPti 250/engine passes (2-ME headroom 500), 100/engine gates every DP combo ("Insufficient
> PTI" ⇒ no valid combinations);
> **H3** uncovered mission reserve (2 370 kW) lands on the HOTEL side ⇒ only AE-3 combos survive
> (adjusted hotel 6 246 kW), and the mission covered band (630) fully absorbs a ±500 DRC variation
> (L3 savings 0, shaved clamped to 500). Suite: **206/206 green, all first-run**.

> **Addendum (Increment F, decision D4):** the two formerly-stubbed Excel inputs are now live —
> `DpRedundancyRequirementKw` (RESERVE row R5) and `MissionHeavyConsumerMaxKw` (R7, variation =
> full I3 value). 7 additional tests in `BatteryExcelLoadInputTests.cs` pin: 400 kW redundancy
> covered 1:1 first-priority; short-budget cascade (300→L=100+30); mission 3000 consuming the whole
> 1260 budget (PS 630 / SR 3019.15); zero-regression when absent; validation. Suite: **203/203**.

> **✅ Implemented 2026-07-13:** all NEW scenarios landed in
> `KSailCalc.Tests/Services/BatteryTestDesignScenarioTests.cs` (21 methods / 27 cases, IDs in test
> names). Full suite **196/196 green on the first run** — every hand-derived Excel number matched
> the implementation with zero code changes, which is the strongest fidelity evidence to date.

**Test-implementation notes:**
- All Family A/B/C/D numbers are exact to the digits shown (derivable by hand from §0); pin with
  `BeApproximately(x, 1e-6)`.
- B6's cross-mode Σ semantics is a *documented* design choice (gate battery.b future item) — the
  test pins current behaviour so a future semantics change is a conscious one.
- Family F3 items must NOT get "creative" tests — they are absent by decision; the doc entry is
  the record.
