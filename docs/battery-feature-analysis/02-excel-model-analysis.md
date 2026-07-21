# 02 — Excel Reference Model Analysis

**Status:** Draft v1 · **Date:** 2026-07-13 · **Author:** Mary (Analyst)

Reverse-engineering of the two reference workbooks. Cell references are given so formulas can be
re-checked in Excel; values shown are from the workbook's saved state (example scenario).

---

## 1. `PowerPlantSetupAdvisesIncludingPTIOAndbatteries_test.xlsx` (PRIMARY reference)

3 sheets: **Optimal Setup**, **Load Demands**, **SFOC**. Together they form a one-scenario power
plant advisor: given load demands, a battery budget, and 6 generators, it recommends which
generators to run.

### 1.1 Plant model (Optimal Setup, rows 1–14)

Six machines, columns B..G in fixed order **G1, G6, G2, G3, G4, G5**:

| Machine | Role | Max Gen [kW] | Max PTI [kW] | Max PTO [kW] |
|---|---|---|---|---|
| G1 | Main engine / Prop 1 | 24 000 | 3 250 | 3 250 |
| G6 | Main engine / Prop 2 | 24 000 | 3 250 | 3 250 |
| G2..G5 | Aux gensets | 4 000 each | — | — |

- `MAX ME + PTI` = 27 250 per ME (B12); total plant `MAX Aux + PTO` = 61 000 (H14).
- **PTI** (Power Take-In): electric motor on the shaft — aux/battery power can *assist propulsion*.
- **PTO** (Power Take-Off): shaft generator — main engine can *feed the electrical bus*.
- `Losses for PTI propulsion` = **5 %** (I4): PTI power drawn from aux gensets is grossed up by 5 %.
- Notes in sheet: "ME loss for power to hotel load (SG PTO → switchboard) ≈ same as for Aux gensets";
  "G2–G5 loss for propulsion power is ~6–15 % higher than directly from engine (mechanical shaft)".

### 1.2 Scenario inputs (rows 2–8)

| Input | Cell | Value |
|---|---|---|
| **Max battery peak shaving and reserve capacity** | I2 | **1 260 kW** (single battery budget) |
| Mission HVY consumer load at max | I3 | 3 000 kW |
| PTI propulsion losses | I4 | 5 % |
| DP redundancy demand | O2 (var. factor P2=0) | 0 |
| DP (thruster) load demand | O3 | 0 |
| Mission load demand | O4 | 0 |
| Propeller load demand | O5, variation factor P5 | 11 463 kW, ±5 % |
| Hotel load demand | O6, variation factor P6 | 3 800 kW, ±2 % |
| Sum of average demands | O7 | 15 263 kW |
| Battery peak-shaving corrections (from Load Demands col J) | Q2:Q6 → Q7 | **−204.40 kW** |
| Additional spinning reserve (from Load Demands col L) | R2:R6 → R7 | **+444.75 kW** |
| **Total demand** | R8 = O7 + R7 | **15 707.75 kW** |

Key insight: **Total demand = Σ average loads + spinning-reserve additions**, while the
**peak-shaving relief enters via the PTI/PTO battery columns (X/Y)**, not by lowering R8 — see 1.4.

### 1.3 Battery allocation algorithm (sheet **Load Demands**) — the heart of the feature

The battery budget (K4 = 1 260 kW, from Optimal Setup I2) is allocated over operation loads in a
**strict priority order** (rows 5→9). Each row is one load with a function tag:

| # | Load (row) | Function tag (col C) | Coverage factor D | Avg load E | Variation source |
|---|---|---|---|---|---|
| 1 | DP (thruster) load **reserve** (R5) | `RESERVE` → D = 100 % | 0 | DP class redundancy req. |
| 2 | DP (thruster) load demand (R6) | `PEAK SHAVING` → D = 50 % | 0 | environment + DP dynamics |
| 3 | Mission load demand (R7, e.g. crane) | `PEAK SHAVING` (D = 50 %) | 0 | max heavy-consumer usage (I3) |
| 4 | Propeller demand (R8) | `PEAK SHAVING`, **D = 0.35** | 11 463 | ±5 % environmental |
| 5 | Hotel load demand (R9) | `PEAK SHAVING`, **D = 0.05** | 3 800 | ±2 % start/stop consumers |

Per-row math (columns E..L):

```
G  MaxActual      = E + variationFactor × E          (max in the operating period)
H  MaxVariation   = G − E                            (peak-shaving potential or reserve; for the
                                                      RESERVE row: H = G, the whole requirement)
I  MaxBatteryUse  = MIN(remaining budget K_prev, H)
J  ±BatteryRange  = I × D                            (the ± band the battery actually covers)
K  RemainingBudget= MAX(K_prev − I, 0)               (cascades to the next priority row)
L  AdditionalSpinningReserveNeeded = H − I × D       (what the battery could NOT cover → must be
                                                      carried by online generators)
```

Totals (row 10): `ΣI = 649.15` (battery kW committed), `ΣJ = 204.40` (± peak-shaving band),
`ΣL = 444.75` (extra spinning reserve). These feed back into Optimal Setup:
- col Q (`Battery peak shaving (+ −)`): `Q5 = −J(propeller) = −200.60`, `Q6 = −J(hotel) = −3.80`
- col R (`Spinning reserve`): `R5 = +372.55`, `R6 = +72.20`, … → Σ = +444.75 added to total demand.

**Interpretation:** `RESERVE` rows are covered kW-for-kW (100 %); `PEAK SHAVING` rows are covered
with a function-specific coverage factor (50 % / 35 % / 5 %) reflecting how much of the variation
band the battery realistically absorbs in that mode. Anything uncovered becomes spinning reserve
that the running generators must provide (demand increase).

### 1.4 Setup enumeration and selection (Optimal Setup, rows 17–75)

58 rows = all meaningful ON/OFF combinations of {G1, G6, G2, G3, G4, G5}. Per combination row *r*:

- **Feasibility (col H)** — three failure gates:
  `IF(TotalDemand > SetupMax, "Insufficient pwr", IF(unmet-PTI-need OR X_r < Q$5, "Insufficient PTI", IF(PTO-overload OR Y_r < …, …)))`
  i.e. a setup is invalid if (a) it cannot carry total demand, (b) propulsion needs PTI the setup
  can't deliver — **including the PTI capacity the battery needs for peak shaving** (X_r ≥ battery
  peak-shave band), (c) symmetric PTO/charging check.
- **Load fraction (col I)** = TotalDemand / SetupMax (uniform loading assumption).
- **ME side:** L = ME_max × I (propulsion delivered by MEs); M = PropellerDemand − L → if M > 0 the
  balance must come as **PTI** (N/O/T/V, capped at 3 250 per ME); if M < 0 the surplus goes out as
  **PTO** (U/W, capped at 3 250).
- **Aux side:** Q_r = M × 5 % (PTI transmission losses added to aux demand); R_r = Aux_max × I + Q_r;
  S = aux load fraction.
- **Battery columns:** X = `Available PTI for Peak shaving from battery`, Y = `Available PTO for Peak
  shaving to battery` — leftover PTI/PTO capacity after propulsion needs, clamped to the battery
  peak-shaving band (±Q$5/±R-side). A combination is only valid if the battery can actually inject
  (PTI) and absorb (PTO) its peak-shaving band through the shaft machinery.
- **SFOC (cols Z..AE):** per selected machine, SFOC interpolated linearly (`FORECAST.LINEAR` over the
  bracketing points of that machine's SFOC curve) at its load fraction (I for MEs, S for aux).
- **Selection (col AF):** `TOTAL SFOC = Σ SFOC of selected machines`; **recommended setup = MIN(total
  SFOC)** over all valid rows (AF17 = `MIN(AF18:AF75)` = 344 in the example).

> ⚠️ Note: the Excel ranks setups by the **sum of SFOC values [g/kWh]**, not by fuel mass flow
> [ton/h]. The app's Level 1 ranks by FOC ton/h (power-weighted), which is physically sounder. This
> difference must be kept in mind when reconciling numbers (see 03-gap-analysis §4.4).

- A **second table** (rows 76+) repeats the same headers **without** the battery PTI/PTO columns —
  i.e. the *baseline (no-battery) comparison table*. In the saved file it holds only headers; the
  battery table above is the populated one.

### 1.5 SFOC sheet

Per-machine SFOC curves, load 10 %→100 % in 10 % steps: Aux engines 2–5 and main engines 1 & 6.
Extrapolation: value at 0.1 % load = `INTERCEPT` of the two lowest points; value at 100 % =
`FORECAST.LINEAR` of the two highest points. This matches the app's `SfocInterpolationHelper`
approach (linear interpolation + low-load extrapolation).

---

## 2. `MachCalcTool-20200821-v2.43 linear 1 (1).xlsm` (SECONDARY reference)

13 sheets; a full annual voyage-energy/fuel/cost simulator (the ancestor of the current app's
domain). Relevant to the battery task:

- **Machinery alternatives** include battery variants (OperationProfile_PowerSpeed R8):
  `HYB_PTI`, `HYB_BattSmall`, `HYB_BattMed`, `DE`, `DE_BattSmall`, `DE_BattMed`, `DE_BattLarge`,
  and a `Pure Battery` / `Alt16-Full_Battery` alternative in StatisticsAnnual (R25, R43, R85).
- **Battery parameters** (Parameter_data, rows 62–69):
  - `ηElectricMotor` = 0.965 — "only used for the estimate of size of PTO/PTI and flettner electric
    motor"
  - `ηBatteryDischarge` = **0.97**
  - `ηBatteryCharge` = **0.97**
  - `ηBatteryEndOfLifeMargin` = 0 (placeholder)
  - a "Battery losses" curve section (v2.3 changelog: "Added efficiency curve for battery losses,
    added new curves for trafo losses").
- Front_Page changelog confirms PTI/PTO gearbox modelling (v2.41) — i.e. the legacy tool models the
  same PTI/PTO concepts the primary workbook uses.

**Use for our task:** source of default efficiency constants (0.97 charge / 0.97 discharge ≈ 0.94
round-trip) if/when battery energy accounting is brought into scope, and evidence that the domain
treats battery sizes as discrete alternatives (Small/Med/Large) — a possible future preset UX.

---

## 3. Consolidated algorithm (what the feature must compute)

Putting §1 together, the battery-aware calculation pipeline in the Excel model is:

```
INPUT:  battery budget (kW), per-load average demands + variation factors,
        per-load function assignment (RESERVE / PEAK SHAVING) + coverage factor,
        priority order of loads, machine list (max gen, max PTI, max PTO, SFOC curves)

STEP 1  Allocate battery budget over loads in priority order
        → per-load: covered ± band (J), uncovered spinning reserve (L)

STEP 2  Effective total demand = Σ(avg demands) + Σ(uncovered spinning reserve)

STEP 3  Enumerate machine ON/OFF combinations; for each:
          - check power sufficiency vs effective demand
          - check PTI path: propulsion deficit + battery peak-shave injection ≤ PTI capacity
          - check PTO path: propulsion surplus + battery charging ≤ PTO capacity
          - distribute load, add PTI transmission losses (5 %) to aux demand
          - interpolate SFOC per machine at its load

STEP 4  Recommend the valid combination with the lowest total SFOC
        (app equivalent: lowest FOC ton/h — see gap analysis)
```

The screenshot's requirements are a **productized simplification** of this: instead of the
per-load priority allocation (STEP 1), the user directly enters the two outcomes — `Spinning
Reserve kW` and `Peak Shaving kW` — plus which modes they apply to. This removes the need for
per-load variation factors and priority configuration in the UI (but see open questions Q2, Q5).
