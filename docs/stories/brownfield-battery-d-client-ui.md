# Story: Battery Increment D — Client UI (Battery Section, Contribution Panel, Profile v3)

<!-- Source: docs/battery-feature-analysis/06-architecture-design.md §4 + QA gate battery.b (carried items) + task sketch (Screenshot 2026-07-13) -->
<!-- Context: Brownfield enhancement to the Angular 18 client (cl/) — consumes Increment B's API contract -->

## Status: Done

<!-- QA Gate: PASS (docs/qa/gates/battery.d-client-ui.yml) · Owner approved 2026-07-13 -->


## Story

As a **user of the Energy Savings Calculator**,
I want **to configure a battery (capacity, power, relevant modes) in the input form and see its
computed functions (Spinning Reserve / Peak Shaving) and benefit in the results**,
so that **battery-equipped vessels can be evaluated end-to-end in the UI, per the requester's sketch**.

## Context Source

- Source Documents: architecture design §4 (form section, results, profile v3); requester's UI
  sketch (battery config box with Capacity/Power, Functions, Relevant Modes Transit/DP/Port);
  QA gate battery.b carried items (zero-hours hint, per-mode display of SR/PS, power-demands copy).
- Enhancement Type: New form section + results panel + profile schema migration; TS types mirror
  the Increment B contract.
- Existing System Impact: `vessel-input-form` (form group + mapping), `calculator-page` (results
  accordion), `profile.service` (schema v2→v3), `calculator.types.ts`.

## Scope

1. **Types** (`calculations/calculator.types.ts`): `BatteryConfigurationInput` on `CalculatorInput`
   (`capacityKwh`, `powerKw`, `relevantModes: OperationalModeName[]` — strings, backend uses
   JsonStringEnumConverter); `BatteryDetails` (+ `BatteryModeAllocation`, `BatteryLoadAllocation`)
   on `AllVariantsCalculationResult`.
2. **Form section** `battery-config-section` (standalone, OnPush, `[parentForm]` pattern like
   `weather-input-section`): per the sketch —
   - "Enable Battery" toggle; when on: Capacity (kWh) and Power (kW) inputs;
   - Functions block: **read-only computed** Spinning Reserve / Peak Shaving kW from the last
     calculation's `batteryDetails` (dash before first calc);
   - Relevant Modes checkboxes: Transit / DP / Port; DP checkbox disabled (and cleared) when DP
     mode is not enabled;
   - hint when battery enabled but result has `batteryDetails == null` (relevant modes have zero
     hours — QA carry-over).
3. **Form wiring** (`vessel-input-form.component`): nested `battery` group; include in
   `buildCalculatorInput` (send `battery: null` when disabled/power 0 — AC zero-regression);
   restore from profile; reset behaviour consistent with other sections.
4. **Results**: "Battery Contribution" expansion panel on `calculator-page` (rendered only when
   `batteryDetails` present): Power/Capacity, computed SR/PS, benefit (ton/yr + $/yr), per-mode
   allocation table (load, function, variation, battery used, covered band, uncovered reserve);
   note in panel copy that plant demand includes the uncovered spinning reserve (QA carry-over).
5. **Profile v3** (`profile.service`, `profile.types`): `PROFILE_SCHEMA_VERSION` 2→3; v2 profiles
   migrate on load (`battery` defaults: disabled, `capacityKwh` from legacy `batteryCapacity` or 0);
   draft autosave includes battery; export writes v3; validation accepts v2 (migrated) and v3.

**Out of scope:** PTI input fields (Increment C), report changes beyond none (report untouched this
increment), charts, baseline-panel changes (hint about third-highest can ride in the battery panel).

## Acceptance Criteria

1. **Zero regression:** battery disabled (default) ⇒ request payload contains `battery: null`
   (or omitted) and legacy `batteryCapacity: 0` — identical to today; all results render as before;
   no Battery Contribution panel.
2. Battery enabled with Power/Capacity + Transit ⇒ request carries
   `battery: { capacityKwh, powerKw, relevantModes: ["Transit"] }`; response `batteryDetails`
   renders: SR/PS values in BOTH the form's Functions block and the results panel; benefit shown.
3. DP checkbox: disabled + unchecked when the DP mode is off; enabling DP mode re-enables it.
4. Battery enabled but `batteryDetails` null in response ⇒ hint shown, no panel.
5. Profile v3: saving stores the battery group; loading a **v2 profile** works (battery defaults,
   no error); draft restore round-trips the battery group.
6. `ng build` succeeds; `ng lint` clean for touched files (if lint configured).

## Dev Technical Guidance

- Follow `weather-input-section` for the toggle-gated section pattern and
  `additional-config-section` for numeric fields (`app-form-input-field` shared component).
- Form change flow: `formChanged` → `calculator-page.onFormChange` (500 ms debounce) → results
  held in component fields; pass `batteryDetails` down to the form section via an `@Input()` (the
  page already holds `_allVariantsResult`).
- Mode names over the wire are **strings** ("Transit", "DP", "Port") — backend
  `OperationalMode` has `JsonStringEnumConverter`.
- Profile validation lives in `profile.service.ts` (`validateProfile`); keep `batteryCapacity`
  legacy field for v2 compatibility.
- Keep Material + Tailwind idiom of sibling components; OnPush where the pattern uses it.

## Risk Assessment

- **Primary Risk:** profile schema break for existing users' saved profiles/drafts.
  **Mitigation:** explicit v2→v3 migration on load + AC5 test path; version accepted range.
- **Secondary:** payload shape drift vs backend (`relevantModes` casing/values) — pin with a
  build-time type union and an e2e-ish mapping test if a test harness exists (else manual verify).
- **Rollback:** revert touched client files; backend unaffected.

## Tasks / Subtasks

- [x] Task 1: Types (`calculator.types.ts`)
- [x] Task 2: `battery-config-section` component (ts/html/css) per sketch
- [x] Task 3: `vessel-input-form` wiring (controls, mapping, restore, edit-tracker)
- [x] Task 4: `calculator-page` — pass batteryDetails to section + Battery Contribution panel
- [x] Task 5: Profile v3 migration (service + types)
- [x] Task 6: Build verification (`ng build` — success, 23.6 s), story record update

## Dev Agent Record

### Agent Model Used

Claude Fable 5 (claude-fable-5)

### Completion Notes

- Battery section per sketch: enable toggle → Capacity (kWh) / Power (kW), read-only computed
  **Functions** (Spinning Reserve / Peak Shaving from `batteryDetails`, "—" before first result),
  Relevant Modes checkboxes (Transit / DP / Port). DP checkbox is disabled+cleared when DP hours
  are 0 (subscribes to `dpHours` valueChanges) — backend rejects DP battery without DP mode.
- **Zero-regression (AC1):** `buildBatteryInput` returns `null` unless enabled AND power > 0 —
  the request is contract-identical to today for existing users; the legacy `batteryCapacity`
  field keeps flowing unchanged.
- QA carry-overs from gate battery.b addressed: "no effect" hint when battery is enabled but
  `batteryDetails` is null after a calculation (zero-hours relevant modes); Battery Contribution
  panel shows **per-mode** allocation tables (not just cross-mode sums); panel copy + tooltip
  explain that plant demand includes the uncovered spinning reserve; the third-highest default
  baseline is noted in the panel footer.
- **Deviation (documented):** the story suggested a *nested* `battery` form group, but the
  existing form is a single flat `FormGroup` (edit-tracker, patchValue and profile restore all key
  on flat control names). Implemented as flat controls (`batteryEnabled`, `batteryPowerKw`,
  `batteryCapacityKwh`, `batteryModeTransit/Dp/Port`) for consistency.
- Profile v3: `PROFILE_SCHEMA_VERSION` 2→3; no explicit migration needed — `battery` is optional
  on `CalculatorInput`, so v2 profiles/drafts validate and load as "battery disabled";
  `isValidBattery` guards imported v3 profiles; profile save/export path is unchanged (it
  serializes `CalculatorInput`, which now includes `battery`).
- `ng lint` not run (no lint config verified in project scripts); `ng build` clean.

### Debug Log References

- `cl/node_modules` was absent — ran `npm ci` (1057 packages) before `ng build`.
- Build: development configuration, success in 23.6 s, no compile errors or template diagnostics.

### File List

New:
- `cl/src/app/features/vessel-input/vessel-input-form/battery-config-section/battery-config-section.component.ts`
- `cl/src/app/features/vessel-input/vessel-input-form/battery-config-section/battery-config-section.component.html`
- `cl/src/app/features/vessel-input/vessel-input-form/battery-config-section/battery-config-section.component.css`

Modified:
- `cl/src/app/calculations/calculator.types.ts` (battery input/response types)
- `cl/src/app/core/profile.types.ts` (schema v3 + comment)
- `cl/src/app/core/profile.service.ts` (battery validation in profile input check)
- `cl/src/app/features/vessel-input/vessel-input-form/vessel-input-form.component.ts`
  (controls, `buildBatteryInput`, profile restore, inputs pass-through, draft version)
- `cl/src/app/features/vessel-input/vessel-input-form/vessel-input-form.component.html`
  (battery section placement after operational modes)
- `cl/src/app/features/calculator-page/calculator-page.component.ts` (batteryDetails/hasResults getters)
- `cl/src/app/features/calculator-page/calculator-page.component.html`
  (inputs to form + Battery Contribution panel)
- `cl/src/app/features/calculator-page/calculator-page.component.css` (battery panel styles)

### Change Log

| Date | Change |
|---|---|
| 2026-07-13 | Increment D implemented: battery form section (sketch layout, computed SR/PS, mode checkboxes with DP gating), Battery Contribution results panel with per-mode allocation tables and benefit line, profile schema v3. `ng build` clean. Status → Ready for Review. |

## QA Results

### Review Date: 2026-07-13

### Reviewed By: Quinn (Test Architect)

### Code Quality Assessment

Faithful implementation of the sketch with all three QA carry-overs from gate battery.b addressed
(no-effect hint, per-mode allocation tables, demand-includes-reserve copy). Types mirror the
Increment B contract exactly; the zero-regression payload rule (`battery: null` unless enabled
with positive power) is correctly centralized in `buildBatteryInput`. Two genuine defects found
and fixed during review; one project-wide test-culture gap noted as debt.

**Defects found (both fixed in review):**

1. **QA-D-1 (medium) — `[disabled]` attribute binding on a reactive-form checkbox.** Angular
   reactive forms ignore `[disabled]` on `formControlName` directives (and log a runtime warning);
   the DP relevant-mode checkbox would not reliably disable. Fixed by driving state through
   `control.enable()/disable({ emitEvent: false })` in `updateDpAvailability`, moving the tooltip
   to a wrapper `<span>` (tooltips don't fire on disabled elements).
2. **QA-D-2 (low) — stale DP availability after profile restore.** `applyProfileInputValues`
   patches with `emitEvent: false`, so the section's `dpHours` subscription never fires and
   `isDpModeAvailable` kept its pre-load value. Fixed with a public `refreshDpAvailability()`
   called from the form via `@ViewChild` after profile values are applied (no extra recalculation,
   consistent with the form's existing ViewChild orchestration pattern).

Also generalized the no-effect hint copy (it appears both for zero-hours modes and for no modes
selected).

### Refactoring Performed

- **File**: `battery-config-section.component.ts`
  - **Change**: control-driven enable/disable + `refreshDpAvailability()`.
  - **Why/How**: see QA-D-1 / QA-D-2 above.
- **File**: `battery-config-section.component.html`
  - **Change**: removed `[disabled]` binding; tooltip wrapper span; generalized hint copy.
- **File**: `vessel-input-form.component.ts`
  - **Change**: `@ViewChild(BatteryConfigSectionComponent)` + refresh call in
    `applyProfileInputValues`.

### Compliance Check

- Coding Standards: ✓ (matches sibling section idiom: standalone, OnPush, `[parentForm]`, Material)
- Project Structure: ✓ (section folder layout mirrors weather-input-section)
- Testing Strategy: ✓ with a caveat — the client has **zero spec files project-wide**; absence of
  new component tests is consistent with the existing convention, recorded as technical debt below
- All ACs Met: ✓ AC1–AC6 (AC1/AC2 verified by code inspection of `buildBatteryInput` + contract
  types; AC3 re-verified after the QA-D-1 fix; AC5 verified via profile validation paths;
  AC6 `ng build` clean, `ng lint` not configured in project scripts)

### Improvements Checklist

- [x] QA-D-1: reactive-form disable mechanism fixed
- [x] QA-D-2: DP availability refresh after profile restore
- [x] Hint copy generalized
- [ ] **Tech debt (project-wide):** introduce a client test harness usage (ng-mocks is installed
      but no spec exists); `buildBatteryInput` and profile v2→v3 loading are prime first targets
- [ ] **Increment C/E follow-up:** when PTI fields arrive, the battery section gains the suggested
      `MaxPtiPerEngineKw` prefill (= SG capacity) per ADR-5
- [ ] **Nice-to-have:** report (`ReportService`) does not yet mention the battery — consider a
      battery block in the client report when the feature stabilizes

### Security Review

No concerns — no new endpoints or storage beyond localStorage profiles (already established
pattern); imported profiles pass `isValidBattery` structural validation.

### Performance Considerations

Negligible — one additional OnPush section with two subscriptions; panel renders only when
`batteryDetails` present.

### Files Modified During Review

- `cl/src/app/features/vessel-input/vessel-input-form/battery-config-section/battery-config-section.component.ts`
- `cl/src/app/features/vessel-input/vessel-input-form/battery-config-section/battery-config-section.component.html`
- `cl/src/app/features/vessel-input/vessel-input-form/vessel-input-form.component.ts`

(Dev: fold into File List on next touch.)

### Gate Status

Gate: **PASS** → docs/qa/gates/battery.d-client-ui.yml
(`ng build` clean after review fixes, 7.8 s.)

### Recommended Status

✓ Ready for Done — with the manual smoke check below recommended before calling the feature
user-verified: restart the API (new backend from Increments A/B is not in the running process),
`ng serve`, enable a battery on a Transit scenario and confirm the Functions values + Battery
Contribution panel render.
(Story owner decides final status.)
