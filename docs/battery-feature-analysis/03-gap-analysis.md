# 03 — Current System vs. Battery Feature: Gap Analysis

**Status:** Draft v1 · **Date:** 2026-07-13 · **Author:** Mary (Analyst)

## 1. Current system snapshot

### 1.1 Backend (ASP.NET Core, .NET 10 — project `KSailCalc.Api`)

- **Endpoints:** `POST api/calculator/calculate-all-variants` (main calc), `GET api/app-data/initial`
  (bootstrap), `GET api/app-data/vessel-config` (parametric vessel resolution),
  `POST api/app-data/refresh-cache`.
- **Pipeline** (`Services/CalculatorService.cs`): validate → sail contribution (Transit) →
  per-mode optimization → aggregate → 3 tiers.
  - **Transit** gets the full **L1 → L2 → L3** pipeline; **DP / Port / Anchor / Maneuvering** run
    **L1 only** (`CalculatorService.cs:90-112`).
  - Tier savings are cumulative: Advanced = L1; Pro = L1+L2; Premium = L1+L2+L3
    (`CalculatorService.cs:200-206`).
- **Level 1** (`Level1OptimizationService.cs`): enumerates ME(0..N) × SG(on/off) × AE(0..N)
  combinations, validates (hotel must be fully covered by SG+AE — **ME has no PTO**, line 162;
  AE load ≤ 90 %; ME power ≤ capacity), distributes load (SG covers hotel first, AE remainder;
  ME = propulsion + SG shaft load), computes FOC via SFOC interpolation, sorts ascending by
  `FocTonPerHour` (tie-break: fewer running engines).
  - `optimal = sorted[0]`; **baseline default = `sorted[Count-1]` (the highest-FOC valid combo)**,
    overridable by `CalculatorInput.BaselineIndex` (`Level1OptimizationService.cs:53-78`).
- **Level 2** (`Level2OptimizationService.cs`): sweeps AE load distributions (10–90 % in 2 % steps,
  last engine absorbs remainder) to minimize total FOC; SG load is fixed from L1.
- **Level 3** (`Level3DrcService.cs`): Dynamic Ramp Control — reduces load variation by 20 %
  (`DrcReductionFactor = 0.80`), 2-minute cycles, annualized savings; variation kW from
  `HotelLoadVariationKw` input or per-vessel-type config (`appsettings.json → VesselVariations`).
- **SFOC** (`SfocService` + `SfocInterpolationHelper`): linear interpolation over per-engine curves
  from DB (`EngineType.SfocDataJson`), fallback 220 g/kWh.
- **Financials:** investment = (`IemsPriceNOK` + `CommissioningNOK`) / `UsdToNokRate` per
  `IntegrationLevel` row; payback, 10-yr ROI, CO₂ per fuel type factors from `appsettings.json`.

### 1.2 Frontend (Angular 18, standalone components, `cl/`)

- Single-page calculator: left = reactive form (vessel config → engine config → 5 operational
  modes → financial/L3 → wind/sails), right = results accordion (power demands, baseline panel with
  a **clickable valid-combinations table** that re-POSTs with `baselineIndex`, three level panels,
  tier comparison, Chart.js FOC/CO₂ charts, printable client report).
- Auto-calculate debounced 500 ms; profiles + auto-draft in `localStorage`
  (`PROFILE_SCHEMA_VERSION = 2`, includes `batteryCapacity`).
- API models mirror backend (`cl/src/app/calculations/calculator.types.ts`).

### 1.3 Database (`VoyageEnergyDB`, local SQL Server)

- `IntegrationLevel`: 3 rows — L1: 1.0 M NOK + 100 k commissioning; L2: 1.205 M + 200 k;
  L3: 1.8 M + 300 k. `BaseEfficiencyFactor` (0.97/0.955/0.94) is legacy, unused by the pipeline.
- `EngineType`: 507 active rows (real engine catalog — MaK, Wärtsilä, MTU, CAT, WinGD, B&W… with
  SFOC JSON curves, SG capacities, fuel families).
- `VesselType`: 19 active parametric buckets (Bulk, Tanker, OSV, Container) with speed-power curves
  and operational profiles (JSON).
- `Configurations`: sail-contribution lookup table. No battery-related tables/columns exist.

## 2. Keyword audit — what exists today for the new concepts

| Concept | Backend | Frontend | DB |
|---|---|---|---|
| Battery capacity (kWh) | `CalculatorInput.BatteryCapacity` — validated ≥ 0, **never used in any calculation** | form control exists, **no visible input in any template**, always sent as 0 | — |
| Battery power (kW) | — | — | — |
| Spinning reserve | — | — | — |
| Peak shaving | only a UI hint string for the L3 variation field | same | — |
| PTI | — (no matches) | — | — |
| PTO | explicitly **not modelled** (comments in `Level1OptimizationService.cs:162,197`) | — | — |
| Modes (Transit/DP/Port/…) | 5-mode enum, full support | full support | in vessel profiles |
| Baseline selection | `BaselineIndex` + sorted combo list, default = worst | baseline panel with combo table | — |
| "Third highest" rule | **does not exist** | — | — |

## 3. Gap map — screenshot requirement → what must change

| # | Requirement | Where the gap is | Change needed |
|---|---|---|---|
| G1 | Battery Capacity kWh input | UI missing; API field dead | Surface field in a new "Battery configuration" form section; wire into calc (role: energy duration, Q1) |
| G2 | Battery Power kW input | Missing everywhere | New API field + UI + validation (functions' kW ≤ power) |
| G3 | Spinning Reserve kW function | Missing everywhere | New input; L1 demand adjustment: reserve requirement covered by battery does not force extra running gensets; uncovered reserve adds to demand (Excel STEP 1–2) |
| G4 | Peak Shaving kW function | Missing everywhere | New input; reduces the peak the plant must carry in relevant modes; interacts with L3 DRC variation (double-count risk, Q4) |
| G5 | Relevant Modes checkboxes (Transit/DP/Port) | No per-mode battery concept | New input (mode flags); battery adjustments applied only in flagged modes' L1/L2 runs |
| G6 | Baseline with battery = third-highest combo | Default is highest-FOC | New rule in `Level1OptimizationService` baseline pick when battery active: `sorted[max(0, Count-3)]`; keep user override via `BaselineIndex` |
| G7 | iEMS L1 = best setup (with battery) | L1 ignores battery | Battery-adjusted mode loads before combination enumeration |
| G8 | iEMS L2 = best from load distribution (with battery) | L2 ignores battery | Runs on adjusted demand; optionally battery as dispatchable unit (Q8) |
| G9 | Results transparency | No battery in results | Response DTOs + panels: show shaved kW, reserve provided by battery, adjusted vs raw demand |

## 4. Discrepancies & risks found during analysis

### 4.1 Baseline documentation vs code — RESOLVED as non-blocking (D1)
`CalculatorInput.BaselineIndex` XML comment claims default is "second-to-last (`sorted[^2]`)", but
the code uses **the last** element (`Level1OptimizationService.cs:62-64`). Per D1 the baseline is
user-selectable in the UI, so the default is only a pre-selection — fix the XML comment
opportunistically; "third highest" becomes the pre-selection when a battery is configured.

### 4.2 No PTI path in the app — now IN SCOPE (D2)
The Excel model validates combinations through PTI/PTO capacity (battery peak shaving must
physically flow through the shaft machinery). Decision D2 (follow the Excel) puts this in scope.
Observation that shrinks the gap: the app's **Shaft Generator is already the PTO direction**
(ME shaft → electrical bus; `MePower = propulsion + sgPower` in `DistributeLoad`). What is genuinely
new is **PTI** (electrical bus → shaft motor): per-ME `MaxPtiKw`, the 5 % PTI transmission loss
added to aux demand, and the feasibility gates ("Insufficient PTI" / PTO-side check). The misleading
"ME has no PTO" comments in `Level1OptimizationService.cs:162,197` should be reworded (they mean:
*hotel cannot be fed beyond SG capacity*).

### 4.3 Peak shaving vs Level 3 DRC double-counting
L3 already monetizes reducing load variation by 20 % (DRC). Battery peak shaving also removes
variation. If both apply to the same ±kW band, savings would be counted twice. A rule is needed
(e.g. L3 variation input reduced by the battery-shaved band, or DRC applies only to the residual
variation) — Q4.

### 4.4 Selection metric mismatch
Excel recommends by **min Σ SFOC [g/kWh]**; the app by **min FOC [ton/h]**. These can rank setups
differently (SFOC sum ignores how much power each machine delivers). Keep the app's FOC metric
(physically correct) and note that exact Excel reconciliation is by-formula, not by-ranking.

### 4.5 Mode model mismatch
Sketch lists Transit / DP / Port; the app also has Anchor and Maneuvering. Also today **only Transit
runs L2**, so "iEMS level 2 … load distribution" with a DP/Port battery raises the question whether
L2 must be extended to those modes (Q5b).

### 4.6 The current `batteryCapacity` stub
Profiles (schema v2) already persist `batteryCapacity`. Extending the battery model must keep old
profiles loadable (schema migration to v3 or defaulting new fields).

## 5. Proposed implementation direction (for validation with PM/architect)

Phased, smallest-credible-increment first. **Updated 2026-07-13 per D1/D2** (Excel is authoritative;
PTI in scope; allocation ported, not direct kW inputs):

- **Phase 0 — Alignment:** remaining open questions (Q1 kWh role, Q4 DRC residual rule, Q5 modes,
  Q9 CAPEX, Q10 presentation); reword misleading PTO comments + `BaselineIndex` XML doc.
- **Phase 1 — Battery allocation engine (backend, pure logic):** port the Load Demands sheet:
  priority-ordered allocation of the battery kW budget over per-mode loads (RESERVE 100 %,
  PEAK SHAVING coverage factors), outputs = ± shaved band per load + additional spinning reserve.
  Unit-test against the workbook's saved example (budget 1 260 kW → ΣJ = 204.40, ΣL = 444.75).
- **Phase 2 — Model & API:** `BatteryConfiguration` input (`capacityKwh`, `powerKw`, `modes[]`),
  computed `spinningReserveKw`/`peakShavingKw` in the response; validation.
- **Phase 3 — PTI + Level 1 integration:** per-ME `MaxPtiKw` (input/config), 5 % PTI loss, Excel
  feasibility gates ("Insufficient pwr / Insufficient PTI / PTO-side"), battery-adjusted demand per
  relevant mode; baseline "third highest" **pre-selection** (D1) with clamping for < 3 combos.
- **Phase 4 — Level 2/3 integration:** L2 on adjusted demand (battery not dispatchable — D2); L3
  residual-variation rule (pending Q4).
- **Phase 5 — Client:** Battery configuration section (per sketch; SR/PS shown as computed values),
  results panels, report, profile schema v3.
- **Phase 6 — Reconciliation & tests:** unit tests keyed to Excel reference numbers (mind the
  ranking-metric difference §4.4); zero-battery regression suite.

Each phase maps naturally onto BMAD brownfield stories (SM → `create-next-story`).
