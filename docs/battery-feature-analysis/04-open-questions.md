# 04 — Open Questions (need stakeholder decisions)

**Status:** Partially answered (see `05-decisions-log.md`) · **Updated:** 2026-07-13 · **Author:** Mary (Analyst)

Numbered for easy reference in meetings/chat. ★ = blocking for phase 1.

## Q1 ★ — What does each battery number constrain?
The sketch asks for **Capacity [kWh]** and **Power [kW]**, plus per-function kW. The Excel reference
uses a **single 1 260 kW budget** ("Max battery peak shaving and reserve capacity") and never uses
kWh. Proposal to confirm:
- `Power kW` = hard ceiling; validation `spinningReserveKw + peakShavingKw ≤ powerKw`.
- `Capacity kWh` = plausibility/duration check only in phase 1 (e.g. warn if capacity can't sustain
  the reserve for a minimum duration — what duration? 30 min? class rules?). No SoC simulation.

**Answer:** _partially (D2)_ — the Excel allocation consumes a single **kW budget** ("Max battery
peak shaving and reserve capacity", Optimal Setup I2), which maps to the sketch's `Power kW`.
Role of `Capacity kWh` still to confirm (duration plausibility vs energy accounting).

## Q2 ★ — Direct kW inputs or Excel-style allocation?
Excel computes reserve/peak-shaving kW from per-load variation factors + priority order + coverage
factors (RESERVE 100 %, DP-PS 50 %, propeller 35 %, hotel 5 %). The sketch has the user type the two
kW numbers directly. Confirm: user-entered kW wins, and the Excel allocation logic is **not** ported
(maybe later as a "suggest values" helper)?

**Answer:** ✅ **CLOSED (D2, 2026-07-13)** — the opposite: **follow the Excel**. Port the priority
allocation from the Load Demands sheet; the sketch's Spinning Reserve / Peak Shaving kW fields are
**computed outputs** of the allocation (direct override possible as later refinement).

## Q3 ★ — "Third highest" exactly means what?
Interpretation: among valid combinations sorted by FOC ascending, baseline = the **3rd from the
worst end** (`sorted[Count-3]`). To confirm:
- 3rd highest **FOC**, correct? (not 3rd highest engine count / power)
- If fewer than 3 valid combinations exist → fall back to worst? to best? error?
- Does the rule apply **only when a battery is configured**, and the no-battery default stays the
  worst combination? (Related bug: code default is last, XML doc says second-to-last — which is the
  *intended* no-battery default?)
- User override via the baseline table remains allowed?

**Answer:** ✅ **mostly CLOSED (D1, 2026-07-13)** — baseline stays **user-selectable** via the
existing Assumed Configuration table; "third highest" is only the **default pre-selection** when a
battery is configured. Still to confirm: clamping when fewer than 3 valid combos
(proposal: `sorted[max(0, Count-3)]`).

## Q4 ★ — Peak shaving vs Level 3 DRC double-counting
Both remove load variation. Proposed rule: DRC (L3) applies to the **residual** variation after the
battery's peak-shaving band is subtracted (`effectiveVariation = max(0, variationKw −
peakShavingKw)` in relevant modes). Confirm or supply the intended rule.

**Answer:** _still open_ — the Excel model predates/has no DRC concept (D2 does not resolve this);
the residual-variation proposal stands for confirmation.

## Q5 — Modes
a) Sketch shows Transit / DP / Port. Are **Anchor** and **Maneuvering** deliberately excluded, or
   should all 5 modes be selectable?
b) Today only Transit runs Level 2 (load-distribution optimization). If battery applies to DP/Port,
   should L2 be extended to those modes (bigger scope), or does the battery only affect their L1?

**Answer:** _pending_

## Q6 — PTI/PTO scope
The Excel feasibility gates route battery power through PTI/PTO shaft machinery. The app has neither.
Proposal: phase 1 models a **bus-connected battery** (direct to switchboard, no PTI/PTO limits);
PTI/PTO becomes a separate future feature. Acceptable?

**Answer:** ✅ **CLOSED (D2, 2026-07-13)** — proposal rejected; **follow the Excel ⇒ PTI/PTO is in
scope**. Note: the app's Shaft Generator already covers the PTO direction; the new machinery
concept is **PTI** (Max PTI per ME + 5 % transmission loss on aux side).

## Q7 — Energy accounting & losses
Should battery charging energy (and 0.97 × 0.97 round-trip efficiency, per MachCalcTool) be charged
back to the generators' fuel consumption, or is phase 1 purely a *capacity/reserve* model with no
energy-flow bookkeeping? (Recommendation: phase 1 = capacity/reserve only; document the assumption
in the report.)

**Answer:** ✅ **CLOSED (D2, 2026-07-13)** — follow the workbooks: the primary (PowerPlant) model is
capacity/reserve-based (no SoC simulation); where energy flows are counted, use MachCalcTool
constants η_charge = η_discharge = 0.97, η_electric-motor = 0.965.

## Q8 — Battery as dispatchable unit in Level 2?
"iEMS level 2: look the best from the load distribution" — is the battery just changing the demand
the gensets share (simple), or is it itself a setpoint in the distribution (battery discharges X kW
so gensets run at better SFOC points — needs an energy budget, contradicts Q7-simple)?

**Answer:** ✅ **CLOSED (D2, 2026-07-13)** — per the Excel model the battery is **not** a
dispatchable unit: it adjusts demand (peak-shaving relief + spinning-reserve additions) and
constrains feasibility (PTI/PTO capacity). L2 distributes the adjusted load among generators.

## Q9 — Financials
Does the battery change `TotalInvestment` (battery CAPEX per kWh/kW?) and therefore payback/ROI, or
is investment still only the iEMS level price? If CAPEX: source of unit prices?

**Answer:** _pending_

## Q10 — Baseline savings semantics with battery
Changing the baseline to "third highest" **reduces** headline savings for battery vessels (smaller
gap to optimum). Is that the intent (credibility), or should the battery's own benefit (fewer
gensets running / better loading) be shown as an *additional* savings line so the story stays
positive? Affects results UI and the client report.

**Answer:** _pending_
