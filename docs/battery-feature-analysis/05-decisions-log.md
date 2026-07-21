# 05 — Decisions Log

Running log of stakeholder decisions for the battery feature. Newest last.

## D1 — 2026-07-13 — Baseline stays user-selectable (Kamen)

The baseline is already user-selectable in the UI ("Assumed Configuration" radio table in the
Baseline panel → re-POST with `baselineIndex`). The code-vs-XML-doc mismatch about the *default*
(last vs second-to-last) is therefore **not a blocker** — the default is only a pre-selection.

Consequence:
- "Baseline with battery = third highest" is implemented as the **default pre-selection** of the
  existing selector when a battery is configured (`sorted[max(0, Count-3)]`), not as a new fixed
  rule. The user can still pick any row.
- The XML comment on `CalculatorInput.BaselineIndex` should still be corrected opportunistically
  (docs-only fix), but it does not gate the feature.
- Closes the blocking part of **Q3**; remaining sub-question: behaviour when fewer than 3 valid
  combinations exist (proposal: clamp to the worst available, i.e. `sorted[max(0, Count-3)]`).

## D2 — 2026-07-13 — The Excel workbooks are the authoritative calculation reference (Kamen)

For all open modelling questions, follow:
- `PowerPlantSetupAdvisesIncludingPTIOAndbatteries_test.xlsx` — **all battery-related calculations**
  (allocation, spinning reserve, peak shaving, PTI/PTO feasibility, setup recommendation);
- `MachCalcTool-20200821-v2.43 linear 1 (1).xlsm` — supporting parameters (battery charge/discharge
  efficiency 0.97/0.97, electric motor efficiency 0.965, hybrid alternatives).

Consequences (question closures — details updated in `04-open-questions.md`):
- **Q2 → CLOSED:** implement the Excel **priority allocation** (Load Demands sheet): battery budget
  cascades over loads in priority order with function tags (RESERVE = 100 % coverage,
  PEAK SHAVING = 50 % / 35 % / 5 % coverage by load type). The sketch's "Spinning Reserve kW" /
  "Peak Shaving kW" fields become **computed outputs** of this allocation (shown to the user),
  with direct override as a possible refinement.
- **Q6 → CLOSED (major scope impact):** PTI/PTO **is in scope**, because the Excel feasibility
  gates route battery peak shaving through PTI/PTO capacity. Note: the app's **Shaft Generator is
  already a PTO** in Excel terms (ME → electrical bus). The genuinely new machinery concept is
  **PTI** (electrical bus → shaft propulsion, with the 5 % loss factor and per-ME Max PTI cap).
- **Q7 → CLOSED:** where energy flows are counted, use MachCalcTool constants
  (η_charge = η_discharge = 0.97; η_electric-motor = 0.965 for PTI/PTO sizing).
- **Q8 → CLOSED:** per the Excel model the battery is **not** a dispatchable SFOC unit in the load
  distribution; it adjusts demand (peak-shaving relief, spinning-reserve additions) and constrains
  feasibility (PTI/PTO capacity). Level 2 semantics stay: distribute the (battery-adjusted) load
  among running generators.
- **Q4 (partially):** the Excel model has no DRC concept, so the double-counting rule remains ours
  to define; standing proposal (DRC applies to residual variation after battery shaving) stays open
  for confirmation.

Still open after D1/D2: **Q1** (role of kWh vs kW — Excel uses a single kW budget; sketch asks for
both), **Q4** (residual-variation rule), **Q5** (mode list; L2 beyond Transit), **Q9** (battery
CAPEX in ROI), **Q10** (how to present the battery benefit in results/report).

## D3 — 2026-07-13 — Architecture review outcomes (Kamen: "follow your recommendations")

Design review of `06-architecture-design.md` (points R1–R7); Kamen approved the architect's
recommendation on every point:

- **R1** — Allocation stays in the orchestrator (`CalculatorService`); Level 1 remains a pure
  combinations engine (ADR-1 confirmed).
- **R2** — Coverage factors / priorities live in `appsettings` (`BatterySettings`) for phase 1;
  a UI "Advanced" override is a possible later enhancement (ADR-2 confirmed).
- **R3 → closes Q10** — **Dual-scenario rule**: with battery active, run L1 twice per relevant
  mode — with-battery demand (avg + ΣL) vs without-battery reference (avg + ΣH, budget 0). The
  delta is reported as a separate **"Battery benefit"** line (`BatteryDetails.BenefitFocTonPerYear`),
  not folded into L1/L2/L3 tier savings.
- **R4** — PTI default = 0 (off); the client only *suggests* SG capacity when the battery section
  is enabled (ADR-5 as corrected).
- **R5** — User-pinned baseline is tracked by **combination signature**
  (`activeMeCount/sgEnabled/activeAeCount`), not by list index.
- **R6** — Spinning Reserve / Peak Shaving are **read-only computed outputs** in the UI (per D2);
  revisit only if Krishna insists on manual override.
- **R7** — Build order confirmed: **A → B → D → C → E** (client before PTI, so users see the
  feature early).

Q4 default (L3 uses residual variation after battery shaving) remains the working assumption
pending confirmation; Q1, Q5, Q9 still open but none blocks increments A–B.

## D4 — 2026-07-13 — The Excel is the FINAL authority; no external questions (Kamen)

"There is no reason for questions to Krishna — all calculations are in the Excel; take them from
there and apply them to the rest of the application."

Resolutions read directly off the workbook:
- **Q1 CLOSED:** kWh does not participate in the power-plant advisor at all (I2 budget is kW).
  Capacity stays informational (plausibility warning only).
- **Q4 CLOSED:** the Excel has no DRC concept — the residual-variation working rule is final.
- **Q5 CLOSED:** Load Demands operation types = DP / Mission / Sailing (Transit) / Harbour (Port).
  No Anchor/Maneuvering. Current mode set is correct; L2 stays Transit-only (no Excel counterpart).
- **Q9 CLOSED:** the advisor workbook has no cost model — battery CAPEX stays out of iEMS ROI.
- **Gap identified → Increment F:** two Excel inputs were still stubbed at 0:
  `DP Redundancy requirement` (O2 — RESERVE row; NOT part of Σavg demand, O7 sums from O3) and
  `Mission HVY consumer max` (I3 — Mission row variation = full I3 when a mission load exists,
  `G7 = IF(E7>0, I3, 0)`, not avg×factor). Both to be implemented end-to-end.
- **Deliberate remaining deviations (documented, NOT gaps):** ranking metric (app: min FOC ton/h;
  Excel: min Σ SFOC g/kWh — switching would silently change every existing non-battery result);
  PTO charge gate (in the app's average-load model, charging happens in the load down-swing when
  genset headroom exists by construction — the Excel needs the Y-gate only for its shaft-PTO path);
  per-machine PTI⊕PTO exclusivity (aggregate plant).

## D6 — 2026-07-20 — Two defects found during live testing, fixed (Kamen: "оправи каквото си намерил")

Both surfaced while walking the Excel scenario through the UI; neither was introduced by the
battery work, but both became visible because the battery moves the baseline to a different
engine mix than the optimum.

1. **QA-C-1 closed — infeasible plant now answers 400 with an actionable reason.** New
   `NoValidCombinationException` carries a user-facing message; `Level1OptimizationService` tallies
   *why* combinations were rejected (structural / PTI assist / aux overload / insufficient power /
   battery PTI gate) and explains the dominant cause with the actual numbers, e.g. *"the battery
   needs 200.6 kW of PTI capacity … but only 100 kW is available. Increase the PTI capacity per
   main engine (currently 50 kW), reduce the battery power, or clear the PTI field…"*. The
   controller returns it in the existing `ValidationResult` shape, so the Angular client renders it
   with no change. Numbers formatted with `InvariantCulture` (server locale was emitting `200,6`).

2. **ME/AE fuel split of the optimized result was taken from the BASELINE ratio.** With the
   optimum running 0 AE while the (3rd-highest) baseline ran AEs, the UI reported
   "Auxiliary engines not required · 0.0 % load" next to 3 067 t/yr of aux fuel. `FocBreakdown`
   already computed `OptimalMeFoc` but nothing read it — the intent was clearly there and never
   wired. Now the split uses the optimum's own ME/AE ratio (`OptimalAeFoc` added), falling back to
   the baseline ratio only when the optimum has no FOC. Totals, savings and costs are unchanged;
   only the breakdown (and per-fuel CO₂ when ME/AE burn different fuels) becomes truthful.
   Pinned by `OptimizedFuelSplit_WhenOptimumRunsNoAuxEngines_AssignsNoFuelToAux` and
   `OptimizedFuelSplit_FollowsOptimalCombinationRatio_NotBaseline`. Suite: 211/211.

3. **Per-engine CO₂ on the ME/AE cards didn't sum to the panel total.** The client recomputed
   card CO₂ as `optimizedME × 3.206` (a hard-coded constant predating per-fuel factors, Epic 3),
   while the backend total used the real fuel factor (MDO = 3.93267) — an ~18 % discrepancy on the
   same panel. Fix: the API now returns `OptimizedMeCO2/OptimizedAeCO2` and
   `BaselineMeCO2/BaselineAeCO2`, each computed with that engine's own fuel factor via the single
   `Co2ForMainEngines`/`Co2ForAuxEngines` source; the two variant/baseline panels display those
   instead of multiplying by the constant. `CO2_EMISSION_FACTOR` marked `@deprecated` (kept, no
   longer used). Pinned by `PerEngineCo2_IsReported_AndSumsToTheTotals` (LNG ME + MGO AE ⇒ a wrong
   single-constant recompute cannot pass). Suite 212/212; `ng build` clean.

## D5 — 2026-07-13 — PTI donor set = installed machines (union model) (Kamen, AUDIT-1)

The combination-table deep dive (07 audit §5b, AUDIT-1) showed the Excel donates PTI only from
IDLE engines' shaft machines (electric drive of the non-running shaft, row 59), while the app
allowed boost only on RUNNING shafts. Kamen approved **option 3 — the union**: PTI capacity =
`MeCount (installed) × MaxPtiPerEngineKw`, covering both the Excel's second-shaft scenarios and
real-world running-shaft boost. Implemented as Increment G (one-line basis change +
`G_IdleMachinePti_EnablesExcelRow59StyleCombination` test; suite 207/207). AUDIT-2 (reserve
routing) and AUDIT-3 (workbook's vacuous X/Y gate, "RESERVES" typo, empty no-battery table)
stay documentation-only by the same decision.
