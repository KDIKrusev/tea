# Story: Battery Increment F — Excel Load Inputs (DP Redundancy & Mission Heavy-Consumer Max)

<!-- Source: decision D4 (Excel is final authority); 02-excel-model-analysis.md §1.2-1.3; workbook Load Demands rows 5 & 7 -->
<!-- Context: Brownfield — wires the two remaining Excel inputs into the allocation. Builds on A–E (all Done/PASS). -->

## Status: Done

## Story

As a **user modelling DP or mission (crane) operations with a battery**,
I want **to enter the DP class redundancy requirement and the mission heavy-consumer maximum, as
the Excel model does**,
so that **the battery allocation covers those loads per the workbook instead of treating them as
zero**.

## Excel semantics (normative)

- **DpReserve row (Load Demands R5):** `E5` = DP redundancy demand (input O2); RESERVE ⇒ coverage
  100 %, `H = E×(1+vf)` (vf default 0). **Not part of Σavg demand** (O7 sums O3:O6) — it is a pure
  reserve requirement: covered kW need no genset backing; uncovered kW (L) add to demand.
- **Mission row (R7):** average mission load is already inside the app's "Hotel/Mission Power"
  input (no new avg — avoids double counting). The NEW information is the variation:
  `G7 = IF(E7>0, I3, 0)` ⇒ `H = MissionHeavyConsumerMaxKw` **as-is** when present (the full heavy
  consumer can start at any moment), not avg×factor. PEAK SHAVING ⇒ coverage 50 %.

## Scope

1. `CalculatorInput`: `DpRedundancyRequirementKw?` (DP modes) and `MissionHeavyConsumerMaxKw?`
   (Transit/DP mission ops). Defaults 0/absent ⇒ today's behaviour (zero-regression).
2. `BatteryAllocationService`: load mapping returns an explicit variation override for Mission
   (H = heavy-consumer max) and the redundancy value for DpReserve; cascade unchanged.
3. Validation: both ≥ 0; DP redundancy only meaningful with `DpEnabled` (warning otherwise).
4. Client: two fields in the battery section (visible when battery enabled; DP redundancy shown
   only when DP mode available), payload + profile plumbing.
5. Tests with hand-derived numbers (below).

## Acceptance Criteria (hand-derived)

1. **Zero regression:** both inputs absent ⇒ full suite unchanged (rows stay 0).
2. **DP redundancy (RESERVE):** DP battery 500 kW, redundancy 400, DP hotel 1500:
   DpReserve: H=**400**, I=**400**, J=**400** (D=1), L=**0**; remaining 100 → Hotel: H=30, I=**30**,
   J=**1.5**, L=**28.5**. ΣI=**430**, PS band=**1.5** (J of RESERVE rows is covered reserve, not PS),
   SR=**28.5**.
3. **Priority order bites:** same but battery 300: DpReserve I=**300**, J=300, L=**100**;
   Hotel I=0, L=**30**. SR=**130**.
4. **Mission variation:** Transit battery 1260, heavy-consumer max 3000 (Excel I3), prop 11463,
   hotel 3800: Mission first ⇒ H=**3000**, I=**1260**, J=**630**, L=**2370**, rem 0;
   Propulsion L=**573.15**; Hotel L=**76**. PS=**630**, SR=**3019.15**.
5. **Mission absent ⇒ H=0** (explicit): heavy-max null/0 ⇒ Mission row variation 0 (today).
6. Validation: negatives ⇒ errors; redundancy without DpEnabled ⇒ warning.
7. Client: fields render, payload carries values, `ng build` clean; profile round-trip.

## Tasks / Subtasks

- [x] Task 1: Input fields + validation (2 errors + 1 advisory warning)
- [x] Task 2: Allocation mapping (`GetLoadInputs` returns (avg, variationOverride); Mission
      H = heavy-max as-is; DpReserve avg = redundancy)
- [x] Task 3: Backend tests (7 new — AC1–AC6; full suite **203/203 green**)
- [x] Task 4: Client fields (Mission always when battery on; DP redundancy only when DP mode
      available) + payload + profile plumbing; `ng build` clean
- [x] Task 5: Story record + test-design doc addendum

## Risk / Rollback

- Gated by the new optional inputs (0 ⇒ identical behaviour); rollback = revert mapping lines.

## Dev Agent Record

### Agent Model Used

Claude Fable 5 (claude-fable-5)

### Completion Notes

- Excel semantics implemented literally: DpReserve average = redundancy requirement (RESERVE ⇒
  H = full value, covered 1:1, first priority — AC2 pins 400→400/400/0); Mission uses an explicit
  **variation override** (H = heavy-consumer max as-is per `G7 = IF(E7>0, I3, 0)`), average stays 0
  because the mission average already lives in the Hotel/Mission input (no double counting).
- Verified priority behaviour: mission max 3000 with budget 1260 consumes the whole budget before
  Propulsion/Hotel (AC4: PS 630 / SR 3019.15).
- `PeakShavingBandKw` correctly excludes RESERVE-row coverage (J of DpReserve is covered reserve,
  not a ± band) — pinned by AC2.
- Zero-regression: absent inputs reproduce scenario A7 totals exactly (AC5 test).

### File List

New: `KSailCalc.Tests/Services/BatteryExcelLoadInputTests.cs` (7 tests)
Modified: `Models/CalculatorInput.cs`, `Services/BatteryAllocationService.cs` (GetLoadInputs +
variation override), `Services/ValidationService.cs`,
`cl/src/app/calculations/calculator.types.ts`, `cl/src/app/core/profile.service.ts`,
`cl/.../vessel-input-form.component.ts`, `cl/.../battery-config-section.component.{ts,html}`

### Change Log

| Date | Change |
|---|---|
| 2026-07-13 | Increment F: DP redundancy + mission heavy-consumer max wired end-to-end per Excel (D4). 203/203 green; ng build clean. Status → Ready for Review. |

## QA Results

### Review Date: 2026-07-13

### Reviewed By: Quinn (Test Architect)

### Code Quality Assessment

Excel-literal implementation with the two semantic subtleties handled correctly: (1) DP redundancy
is a pure RESERVE requirement — average NOT added to demand (matches O7 summing from O3), covered
1:1 at top priority; (2) Mission's variation is an explicit override (full I3 value), with the
average deliberately left at 0 because it already lives in the Hotel/Mission input — the
double-counting trap the Excel formula `G7=IF(E7>0,I3,0)` implies was avoided and documented in
the code. `GetLoadInputs` refactor is clean (tuple return, exhaustive switch). **No defects.**

The initial 7 allocation/validation tests left the pipeline effects uncovered; the gap was closed
in-review with Family H (see test-design addendum 2): H1 benefit-from-covered-redundancy, H2
redundancy band × PTI discharge gate (cross-increment F×C interaction), H3 mission reserve landing
on the hotel side + fully absorbing DRC variation (F×B×E triple interaction). All first-run green.

### Compliance Check

- Coding Standards / Structure / Testing Strategy: ✓ · All ACs Met: ✓ (AC1–AC7 + H1–H3)

### Improvements Checklist

- [ ] UI polish (future): DP Redundancy field hides when DP mode is off — consider keeping the
      entered value on re-enable (currently preserved in the form control; verify UX in smoke test)

### Gate Status

Gate: **PASS** → docs/qa/gates/battery.f-excel-load-inputs.yml
(Full suite: **206/206 green**; ng build clean.)

### Recommended Status

✓ Ready for Done.
