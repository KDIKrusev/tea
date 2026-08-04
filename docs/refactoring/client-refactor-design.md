# Client Refactoring — Architecture Design

<!-- Author: Winston (Architect) · Date: 2026-08-04 -->
<!-- Source for stories: docs/stories/brownfield-client-{a..i}-*.md (to be written after scope approval) -->
<!-- Scope: cl/ only. No backend changes. No calculation changes. -->

## 1. Why

The client has **zero tests**. That is not a hygiene observation — it is the direct cause of the
session that produced this document: the same bug was "fixed" and committed twice, and both fixes
were correct in principle while the symptom survived. Without a test that pins the *order of
events*, a fix to a race is a guess with a commit message.

The bug itself is structural. Eight independent writers patch one `FormGroup`, coordinated only by
five `setTimeout` constants and four boolean flags:

| Writer | File | Coordination |
|---|---|---|
| `loadCategories` → `applyCategorySelection` | `vessel-config-section.component.ts:128` | none |
| `selectVessel` | `vessel-config-section.component.ts:353` | none |
| `watchSizeAndSpeedInputs` | `vessel-config-section.component.ts:238` | debounce 400 |
| `applyVesselData` | `vessel-config-section.component.ts:286` | `FetchRequest` flags |
| `setEngineConfiguration` | `engine-config-section.component.ts:255` | `pendingEngineConfig` |
| `applyPendingEngineConfig` | `engine-config-section.component.ts:326` | `pendingEngineConfig` |
| `applyProfileInputValues` | `vessel-input-form.component.ts:618` | timers 200 / 800 |
| `updateFuelPriceFromFuelType` | `vessel-input-form.component.ts:197` | debounce 500 |

Plus `loadEngineConfigurations` (`engine-config-section.component.ts:96`), which silently applies
`mainEngineTypes[0]`'s rated capacities whenever the catalogue lands while nothing else has claimed
the field yet — a ninth writer that no flag guards.

The flags are `componentsLoaded`, `initialEmissionScheduled`, `restoreInFlight`,
`pendingProfileInput`, `pendingEngineConfig`. The timers are 200, 400, 500, 800, 1500, 3000 ms.
**Who wins depends on which HTTP response arrives first.** The three logged findings — engine values
overwritten after restore, `CalculateAllVariants` called three times on load, `baselineIndex` lost —
are one root cause with three faces.

Two of the three are already provable by reading:

- **`baselineIndex`**: `applyProfileInputValues` calls `endRestore()` (line 689) *before*
  `emitFormValues()` (line 691). That last emission of the restore is therefore tagged `'user'`, and
  `calculator-page.component.ts:260-262` clears the pinned baseline on any `'user'` emission. The pin
  is destroyed by the restore that was supposed to carry it.
- **Three calculations**: emission #1 from `onOperationalProfileLoaded` (line 603), #2 from
  `applyProfileInputValues` (line 691), #3 from the 500 ms `valueChanges` debounce fed by the
  `emitEvent:true` patches in `applyVesselData`, `onVesselEngineConfigSelected` and
  `populateFormWithProfile`.

The third — engine values overwritten — had several candidate orderings, and reading could not
settle which one fires. **Story C‑B settled it by experiment.**

### 1.1 The reproduced root cause (C‑B, 2026-08-04)

The restore declares itself finished **before its own HTTP response has arrived**. The flag is
right; its lifetime is wrong.

Trigger: the user clicks Load while the page's initial vessel-config request is still in flight —
i.e. they open the app and click their saved scenario straight away.

1. `applyCategorySelection` has already fired a fetch for the auto-selected default category.
2. `selectVessel` queues a **second** fetch, 400 ms behind it.
3. The **stale** response lands first. `restoreInFlight` is `true`, engine defaults are correctly
   skipped, and the profile is applied on the 200 ms path. *The form now holds the file's values —
   this is the flash.*
4. `applyProfileInputValues` calls `endRestore()` (line 689).
5. The **restore's own** response arrives. `restoreInFlight` is now `false`, so
   `onVesselEngineConfigSelected` computes `applyEngineDefaults = true` (line 496) and calls
   `setEngineConfiguration`; `applyVesselData`'s `patchPowerFields` overwrites `propulsionPower`.
   *The vessel-type defaults replace the profile.*

Both committed fixes govern what happens **while** `restoreInFlight` is set. This race plays out
**after** it is cleared, which is why neither could have worked. It also explains why manual
reproduction is intermittent (it needs a stale request in flight) and why the README's workaround —
"load the scenario once more" — succeeds: on the second load there is no stale request.

Confirmed at the same time: the **first** of the three calculations a restore fires is computed on
the *vessel-type default plant*, not the user's — propulsion 8888 vs 12036.15, ME 15000 vs 24000,
fuel price 950 vs 780. For a moment the panels show results for the wrong ship under the scenario's
name. C‑F is worth more than two saved HTTP calls.

This document defines the target structure and the **invariants** that make the epic
provably behaviour-preserving.

## 2. Non-goals

- Any backend change. `441/441` stay green, golden snapshots byte-identical.
- Any change to the request body produced by a given settled form state (see I2).
- Any change to a displayed number, label, rounding or panel order.
- Any Angular version upgrade (18.2 stays; see §7.4).
- Fixing the findings in §7 — each changes behaviour and needs Kamen's decision first.

## 3. Invariants (binding on every story)

**I1 — The backend is frozen.**
`GOLDEN_UPDATE=1` is forbidden for the whole epic. Every story's Task 0 reproduces the baseline:

```
dotnet test KSailCalc.Tests\KSailCalc.Tests.csproj -p:BaseOutputPath=<temp>\
```

`git status` under `KSailCalc.Tests/Golden/Expected/` must be clean at every commit.

**I2 — The request body is the client's golden snapshot.**
This is the invariant that closes the gap Kamen named: *the golden tests post JSON straight at the
API and never pass through the form, so they stay green while the UI is wrong.*

For a given settled form state, the JSON posted to `/api/calculator/calculate-all-variants` must be
**identical** before and after every story. Story C‑B froze that body for scenarios 01, 03 and 15
into `cl/src/testing/golden/`; every later story compares against it. A story that changes one is
not a refactoring story — stop and escalate.

*Comparison basis (settled in C‑B):* the **parsed** body — same keys, same values, same types —
not the serialised string. Key order is not part of the wire contract, and freezing it would make
C‑G's `vessel-form.mapper.ts` extraction fail for a reason that has nothing to do with behaviour.
`undefined` properties are dropped by the JSON round-trip before comparison, exactly as they are
before they reach the network.

*What is frozen is today's behaviour, bugs included.* `15-baseline-user-pick.request.json` carries
no `baselineIndex` although scenario 15 pins index 4. I2 guards against *unintended* change; C‑E
re-freezes deliberately and names the diff.

This is the client's equivalent of `Golden/Expected/`, and it is the whole safety case for
"same numbers, clearer code".

**I3 — One fixture source.**
Specs read `docs/qa/manual-test-scenarios/*.json` directly. No copies. A client test and a manual
scenario can never drift, and the three client-only fields (`hotelLoad`, `batteryCapacity`,
`sailInstalled`) stay covered by the same files that document them.

**I4 — Red before green.**
C‑B contains **no production code change**. Its Definition of Done is that its specs *fail*, with the
losing writer and the millisecond named in the Debug Log. If a spec passes on today's code, we tested
something else — stop, do not proceed to C‑E.

**I5 — One story, one commit, independently revertable.** No commit without Kamen's explicit word.

**I6 — Behaviour-preserving means identical.** Anything that would move a number is logged in §7 and
asked. It is never "fixed" inside a refactoring story.

**Verified baseline (2026-08-04, before any change):**
`npm run lint` → **190 problems (151 errors, 39 warnings), 102 auto-fixable**.
`ng build` → green. Client tests → **none exist**. Backend → 441/441.

## 4. Test runner — decision

**Karma + Jasmine — approved by Kamen, 2026-08-04.** Reasoning, since Kamen asked for the argument
and not the default:

1. **The builder already exists.** `@angular-devkit/build-angular:karma` ships inside the installed
   devkit (18.2.6). Angular 18's Jest builder (`:jest`) is flagged experimental and does not cover the
   application builder's full pipeline; Vitest has no first-party Angular builder until 20
   (`@angular/build:unit-test`). On 18.2 both are a bet, Karma is not.
2. **Someone already started.** `@types/jasmine` 5.1 and `ng-mocks` 14.14 are in `devDependencies`
   with nothing consuming them. The intent was there; only the runner is missing.
3. **`fakeAsync`/`tick`/`flush` are the tools this epic needs.** Making 200/400/500/800/1500/3000 ms
   deterministic *is* the task. They are zone-based, the app runs `provideZoneChangeDetection`, and
   under Karma they work with no adapter layer. Under Jest+jsdom they work too — but the friction
   (ESM, `jest-preset-angular` pinning) buys nothing for this specific problem.
4. **Environment is ready.** Chrome and Edge are both installed; `ChromeHeadless` works locally and in
   `azure-pipelines.yml`.

Cost: six `devDependencies` (`karma`, `karma-chrome-launcher`, `karma-jasmine`,
`karma-jasmine-html-reporter`, `karma-coverage`, `jasmine-core`). Zero runtime impact.

Counter-argument, for the record: Jest starts faster and needs no browser in CI. If that becomes
worth it, the migration is mechanical — the specs are plain `TestBed` specs with no Karma-specific
API. It is not worth it *now*, because the value of this session is reproducing a timing bug, and
Karma is the path with the fewest unknowns between us and a red test.

## 5. Target structure

```
CalculatorPageComponent                    ~150 lines — page state + one calculation stream
└─ VesselInputFormComponent                ~150 lines — hosts sections, owns nothing
   ├─ vessel-form.schema.ts                the fb.group, extracted verbatim
   ├─ vessel-form.mapper.ts   (pure)       form value → CalculatorInput   ← the client's TierResultBuilder
   ├─ FormWriteCoordinator                 THE single writer (story C-E)
   │    ├─ layer 0  catalogue defaults
   │    ├─ layer 1  vessel-type defaults
   │    ├─ layer 2  profile / draft values
   │    └─ layer 3  user edits
   ├─ DraftAutosaveService                 the 30 s interval, out of the component
   └─ sections/ …                          render-only; they raise intent, they never patch

results-display/                           one component per panel, in render order
   01-validation-warnings · 02-power-demands · 03-baseline · 04-battery-contribution
   05-tier-panel (×3) · 06-sail · 07-tier-comparison · 08-charts
```

### 5.1 The cascade, restated

Today: nine writers race; the last one to arrive wins; timers try to guess the order.

Target: **precedence replaces arrival order.** Every async source resolves into a *layer*. Layers
have a fixed priority. `FormWriteCoordinator` composes them and performs **one** `patchValue` when
the sequence settles, then emits **once**:

```
idle → selecting → awaiting(catalogue, vesselConfig) → compose(layers) → settled → emit once
```

No `setTimeout` survives except the two deliberate UX debounces — 500 ms on form input and 400 ms on
size/speed — each keeping a comment that says why it exists. `componentsLoaded`,
`initialEmissionScheduled`, `restoreInFlight`, `pendingProfileInput`, `pendingEngineConfig` are all
deleted: they exist only to approximate the sequence the state machine states outright.

`AppDataService.loadInitialData()` loses its hand-rolled `loadingPromise` +
`new Observable(observer => promise.then(...))` wrapper (lines 36-63). That wrapper is *why*
subscription order decides the winner: four subscribers resolve as microtasks in registration order,
and `loadEngineConfigurations` is second in that queue. One `shareReplay({bufferSize:1,
refCount:false})` replaces it.

## 6. Story breakdown

Nine stories, in order. Each is behaviour-preserving on its own.

| Story | Title | What it removes |
|---|---|---|
| **C‑A** | Test harness that owns time and the network | "the client has no tests" |
| **C‑B** | The red suite | "we cannot reproduce it" |
| **C‑C** | Lint gate: 190 → 0, blocking | 151 errors, 39 warnings |
| **C‑D** | Dead code + `noUnusedLocals`/`noUnusedParameters` | ~800 lines that ship and do nothing |
| **C‑E** | One writer, one order — the load sequence ✅ **DONE** | 4 flags, 5 timers, 2 of 3 calculations |
| **C‑F** | One calculation on load | 2 of the 3 POSTs |
| **C‑G** | Screen order in the code | 712-line component, 395-line template |
| **C‑H** | Signals: contained adoption, argued | 8 manual `markForCheck()` calls |
| **C‑I** | Cleanups + observability | copy-pasted snackbars, swallowed errors |

### 6.1 C‑A — Test harness that owns time and the network

1. Runner: the six devDeps, `karma.conf.js`, `tsconfig.spec.json`, a `test` target in
   `angular.json`, `npm test` and `npm run test:ci` (`--watch=false --browsers=ChromeHeadless`).
2. **Proof-of-life spec**: `form-edit-tracker.service.spec.ts` — a pure service, five assertions,
   no TestBed magic. If this is green, the infrastructure works.
3. `ApiFixture` — a thin helper over `HttpTestingController` that answers
   `GET /api/app-data/initial` and `GET /api/app-data/vessel-config` **independently, at a moment the
   spec chooses**, and captures every `POST /api/calculator/calculate-all-variants`.
4. `RestoreHarness` — mounts the component tree under `fakeAsync`, exposes
   `loadProfile(json) · answerCatalogue(at) · answerVesselConfig(at) · tick(ms) · flush() ·
   formValue() · emissions() · postedBodies()`.
5. Fixtures read the scenario JSONs from `docs/qa/manual-test-scenarios/` (I3).

**DoD:** `npm test` green with one spec. Zero production code touched. Zero backend touched.

### 6.2 C‑B — The red suite

Four specs. Three must fail today; the fourth is gated on §7.1.

1. **`restore-engine-values.spec.ts`** — scenario 03 (ME id 1 @ 24 000, SG 3 250, AE id 8 @ 4 000
   against vessel-type defaults ME id 2 @ 15 000, SG 2 000, AE id 7 @ 1 000). Run the **full arrival
   matrix**: catalogue before / interleaved with / after the profile is applied; vessel-config fast /
   slow; catalogue slower than the 3 000 ms watchdog. After `flush()`, assert in *every* order:
   `meCapacityPerEngine 24000 · sgCapacityPerEngine 3250 · aeCapacityPerEngine 4000 ·
   mainEngineTypeId 1 · auxEngineTypeId 8 · propulsionPower 12036.15`.
2. **`restore-baseline-index.spec.ts`** — scenario 15 (`baselineIndex: 4`). Assert the last POST body
   carries `baselineIndex: 4`. Expected failure: `undefined`.
3. **`load-emits-once.spec.ts`** — scenario 03. Assert exactly **one** POST after quiesce.
   Expected failure: three.
4. **`restore-fuel-price.spec.ts`** — scenario 03 saves `fuelPrice: 780`. See §7.1. **Write it,
   `xit` it, and reference the finding** until Kamen decides; do not let it fail silently.

Then freeze I2: snapshot the settled POST body for scenarios 01, 03, 15 into
`cl/src/testing/golden/`.

**DoD:** specs 1–3 fail with a diagnosable message; the Debug Log names, per failing order, *which*
writer wrote last and at what millisecond. No production file modified.

### 6.3 C‑C — Lint gate

`--fix` clears 102. The remaining 88 by hand: `curly` (74), `explicit-function-return-type` (27),
`eqeqeq` (17), `no-inferrable-types` (17), `prefer-inject` (16), `use-track-by-function` (10),
`no-unused-vars` (7), `array-type` (7), the rest single-digit.

A rule that genuinely does not fit gets disabled **with a written reason in `eslint.config.js`** —
never silently. `prefer-on-push-component-change-detection` stays a warning here and is escalated to
error in C‑G, when the two remaining non-OnPush components are converted (converting them is a
change-detection change and does not belong in a lint story).

`npm run lint` becomes a blocking step in `azure-pipelines.yml` — the analogue of
`TreatWarningsAsErrors` on the backend.

**DoD:** `npm run lint` exits 0. `ng build --configuration production` green.

### 6.4 C‑D — Dead code and tsconfig strictness

Turn on `noUnusedLocals` and `noUnusedParameters`, then delete what the compiler and the sweep found:

| Dead thing | Size | Evidence |
|---|---|---|
| `result-metric-card` component | 137 ts + 81 html + 266 css | no `<app-result-metric-card>` anywhere |
| `savings-breakdown-card` component | 169 lines | no selector use |
| `sail-indicator` component | 71 lines | in `calculator-page` `imports[]`, absent from its template |
| `form-select-field` component | 55 ts + html + css | in `vessel-config-section` `imports[]`, absent from its template |
| `shared/pipes/index.ts` | 0 bytes | empty file, exported by nothing |
| `CO2_EMISSION_FACTOR` | — | `@deprecated`, zero uses |
| `DEFAULT_VALUES.ANNUAL_HOURS`, `VALIDATION_LIMITS.ANNUAL_HOURS` | — | zero uses |
| `DEBOUNCE_TIMES.SEARCH`, `DEBOUNCE_TIMES.RESIZE` | — | zero uses |
| `AppDataService.getCurrentData()`, `.getAppData()` | — | zero callers |
| `OperationalModesSectionComponent.operationalProfileLoaded` | @Output | never bound (the parent binds the *vessel-config* one) |
| `app.routes.ts` + `provideRouter` | — | empty `Routes`, no `<router-outlet>` |
| `cypress-run` / `cypress-open` / `ct` / `e2e` targets | 4 targets | `@cypress/schematic` is **not installed** — the targets are broken |
| `WeatherInputSectionComponent.weatherChanged.sailEnabled` | — | parent's handler ignores its whole payload (`vessel-input-form.component.ts:698`) |

Roughly 800 lines that ship in the bundle today.

**DoD:** `ng build --configuration production` green, bundle not larger; C‑B specs still red
(expected — the story reports "3 failures, all from the known red suite, no others").

### 6.5 C‑E — One writer, one order (the core)

The story that turns the red suite green. Design in §5.1. **The cause is now known (§1.1), so this
story has a specific target: a restore must not be able to end before every response it caused has
been processed.** A `restoreInFlight` boolean cannot express that — a sequence with a declared set
of awaited sources can. Concretely:

1. `AppDataService`: replace `loadingPromise` + the `new Observable(...)` wrapper with one
   `shareReplay({bufferSize:1, refCount:false})`. Subscription order stops deciding anything.
2. Introduce `FormWriteCoordinator`. Sections stop calling `parentForm.patchValue` — they raise
   intent (`categorySelected`, `engineSelected`, `sizeChanged`) and the coordinator writes.
3. The restore becomes a state machine over `forkJoin(catalogue, vesselConfig)`, not a chain of
   timers. `applyProfileInputValues`'s double `setEngineTypeReferences` call disappears with it.
4. Emission: one `formState$` that emits per settled state. `source` comes from the machine, not from
   a flag read at emit time. `endRestore()`-before-`emitFormValues()` cannot recur because neither
   exists.
5. The pinned baseline is cleared by an explicit **user-edit event**, not inferred from a form
   emission's tag. `calculator-page.component.ts:260-262` goes away.
6. Delete: all five `setTimeout` constants, `RESTORE_WATCHDOG_MS`, `componentsLoaded`,
   `initialEmissionScheduled`, `restoreInFlight`, `pendingProfileInput`, `pendingEngineConfig`,
   `scheduleInitialEmission`, the 800 ms fallback, `engine-config-section.component.ts:79`'s
   zero-delay `setTimeout`.

7. **A stale in-flight response must not be able to drive a restore.** Cell E3 shows the restore
   being completed by a response fetched for a *different* vessel selection. The sequence must
   accept only the response it asked for.

**DoD:** C‑B specs 1–3 green **for every cell in the matrix, E3 included**; I2 snapshots unchanged
except for the deliberate `baselineIndex` re-freeze; only two timing constants remain in the client,
both commented.

### 6.6 C‑F — One calculation on load

Drive the calculation from settled form state. Assert: 1 POST on cold load · 1 per restore · 1 per
user edit after debounce · 1 silent POST per baseline re-pick. Separate from C‑E so the emission
count gets its own gate rather than riding along.

### 6.7 C‑G — Screen order in the code

The `Services/Results/` property Kamen asked for, applied to the client.

- `calculator-page.component.html` 395 → ~120 lines: the results column becomes `<app-*>` elements in
  render order, numbered `01…08` with comments, matching what is on screen top to bottom. The three
  tier accordions (Advanced/Pro/Premium) are one `<app-tier-panel [tier]>` used three times — today
  they are 120 lines of near-identical markup.
- `vessel-input-form.component.ts` 712 → ~150: `fb.group` → `vessel-form.schema.ts`;
  `buildCalculatorInput`/`buildBatteryInput` → `vessel-form.mapper.ts` (pure, unit-testable in
  isolation — this is where I2's snapshot gets its cheapest test); draft timer →
  `DraftAutosaveService`; restore → C‑E's coordinator.
- `vessel-input-form` and `operational-modes-section` become OnPush (25/25); escalate the lint rule
  to error.

### 6.8 C‑H — Signals: the argument

**Recommendation: do NOT move form state to signals. Do use signals for derived view state.**

Against a state migration:

- **The bug is not a change-detection bug.** 23 of 25 components already run OnPush and every value
  on screen is correct at the instant it is written. The defect is *which writer wrote last* —
  signals do not arbitrate that. A signal store would inherit the identical race unless C‑E lands
  first, and landing it *before* C‑E would make the red test harder to write, not easier.
- **`ReactiveFormsModule` is the state container, and Angular 18 has no signal forms.** "Signals for
  state" on 18.2 means running two state systems side by side and syncing them — a tenth writer, in
  an epic whose entire purpose is to get to one.
- **18.2 has `toSignal`/`toObservable` and nothing else that matters here** — no signal forms, no
  `linkedSignal`, no `resource()`. The ergonomics that justify the churn arrive in 19/20. Migrating
  now buys the risk and defers the payoff.

For, narrowly: in `CalculatorPageComponent`, `isCalculating`, `recommendedTier`,
`advancedExpanded/proExpanded/premiumExpanded`, `hasResults` and `batteryDetails` become signals;
`allResults` and `allVariantsResult` become `computed()`. Eight manual `markForCheck()` calls
disappear. Contained, provable, reversible.

An Angular 19/20 upgrade plus signal forms is a **separate epic** (§7.4). Not this one.

### 6.9 C‑I — Cleanups and observability

Four snackbar call sites repeat the same `{ duration: 5000, panelClass: ['error-snackbar'] }` —
one `NotificationService`. `console.warn` policy aligned with the lint rule. `getValidationError`
exists in three components with three slightly different bodies while `FormValidationService`
already provides it — collapse to the service.

## 7. Deferred — behaviour-changing, tracked separately

Each moves a number or a user-visible behaviour. Logged, not fixed. **Needs Kamen's decision.**

1. **A restored profile loses its saved fuel price.** `applyProfileInputValues` patches
   `fuelPrice: 780` (line 646) and then calls `setEngineTypeReferences` **again** (line 671), whose
   `reconcileMainFuel` → `prefillPriceFromMainFuel` overwrites it with the catalogue default and
   re-baselines the edit tracker — so the 500 ms `updateFuelPriceFromFuelType` pass keeps the default
   too. Every saved scenario stores 780; restoring one shows the fuel default instead. Fixing this
   changes displayed $ figures, so it is not part of a refactoring story. *New finding, 2026-08-04.*
   **Decision (Kamen, 2026-08-04): deferred.** C‑B writes the spec and `xit`s it with a reference to
   this section, so the question stays tracked instead of forgotten.
   **Severity correction (C‑A, 2026-08-04):** all 35 scenario files store a `fuelPrice` *equal to*
   their main fuel's backend default (MDO 780, LNG 620, HFO 420, Ammonia 1350), so the overwrite
   replaces the value with itself and is invisible in every scenario. It only bites a user who typed
   a custom price and saved it. The mechanism is unchanged; the impact is narrower than first stated,
   and C‑B's spec 4 must therefore *synthesise* a profile (`withInput`) rather than load a file.
2. **A child mutates its own `@Input()`.** `weather-input-section.onSailToggle` assigns
   `this.sailContribution = null` — parent-owned state written by the child.
3. **`onWeatherChanged(data)` ignores its entire payload** (`vessel-input-form.component.ts:698`, and
   the lint error there). The `sailEnabled` form control is what actually reaches the request.
   Possibly a fully dead path — verify before deleting.
4. **Angular 19/20 upgrade + signal forms.** The prerequisite for revisiting C‑H's decision.
5. **`hotelLoad` has two sources.** It is a *disabled* control written by
   `updateWeightedAverageHotelLoad`, and it is also carried in the saved profile and required by the
   import validator. One field, two authorities.
6. **`initializeApp` swallows a failed `/config.json`** and silently falls back to
   `https://localhost:7197`. In production that is a hard failure that presents as a hang.
7. **`FormEditTrackerService` is a root singleton keyed by bare field name** with no reset between
   restores. It survives a profile load; whether that is intended is a product question.
8. **`NG0100 ExpressionChangedAfterItHasBeenCheckedError` during a restore.** *Found by the C‑A
   harness, 2026-08-04 — mechanical proof, not a reading.* `onVesselEngineConfigSelected` assigns
   `this.vesselTypeName` from inside a subscription that runs *during* a change-detection pass, so
   the template binding changes after it was checked. Dev-mode only, so no user ever sees an error —
   but it is a hard demonstration that a write lands mid-CD, which is precisely the class of problem
   C‑E removes. The harness runs `detectChanges(false)` until then; that call reverts to the checked
   form in C‑E, and the revert is the fix's proof.
9. **The 800 ms profile-apply fallback `setTimeout` has no handle and no cancellation path.**
   *Closed by C‑E — the timer no longer exists.* The harness still `flush()`es after destroy, which
   is now belt-and-braces rather than a requirement.
10. **An auto-draft never carries a pinned baseline.** *Found while verifying C‑E's AC5.*
    `startAutoDraft` builds its payload with `buildCalculatorInput()`, which has no `baselineIndex`
    — that field is not a form control, and only the explicit Save path adds it via
    `getCurrentInputSnapshot(baselineIndex)`. So a hard refresh silently drops a pinned baseline even
    though an explicitly saved profile now keeps it. Pre-existing and untouched by C‑E. Fixing it
    changes what a restored draft calculates, so it needs Kamen's decision.

### 7.1 Confirmed by the C‑A harness

**Matrix ordering O5 — "the catalogue returns after the profile has been applied" — is unreachable
on today's code.** Both `loadCategories` and `selectVessel` await `/api/app-data/initial`, so the
per-vessel fetch is structurally downstream of the catalogue and no interleaving can invert them.
Recorded as a characterisation test in `restore-harness.spec.ts`. C‑E removes that coupling, at
which point the ordering becomes reachable and the spec turns from a description into a guard.

## 8. Estimate

C‑A ≈ 4h · C‑B ≈ 5h · C‑C ≈ 3h · C‑D ≈ 2h · **C‑E ≈ 8h** · C‑F ≈ 2h · C‑G ≈ 6h · C‑H ≈ 3h · C‑I ≈ 2h
— roughly 4½ days including QA gates.

The whole epic's safety rests on C‑B. If C‑B's specs do not go red, nothing after it is trustworthy.
