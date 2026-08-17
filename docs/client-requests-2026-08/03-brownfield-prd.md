# KSailCalc — Client Requests 2026-08 Brownfield Enhancement PRD

**PM:** John (BMAD) · **Version:** 1.0 (approved) · **Date:** 2026-08-13
**Inputs:** `01-analyst-brief.md`, `02-decisions-log.md` (D-DE1..D-DE5 confirmed; O-1..O-5 open)

> Note on location: the template default (`docs/prd.md`) is the existing sharded product PRD (v4).
> This enhancement PRD deliberately lives in the wave folder to keep the client-request package
> self-contained, mirroring how `docs/battery-feature-analysis/` packaged the battery feature.

---

## 1. Intro — Project Analysis and Context

**Analysis source:** IDE-based fresh analysis by the BMAD analyst (see `01-analyst-brief.md`),
grounded in file/line references; no assumptions carried without code verification.

**Current state:** KSailCalc computes fuel savings for ships: .NET 10 backend (441/441 green,
frozen golden snapshots, `GOLDEN_UPDATE=1` forbidden) + Angular 18.2 client (68/68). Every plant
today has ≥ 1 main engine; propulsion is always carried by the ME shaft
(`Level1CandidateBuilder.TryDistribute`). The battery cascade, two-world Battery Benefit, and
baseline rules are plant-shape-neutral upstream of the distribution step.

**Available documentation:** ✓ tech stack / source tree / coding standards
(`docs/architecture/*`), ✓ calculation pipeline map (`docs/qa/manual-test-scenarios/calculations/00-ORIENTATION.md`),
✓ scenario catalogue + expected values (`docs/qa/manual-test-scenarios/README.md`), ✓ customer
case notes (`docs/qa/customer-notes/`), ✓ client-supplied Wärtsilä mode reference
(`docs/pics/`, summarized in the analyst brief).

**Enhancement type:** New Feature Addition (Epics 1, 2) + UI polish (Epic 3).
**Impact assessment:** Epic 1 — Moderate (gated additions, zero change for MeCount ≥ 1);
Epic 2 — Moderate + golden unfreeze (blocked); Epic 3 — Minimal.

## 2. Goals and Background Context

**Goals**

- A ship engineer can model a diesel-electric vessel (0 installed main engines; AEs + thrusters
  carry everything) and get honest fuel, battery and savings figures.
- Battery input rows match the client's operational model (Mission/DP-redundancy DP-only; new
  "Others" demand for the remaining modes) — once its open decisions are resolved.
- The Battery Contribution table reads unambiguously (± band labels; Covered/Peak-Shaving relabel
  already promised to the client).
- Existing behaviour for every MeCount ≥ 1 vessel is bit-for-bit unchanged; goldens 01–35 stay
  frozen and green throughout.

**Background:** The client sent three requests backed by a Wärtsilä hybrid-propulsion reference
(Aker AH012/AH04). Its diesel-electric modes (6, 9) show propulsion fed from auxiliary engines
through converters — the plant family KSailCalc cannot represent today. Requests 2 and 3 refine
the battery input/output panels toward the client's mental model. Analysis confirmed the cascade
and Benefit machinery are already neutral to the plant shape, making an isolated implementation
realistic.

**Change log**

| Change | Date | Version | Description | Author |
|---|---|---|---|---|
| Draft | 2026-08-13 | 0.1 | Initial PRD from analyst brief + confirmed decisions | John (PM) |
| Approved | 2026-08-13 | 1.0 | Approved by Kamen (YOLO run): E3 to SM/Dev now, E1 to Architect, E2 parked | John (PM) |

## 3. Requirements

### Functional

- **FR1:** The system accepts `meCount = 0` as valid input (backend + client form + profile
  import) for a plant with ≥ 1 auxiliary engine.
- **FR2:** At `meCount = 0`, all demand (propulsion' + hotel', including battery-uncovered
  reserve from both plant sides) is distributed to auxiliary engines;
  `aePower = hotel' + propulsion' × (1 + ElectricPropulsionLossFactor)`; ME and SG power are 0 in
  every combination.
- **FR3:** `ElectricPropulsionLossFactor` is a new `appsettings.json` key, default 0 (D-DE2).
- **FR4:** At `meCount = 0`, SG capacity > 0 or PTI > 0 produce blocking validation errors; ME
  type and ME capacity are no longer required (D-DE3). An AE-capacity feasibility check mirrors
  the scenario-17 friendly 400 message for plants whose AEs cannot carry propulsion + hotel.
- **FR5:** Battery cascade, Battery Benefit (two worlds) and baseline rules (`count−1` /
  `max(0, count−3)`) operate unchanged at `meCount = 0`.
- **FR6:** (Epic 2, blocked on O-1..O-5) Mission row becomes DP-only; a new "Others" battery
  demand input applies to the remaining relevant modes.
- **FR7:** (Epic 3) Battery allocation table header reads `Variation ± (kW)`; Covered /
  Peak-Shaving labels are clarified per the test23 customer note.

### Non-Functional

- **NFR1:** No measurable performance regression for MeCount ≥ 1 calculations (identical code
  path).
- **NFR2:** New validation messages follow the existing actionable style ("…Consider reducing /
  increasing…") and append without reordering the pinned golden 400 error sequence.

### Compatibility (the contract of this wave)

- **CR1:** Goldens 01–35 byte-for-byte unchanged in Epics 1 and 3. Enforced by construction: every
  Epic-1 behaviour change is gated on `input.MeCount == 0`.
- **CR2:** Profile schema stays v3 for Epics 1 and 3 (`meCount: 0` already passes the importer's
  `typeof === 'number'` checks). Epic 2 bumps to v4 (new field) with import-contract test updates.
- **CR3:** The client's three legacy required fields (`hotelLoad`, `batteryCapacity`,
  `sailInstalled`) remain mandatory in any new scenario JSON.
- **CR4:** Test runs use the locked-bin workaround: `dotnet test KSailCalc.Tests\KSailCalc.Tests.csproj -p:BaseOutputPath=<temp>\`.

## 4. Epic List

| Epic | Title | Status | Order |
|---|---|---|---|
| **E3** | Battery panel cosmetics (± Variation, Covered relabel) | **Ready** — no dependencies | 1st |
| **E1** | Diesel-electric plant (0 main engines) | **Ready for Architect** — D-DE1..D-DE5 confirmed | 2nd |
| **E2** | Battery input rows (Mission DP-only + "Others") | **DONE 2026-08-13** — O-1..O-5 resolved provisionally as D-BI1..5 (revert levers named); implemented as story BI-A | 3rd |

## 5. Epic E3 — Battery panel cosmetics

Single story. No backend, no goldens.

**Story COS-A: Variation ± header and Covered relabel**
- AC1: `battery-contribution-panel.component.html:35` header reads `Variation ± (kW)` (symmetric
  with `Covered ± (kW)`).
- AC2: The Covered/Peak-Shaving presentation is relabeled so a DpReserve-only allocation (Covered
  total 0 while the reserve row shows 800 covered) no longer reads as a broken sum — wording per
  the test23 customer note §"Why the Covered total reads 0".
- AC3: `ng build` clean; 68 client specs green; a DOM spec pins the new headers.
- AC4: No API/golden change (client-only).

## 6. Epic E1 — Diesel-electric plant (0 main engines)

Gate: every behaviour change conditional on `input.MeCount == 0` (CR1). Architect designs the
distribution branch and validation choreography first (see hand-off below).

**Story DE-A: Backend validation opens the 0-ME door**
- AC1: `MeCount = 0` passes validation when AE plant is sufficient; `MeCount < 0` rejected.
- AC2: Conditional requirements per FR4/D-DE3 (SG/PTI blocking errors; ME type/capacity optional
  at 0; AE-capacity feasibility error with actionable text).
- AC3: Golden 400-response order pinned tests stay green (no reordering — NFR2).
- AC4: Full suite green at epic start (baseline run recorded) and after the story.

**Story DE-B: Distribution branch + loss factor**
- AC1: New branch in `Level1CandidateBuilder.TryDistribute` per FR2; the Transit/Maneuvering ME=0
  block bypassed only when `input.MeCount == 0`.
- AC2: `ElectricPropulsionLossFactor` config key (FR3), default 0, covered by a config test
  (pattern of `BatterySettingsConfigurationTests`).
- AC3: Parametric tests: distribution at 0 ME (Transit, DP incl. DpReserve on the AE side, Port);
  90 % AE cap enforced (D-DE4); battery cascade + Benefit two-worlds at 0 ME; PTI gate inert.
- AC4: Characterization test: Level 2 at zero SG returns empty redistribution (logged limitation).
- AC5: Goldens 01–35 byte-for-byte unchanged.

**Story DE-C: Client form and results at 0 ME**
- AC1: ME count accepts 0 (`COUNT.MIN` split — AE keeps min 1); ME type/capacity/SG/PTI fields
  disabled or zeroed with visible affordance when meCount = 0.
- AC2: Backend validation errors for the 0-ME rules surface in the existing error UX.
- AC3: Results panels honest at 0 ME (ME 0 kW rows, no NaN/blank); DOM specs added.
- AC4: Profile import/export round-trips `meCount: 0` (schema stays v3 — CR2).

**Story DE-D: Golden scenarios + documentation**
- AC1: New scenarios (36+): transit-only diesel-electric; DP diesel-electric (doc mode 6
  analogue); diesel-electric with battery; infeasible AE plant (400). JSONs carry the three
  legacy fields (CR3).
- AC2: Calculation cards written per the `calculations/` pattern; README updated; marked
  "characterisation — pending reference verification" until the client confirms one against his
  workbook.
- AC3: Cosmetic "diesel-electric" label decision executed or explicitly deferred (D-DE5).
- AC4: `00-ORIENTATION.md` gains a short "0 ME" note (distribution rule + limitations L2/L3).

## 7. Epic E2 — Battery input rows (BLOCKED)

Provisional story shape, to be re-cut when O-1..O-5 are resolved:

- **BI-A:** Mission row DP-only (`BatteryAllocationService.cs:111-112`) + golden refresh strictly
  per the O-2 unfreeze list; scenario docs 05/06/07 + test23 notes updated.
- **BI-B:** "Others" cascade row (enum, config: coverage factor + priority position, allocation) —
  semantics per O-3/O-4/O-5.
- **BI-C:** Client input field + profile schema v4 + import contract test update (CR2/CR3);
  Mission tooltip rewrite.
- **BI-D:** Scenario/docs wave for the new row.

**Definition of unblocked:** O-1..O-5 answered in `02-decisions-log.md`, including the explicit
golden unfreeze list (O-2) approved by Kamen.

## 8. Hand-offs

- **To Architect (Epic 1):** design the `TryDistribute` 0-ME branch (where the loss factor lives,
  interaction with `PlantLimits`, rejection tally wording), the validation choreography against
  the pinned 400 order, and the config surface. Inputs: `01-analyst-brief.md` §Request 1,
  D-DE1..D-DE5.
- **To PO/SM:** COS-A can be drafted and executed immediately; DE-A..DE-D after architecture
  sign-off; BI-* stay parked.
- **Excel authority (D4):** any numeric disagreement resolves in favour of the client's workbook;
  new diesel-electric figures remain characterisation until client-verified.
