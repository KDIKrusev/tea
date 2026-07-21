# 01 — Task Brief: Battery Configuration for the iEMS Savings Calculator

**Status:** Draft v1 (initial analysis) · **Date:** 2026-07-13 · **Author:** Mary (Analyst)

## 1. The request (from the Teams screenshot, meeting 5/22, Krishna Kumar Nagalingam)

The screenshot contains two parts: a requirements note and a UI sketch.

### 1.1 Requirements note (verbatim)

```
Battery Capacity : kWh
Battery Power : kW
Functions:
Spinning reserve : kW
Peaking Shaving: kW
```

### 1.2 UI sketch (verbatim structure)

```
Battery configuration
  Capacity : ____ kWh
  Power:    ____ kW

  Functions:
    Spinning Reserve: ____ kW
    Peak Shaving:     ____ kW

  Relevant Modes
    Transit ☐    DP ☐    Port ☐
```

### 1.3 Behavioural rules (verbatim)

> **Baseline with battery:** We select the configuration with third highest
>
> **iEMS level 1:** Best setup
> **iEMS level 2:** look the best form the load distribution

## 2. Interpretation

The KSailCalc app today computes fuel/CO₂ savings of an iEMS energy-management system at three
integration levels (L1 = optimal on/off engine setup, L2 = optimal load distribution, L3 = dynamic
ramp control), always **without any energy storage**. The request adds a **battery** to the model:

1. **New input group — "Battery configuration"**:
   - `Capacity` [kWh] — energy content of the battery (an input field `batteryCapacity` already
     exists in the API/client model but is a dead stub — it affects nothing).
   - `Power` [kW] — maximum charge/discharge power (new concept, does not exist anywhere yet).
   - Two **functions**, each with its own kW allocation:
     - `Spinning Reserve` [kW] — battery power held ready to replace an online generator / absorb a
       sudden load step, allowing the plant to run with **fewer generators online** (or the same
       generators at higher, more efficient load) while keeping the safety reserve.
     - `Peak Shaving` [kW] — battery absorbs/supplies the short-term **load variation** around the
       average, so generators are sized/loaded for the *average* rather than the *max* load.
   - **Relevant Modes** — checkboxes selecting in which operational modes (Transit / DP / Port) the
     battery functions apply. (The app has 5 modes: Transit, DP, Port, Anchor, Maneuvering — the
     sketch names 3; see open question Q5.)

2. **Baseline selection change ("with battery")**: today the default baseline is the **worst
   (highest-FOC) valid engine combination**. When a battery is configured, the baseline shall be the
   combination with the **third highest** fuel consumption (i.e. `sorted[Count-3]` in the ascending
   FOC ordering). Rationale (to be confirmed, Q3): a vessel that installed a battery already operates
   more efficiently than the theoretical worst case, so the comparison baseline must be less
   pessimistic to keep the savings claim honest.

3. **iEMS Level 1 with battery — "Best setup"**: L1 keeps its meaning (pick the lowest-FOC valid
   ON/OFF combination), but the combination space and the demand it must satisfy change, because the
   battery covers part of the reserve/peak requirements (per the Excel reference model, see
   `02-excel-model-analysis.md` §3):
   - Peak-shaving kW **reduces** the effective demand the plant must carry (the battery absorbs the
     variation band).
   - Spinning-reserve requirements not covered by the battery are **added** to the demand.

4. **iEMS Level 2 with battery — "look the best from the load distribution"**: L2 keeps its meaning
   (optimal load setpoints among running generators), evaluated on the battery-adjusted demand; the
   battery may additionally act as a dispatchable unit in the distribution (Q8).

## 3. Why this matters (business context)

- The Excel reference workbook (`PowerPlantSetupAdvisesIncludingPTIOAndbatteries_test.xlsx`) is the
  domain expert's prototype of exactly this feature: it allocates a battery budget across operation
  types (DP reserve → DP peak shaving → mission → propeller → hotel) and shows how peak shaving and
  spinning reserve change the recommended generator setup, including PTI/PTO interplay.
- Batteries are a headline selling point of hybrid iEMS installations; the calculator currently
  cannot demonstrate any battery benefit, which undersells configurations that include one.
- The baseline rule ("third highest") protects credibility: savings for battery vessels must not be
  inflated by comparing against the absolute worst-case plant operation.

## 4. In scope / out of scope (proposed — to validate)

> **Updated 2026-07-13 per decisions D1/D2 (`05-decisions-log.md`):** the Excel workbooks are the
> authoritative calculation reference; PTI/PTO moved INTO scope; spinning-reserve / peak-shaving kW
> are computed by the Excel allocation, not free user inputs.

**In scope:**
- Battery configuration input group (capacity, power, relevant modes) in client + API model +
  validation; **Spinning Reserve kW and Peak Shaving kW shown as computed outputs** of the Excel
  priority-allocation algorithm (Load Demands sheet — see `02-excel-model-analysis.md` §1.3).
- The Excel battery allocation: priority order over loads, RESERVE = 100 % coverage,
  PEAK SHAVING with per-load coverage factors; uncovered variation → added spinning reserve.
- Battery-adjusted demand in Level 1 optimization for the selected modes.
- **PTI modelling** (D2): Max PTI per main engine, 5 % PTI transmission loss, PTI/PTO feasibility
  gates for combinations (battery peak shaving must fit through available PTI/PTO). The existing
  Shaft Generator covers the PTO direction.
- Baseline default with battery → third-highest-FOC combination pre-selected; the user keeps the
  existing manual baseline selector (D1).
- Level 2 evaluated on battery-adjusted demand (battery is not a dispatchable setpoint unit — D2).
- Battery efficiency constants where energy flows are counted: η_charge = η_discharge = 0.97,
  η_electric-motor = 0.965 (MachCalcTool).
- Results display: show battery contribution explicitly (what was shaved, what reserve it provides).

**Out of scope (current understanding):**
- State-of-charge simulation, battery degradation / end-of-life margin (MachCalc placeholder = 0).
- Battery investment cost in ROI/payback (Q9 — still open).

## 5. Success criteria (draft)

1. A user can enter a battery (capacity, power, function allocations, relevant modes) and see it
   change: the valid-combination table, the default baseline pick, L1 optimal setup, L2 setpoints,
   and the headline savings — in a way that reconciles with the Excel reference for the same inputs.
2. With `batteryCapacity = 0` / battery disabled, results are **bit-identical** to today (no
   regression for existing users, profiles, and saved drafts).
3. All new behaviour covered by unit tests mirroring the Excel reference numbers where practical.
