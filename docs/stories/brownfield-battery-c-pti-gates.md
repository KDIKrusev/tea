# Story: Battery Increment C — PTI Propulsion Assist & Battery Feasibility Gate

<!-- Source: docs/battery-feature-analysis/06-architecture-design.md §3.3 increment C, ADR-5; 02-excel-model-analysis.md §1.4; QA gates battery.b/battery.d carried items -->
<!-- Context: Brownfield enhancement to KSailCalc.Api Level 1 + client battery section. Builds on Increments A/B/D (all Done, gates PASS). -->

## Status: Done

## Story

As a **user evaluating a hybrid plant with shaft electric machinery**,
I want **PTI (Power Take-In) modelled: aux power can assist propulsion through the shaft motor,
and the battery's propulsion-side peak shaving must physically fit through available PTI capacity**,
so that **recommended setups match the Excel reference's feasibility rules instead of assuming an
unconstrained electrical path to the propeller**.

## Context Source

- Excel "Optimal Setup" per-combination model (`02-excel-model-analysis.md` §1.4): PTI covers the
  propulsion deficit (capped at Max PTI per ME), PTI power drawn from the aux side is grossed up by
  the 5 % loss factor (I4), and a combination is invalid when the battery's peak-shaving band
  cannot flow through remaining PTI ("Insufficient PTI").
- ADR-5 (as corrected in review R4): `MaxPtiPerEngineKw` **defaults to 0 = PTI not modelled**;
  the client only *suggests* the SG capacity when the battery section is enabled.

## Scope

1. **Input:** `CalculatorInput.MaxPtiPerEngineKw` (double, default 0). Validation: ≥ 0.
2. **Opt-in semantics (zero-regression pivot):** when `MaxPtiPerEngineKw == 0`, behaviour is
   bit-identical to Increment B (bus-level battery simplification, no PTI assist, no gates).
   Entering a positive PTI capacity turns on Excel-fidelity feasibility.
3. **PTI propulsion assist (Level 1):** for combos where `MePowerKw > ME capacity`, the deficit may
   be delivered as PTI, capped at `ActiveMeCount × MaxPtiPerEngineKw`; the aux side carries
   `pti × (1 + PtiLossFactor)` (BatterySettings, 0.05); ME/AE loads recomputed; AE capacity and
   ≤ 90 % checks re-applied on post-assist values. Previously-invalid combos may become valid.
4. **Battery discharge gate:** with an active battery whose **propulsion-side covered band**
   (Σ CoveredBandKw over Propulsion/DpReserve/DpDemand loads) is > 0 and PTI configured, a combo is
   invalid unless `ptiCapacity − ptiUsed ≥ propulsionBand` (the battery must be able to inject its
   shaved band through the shaft motor).
5. **Transparency:** `EngineCombination.PtiPowerKw` + `AvailablePtiKw`; `ValidCombinationDto.ptiKw`
   (nullable) for the baseline table.
6. **Plumbing:** `BatteryL1Adjustment` gains `PropulsionPeakShavingKw` (default 0 — existing
   callers unaffected); `CalculatorService.ToAdjustment` computes it;
   `Level1OptimizationService` gains `IOptions<BatterySettings>` (loss factor).
7. **Client:** PTI capacity field in the battery section (visible when battery enabled), prefilled
   with the SG capacity per engine on enable (editable, clearable); flows as `maxPtiPerEngineKw`;
   profile validation accepts the optional number.

**Documented deviations from the Excel (aggregate-plant approximations):**
- **D-C1 — charge-side (PTO) gate deferred.** The Excel also gates on PTO headroom for recharging.
  In this app the SG runs at `min(hotel, capacity)` by construction, so a strict PTO gate would
  invalidate almost every SG vessel (false negatives); bus-side charging from gensets is physically
  available. The PTO gate is deferred to a per-machine model.
- **D-C2 — direction exclusivity relaxed.** Excel machines are individually PTI xor PTO; the app
  aggregates identical machines, so a combo may show both SG (PTO) power and PTI assist. Justified
  for multi-ME plants; slightly optimistic for single-ME plants.

## Acceptance Criteria

1. **Zero regression:** `MaxPtiPerEngineKw` absent/0 ⇒ full existing suite passes unchanged, and a
   direct test shows identical Level 1 results with and without the new parameter at 0.
2. **PTI enables combos:** a plant whose propulsion slightly exceeds ME capacity has no valid
   ME-only combos today; with sufficient `MaxPtiPerEngineKw` the combo becomes valid,
   `PtiPowerKw == deficit`, and `AePowerKw` includes `deficit × 1.05`.
3. **Deficit beyond PTI stays invalid:** deficit > ptiCapacity ⇒ combo still invalid.
4. **Discharge gate:** active Transit battery with propulsion-side band B and PTI configured:
   combos with `ptiCapacity − ptiUsed < B` are excluded; with ample headroom they remain.
5. **AE feasibility after assist:** PTI aux load pushing AE above capacity or 90 % load ⇒ combo
   invalid (test).
6. **Client:** PTI field appears when battery enabled, prefills SG capacity, payload carries
   `maxPtiPerEngineKw`; `ng build` clean.
7. Full backend suite green.

## Dev Technical Guidance

- Insert the assist between `DistributeLoad` and the existing AE/ME capacity checks in
  `Level1OptimizationService.FindOptimalCombinationAsync`; recompute `MeLoadPercent`/`AeLoadPercent`
  via `CalculationHelpers.LoadPercent`.
- `TestServiceFactory` constructs `Level1OptimizationService` — add the options argument there
  (and any direct construction sites; `EngineFuelCo2Tests` reuses factory services, should be safe).
- Propulsion-side band mapping mirrors `ToAdjustment`'s side split (thrust loads → propulsion).
- Client: flat control `batteryMaxPtiKw` in the battery section (consistent with Increment D's
  flat-controls deviation); prefill from `sgCapacityPerEngine` only when empty.

## Risk Assessment

- **Primary:** changed combination space shifts baseline indices → mitigated: only when the user
  explicitly enters PTI capacity; baseline remains user-selectable (D1).
- **Secondary:** ME load at exactly 100 % after assist (ME pinned to capacity) hits SFOC curve
  endpoints — covered by existing interpolation extrapolation rules.
- **Rollback:** revert Level1/CalculatorService/model changes; A/B/D unaffected when PTI unset.

## Tasks / Subtasks

- [x] Task 1: Models (`MaxPtiPerEngineKw`, `EngineCombination` fields, `BatteryL1Adjustment`,
      `ValidCombinationDto.ptiKw`) + validation rule
- [x] Task 2: Level 1 — PTI assist + discharge gate + BatterySettings injection
- [x] Task 3: `CalculatorService.ToAdjustment` propulsion band; `LevelDetails` DTO mapping
- [x] Task 4: Backend tests (AC1–AC5, AC7 — 9 new tests; full suite 163/163)
- [x] Task 5: Client — PTI field + SG prefill + types + profile validation; `ng build` clean (7.0 s)
- [x] Task 6: Story record update

## Dev Agent Record

### Agent Model Used

Claude Fable 5 (claude-fable-5)

### Completion Notes

- PTI is **opt-in** per ADR-5: `MaxPtiPerEngineKw` default 0 keeps every existing scenario
  bit-identical (AC1 pinned by test + untouched 154-test suite).
- Assist implemented as `TryApplyPtiAssist` between `DistributeLoad` and the capacity checks:
  deficit ≤ `ActiveMeCount × MaxPti` moves to the aux side × (1 + `PtiLossFactor`), ME pinned to
  capacity, loads recomputed; aux infeasibility (capacity or later 90 % check) rejects the combo.
- Discharge gate uses the **propulsion-side covered band** (Σ CoveredBandKw over thrust loads),
  computed in `CalculatorService.ToAdjustment` and carried by the extended `BatteryL1Adjustment`
  (third positional param with default — existing callers untouched).
- `Level1OptimizationService` takes `IOptions<BatterySettings>?` with a null default → DI supplies
  it in production; `TestServiceFactory` needed **no change** (falls back to code defaults,
  PtiLossFactor 0.05).
- Deviations D-C1 (PTO charge gate deferred — bus charging) and D-C2 (direction exclusivity
  relaxed in the aggregate plant) implemented exactly as scoped in the story.
- Client: `batteryMaxPtiKw` flat control in the battery section; on battery enable the field is
  prefilled with `sgCapacityPerEngine` (only when empty); payload sends `maxPtiPerEngineKw` only
  when set; profile validation accepts the optional number (still schema v3 — additive optional).

### Debug Log References

- One test-selection fix on first run: the deficit plant yields **two** PTI combos (AE 2 and AE 3);
  `Single` filter narrowed to `ActiveAeCount == 3`.
- Suite: **163/163 green** (154 prior + 9 new), 239 ms. `ng build` clean, 7.0 s.

### File List

New:
- `KSailCalc.Tests/Services/Level1PtiTests.cs` (9 tests)

Modified (backend):
- `Models/CalculatorInput.cs` (`MaxPtiPerEngineKw`)
- `Models/EngineCombination.cs` (`PtiPowerKw`, `AvailablePtiKw`)
- `Models/BatteryAllocation.cs` (`BatteryL1Adjustment.PropulsionPeakShavingKw`)
- `Models/LevelDetails.cs` (`ValidCombinationDto.PtiKw` + mapping)
- `Services/Level1OptimizationService.cs` (BatterySettings injection, `TryApplyPtiAssist`, gate)
- `Services/CalculatorService.cs` (`ToAdjustment` propulsion band)
- `Services/ValidationService.cs` (MaxPti ≥ 0)

Modified (client):
- `cl/src/app/calculations/calculator.types.ts` (`maxPtiPerEngineKw`, `ValidCombinationDto.ptiKw`)
- `cl/src/app/core/profile.service.ts` (optional number in validation)
- `cl/src/app/features/vessel-input/vessel-input-form/vessel-input-form.component.ts`
  (control, mapping, restore)
- `cl/src/app/features/vessel-input/vessel-input-form/battery-config-section/battery-config-section.component.ts` (getter, SG prefill on enable)
- `cl/src/app/features/vessel-input/vessel-input-form/battery-config-section/battery-config-section.component.html` (PTI field)

### Change Log

| Date | Change |
|---|---|
| 2026-07-13 | Increment C implemented: opt-in PTI propulsion assist (5% loss to aux side), battery discharge gate on propulsion-side band, combo/DTO transparency fields, client PTI input with SG prefill. Backend 163/163; ng build clean. Status → Ready for Review. |

## QA Results

### Review Date: 2026-07-13

### Reviewed By: Quinn (Test Architect)

### Code Quality Assessment

Disciplined, opt-in implementation. The ADR-5 pivot (MaxPti = 0 ⇒ PTI not modelled) is enforced at
every new branch, which makes the zero-regression argument structural — confirmed by the untouched
154 prior tests plus a dedicated AC1 test. `TryApplyPtiAssist` sits at the right point in the loop
(post-DistributeLoad, pre-capacity-checks) and correctly re-applies the AE 90 % check on
post-assist loads. Deviations D-C1/D-C2 are implemented exactly as the story scoped and documented
them. **No defects found.** One trace gap closed during review; one low-severity UX observation
left open for the PO.

### Refactoring Performed

- **File**: `KSailCalc.Tests/Services/Level1PtiTests.cs`
  - **Change**: added two end-to-end tests through `CalculateAllVariantsAsync`
    (`Calculate_ExcelBatteryWithAmplePti_Succeeds…`, `Calculate_ExcelBatteryWithTinyPti…Throws`).
  - **Why**: `ToAdjustment`'s `PropulsionPeakShavingKw` computation (the wiring that feeds the
    gate) was untested — all gate tests injected the band manually into Level 1.
  - **How**: Excel scenario (band 200.6 kW) with PTI 1000 kW (passes) and 100 kW (every combo
    gated ⇒ throws), pinning the allocation→gate path.

### Compliance Check

- Coding Standards: ✓ (region conventions, tolerance idiom, optional-DI pattern documented)
- Project Structure: ✓
- Testing Strategy: ✓ (unit for assist mechanics, e2e for wiring; client untested per project
  convention — debt already recorded in gate battery.d)
- All ACs Met: ✓ AC1–AC7 (AC6 client payload verified by inspection + build)

### Improvements Checklist

- [x] Trace gap closed: allocation→gate wiring e2e tests
- [ ] **QA-C-1 (PO decision):** an easily user-triggered infeasible state (small PTI + large
      battery band, or PTI aux overload) surfaces as `InvalidOperationException` → HTTP 500 →
      generic "Calculation failed" in the UI. Recommend converting "no valid combinations" into a
      structured 400 with a human-readable reason ("Insufficient PTI capacity for battery peak
      shaving — increase PTI or reduce battery power") in a follow-up story.
- [ ] **Observation:** DP thrust is modelled as shaft propulsion, so a future DP battery band
      (currently 0 — DP variation factors default to 0) would demand PTI in DP mode; revisit when
      the DP redundancy input lands (story A "Missing Information").
- [ ] **Accepted tolerance change:** ME capacity check gained a `+0.001` FP tolerance (was strict
      `>`); behaviourally invisible in practice, full suite green.

### Security Review

No concerns — one numeric input, validated ≥ 0; no new I/O.

### Performance Considerations

Negligible — O(1) arithmetic per combination; suite 170 ms.

### Files Modified During Review

- `KSailCalc.Tests/Services/Level1PtiTests.cs` (2 e2e tests)

(Dev: fold into File List on next touch.)

### Gate Status

Gate: **PASS** → docs/qa/gates/battery.c-pti-gates.yml
(Full suite after review: **165/165 green**.)

### Recommended Status

✓ Ready for Done — carry QA-C-1 as a follow-up story candidate.
(Story owner decides final status.)
