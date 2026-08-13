# Client Requests 2026-08 — Analyst Brief

**Analyst:** Mary (BMAD) · **Date:** 2026-08-13 · **Status:** Complete — decisions D-DE1..D-DE5 confirmed by Kamen 2026-08-13 (see `02-decisions-log.md`)

The client (ship engineer, owner of the reference Excel workbooks) sent three requests. Each is
classified below as bug / misunderstanding / new requirement, with the exact code that governs
today's behaviour and the agreed direction.

---

## Request 1 — Support 0 main engines (diesel-electric propulsion)

**Classification: new requirement.** Not a small change — a new plant model — but isolable behind a
single gate.

### The client's supporting material

`docs/pics/1000005188..198.jpg` — Wärtsilä "Hybride Propulsion System" presentation for Aker
AH012/AH04 (anchor-handling vessel, "Modes for Vega from SAT Protocol", 11 pages). Plant: 2 shafts,
each ME 16V32 7 680 kW + CPP, shaft machine 3 500 kW (SG and PTI in one), 4 AE × 2 495 kW on a
common MSB. The nine modes reduce to three families:

| Family | Doc modes | Propulsion carrier |
|---|---|---|
| Diesel-mechanic | 1, 3, 5a, 5b | ME on the shaft; SG in or out |
| Hybrid boost | 2, 4, 7, 8 | ME + PTI on top (AEs feed the shaft) |
| **Diesel-electric** | **6 (DP nice weather), 9 (slow steaming 12 kn)** | **ME 0 kW/0 rpm; AE → MSB → converter → PTI → CPP, 0–2 000 kW @ 45–80 rpm** |

Key observation: in this document diesel-electric is a **mode of an ME-equipped vessel** (MEs
installed but off). The client's request is the stronger and simpler-to-isolate case: **vessels
with no MEs installed at all** — propulsion by AEs + thrusters. Decision D-DE1 scopes the epic to
the latter; "MEs off per mode" is a possible future epic (the Excel already hints at it — the
N-column "electric drive of the non-running shaft" cited in `Level1PtiAssist.cs`).

### What blocks 0 ME today (four independent barriers)

| # | Location | Behaviour |
|---|---|---|
| 1 | `Services/Validation/ValidationService.cs:66-67` | `MeCount < 1` → error "Number of main engines must be at least 1" |
| 2 | `Services/Calculation/Level1CandidateBuilder.cs:59-60` | ME=0 combinations rejected for Transit/Maneuvering |
| 3 | `Services/Calculation/Level1OptimizationService.cs:137-141` | ME=0 with nonzero ME power → structural rejection (this also blocks DP, whose thrust is modelled as ME shaft load today) |
| 4 | Client: `vessel-form.schema.ts:22` + `defaults.constants.ts:19` (`COUNT.MIN` shared ME/AE) + `engine-config-section.component.html:96` (`min="1"`) | form validation floor of 1 |

Deeper: there is **no "propulsion on AE" path anywhere**. `Level1CandidateBuilder.TryDistribute`
(lines 89–116) puts propulsion + SG on the ME and hotel on SG+AE only ("ME has no PTO").

### Why isolation is realistic — what does NOT change

The battery cascade, the two Benefit worlds and the baseline rules need **no change**. The cascade
splits the world into a propulsion side and a hotel side (`BatteryModeAdapter.cs:30-46`); only the
final distribution step decides which machinery physically carries each pile. At 0 ME both piles
land on the AEs; everything upstream is neutral. Also verified as already safe:

- `PowerDemandsBuilder.cs:45-49` reads per-combination values → ME 0 kW shown honestly; load-%
  guards (`ActiveMeCount > 0`) avoid division by zero.
- Client baseline panel guards `activeMeCount > 0` (`baseline-panel.component.ts:59`).
- Profile importer checks `typeof === 'number'`, not truthiness (`profile.service.ts:213-217`) —
  `meCount: 0` imports fine.
- PTI: `ptiCapacity = MeCount × MaxPti` (`Level1PtiAssist.cs:34`) self-zeroes; the battery PTI
  discharge gate is guarded by `MaxPtiPerEngineKw > 0` (`Level1OptimizationService.cs:146`) →
  inert; the battery propulsion band flows at bus level, consistent with ADR-5.
- SFOC: AE curve is read from `AeLoadPercent`, which will already include the propulsion load.

### The change inventory (single gate: `input.MeCount == 0`)

1. `ValidationService.cs:66-67` — allow 0. **Caution:** golden 400-responses pin the error-list
   order (`ValidationService.cs:19-22`); new conditional rules must append, not reorder.
2. `ValidationService.cs:63-64`, `:128-129` — `MeCapacityPerEngine > 0` and `MainEngineTypeId > 0`
   required only when `MeCount ≥ 1`.
3. ValidationService, new at MeCount = 0: SG capacity > 0 → blocking error; PTI > 0 → blocking
   error (no shaft); new capacity check "AE capacity must carry propulsion + hotel" mirroring the
   scenario-17 message so the friendly 400 text is preserved.
4. `Level1CandidateBuilder.cs:59-60` — bypass the Transit/Maneuvering ME=0 block when
   `input.MeCount == 0`.
5. `Level1CandidateBuilder.cs:89-116` — new distribution branch at MeCount = 0:
   `aePower = hotel + propulsion × (1 + ElectricPropulsionLossFactor)`, `MePowerKw = SgPowerKw = 0`,
   coverage checked against AE capacity. The 90 % `MaxAuxLoadFraction` cap now applies to the whole
   electric load (kept deliberately — D-DE4): plant must have AE capacity ≥ (prop + hotel)/0.9.
6. Client: split `COUNT.MIN` (ME min 0, AE min 1); disable/zero ME type, ME capacity, SG, PTI
   fields when meCount = 0; optional cosmetic label (D-DE5, deferred).

### Verification items recorded for the design/story phase

- Level 2 at zero SG is expected to return an empty redistribution — needs a characterization
  test, not belief (lesson of Finding 5).
- Level 3 DRC stays hotel-only (no Excel authority for diesel-electric DRC) — documented limitation.

### Test & golden strategy

1. Epic step 0: full run of the 441 tests with the redirected `BaseOutputPath` — green baseline.
2. Goldens 01–35 stay untouched **by construction** (the MeCount == 0 gate), verified per story.
3. New parametric tests: distribution at 0 ME, validation rules, battery on diesel-electric
   (cascade + Benefit), infeasible AE plant (new 400 text).
4. New golden scenarios (36+): pure transit diesel-electric; DP diesel-electric (doc mode 6
   analogue — DpReserve now lands on the AE side); with battery; characterization status until the
   client verifies at least one against his Excel (then promoted to proof, like 01–18).

---

## Request 2 — Mission/DP-redundancy rows DP-only + new "Others" battery input

**Classification: requirement change (not a bug in our code) + new requirement. BLOCKED on open
product decisions.**

- DP redundancy is **already DP-only** (`BatteryAllocationService.cs:108`). No change.
- The Mission row today exists in **Transit or DP** (`BatteryAllocationService.cs:111-112`).
  Client: it must be DP-only.

⚠️ **Logged conflict:** the mode-matrix comment states it mirrors "the Excel sheet's own shape",
and scenarios 05/06/07 (crane in Transit) were verified against the reference workbook (README,
01–18). Either the Excel actually has Mission only in DP and our port over-generalized (a bug per
decision D4), or the client is now changing his model. Removing Mission from Transit **changes the
numbers** of scenarios 05, 06, 07 and their frozen golden snapshots — `GOLDEN_UPDATE=1` is
forbidden, so this cannot proceed without an explicit product decision to unfreeze those goldens.

**Open decisions (see `02-decisions-log.md` §Open):** Others-row semantics (full value per relevant
mode vs split), function (PeakShaving vs Reserve), coverage factor, priority position, plant side,
whether "remaining modes" extends beyond Transit/Port (`ValidationService.cs:14-15` restricts
battery modes to Transit/DP/Port per D4), golden unfreeze list. Side effects when resolved:
`BatteryLoadType` enum + client `functionLabel`, profile schema v3 → v4 + import contract
(`ScenarioImportContractTests`), Mission field tooltip (currently describes Transit behaviour).

---

## Request 3 — "±" on the Variation column header

**Classification: trivial cosmetics, client-only. Ready now.**

`battery-contribution-panel.component.html:35` — `Variation (kW)` → `Variation ± (kW)` (symmetry
with `Covered ± (kW)` on line 37). Goldens unaffected (API snapshots); check the 68 client DOM
specs don't pin the header. Bundle the long-promised **Covered/Peak-Shaving relabel** (the
"Covered reads 0 at DpReserve 800" finding, promised to the client in the test23 note) into the
same story.

---

## Recommended epic order

1. **Epic 3** (cosmetics) — no dependencies, immediate.
2. **Epic 1** (diesel-electric 0 ME) — decisions confirmed, ready for Architect.
3. **Epic 2** (battery input rows) — blocked until its open decisions are made.
