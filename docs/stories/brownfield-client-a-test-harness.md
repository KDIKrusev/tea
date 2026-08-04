# Story: Client A — A test harness that owns time and the network

<!-- Source: docs/refactoring/client-refactor-design.md §4 (runner choice), §6.1 (this story) -->
<!-- Context: Brownfield refactoring of cl/. The client has zero tests. First of nine (C-A → C-I). -->

## Status: Ready for Review

## Story

As a **developer about to refactor a nine-writer race condition**,
I want **a test harness that decides when each HTTP response arrives and when each timer fires**,
so that **the next fix to the restore cascade is verified instead of guessed — which is what the two
already-committed fixes were not**.

## Context Source

- Design §1 (why), §3 (invariants I1–I6), §4 (runner recommendation, approved by Kamen 2026-08-04),
  §6.1 (this story).
- The client has **no test target, no runner, and no spec files**. `@types/jasmine` 5.1 and
  `ng-mocks` 14.14 are already in `devDependencies` with nothing consuming them — the intent existed,
  only the runner is missing.
- The bug this epic exists for is a *timing* bug across two HTTP responses and six timer constants
  (200 / 400 / 500 / 800 / 1500 / 3000 ms). A harness that cannot control both independently cannot
  reproduce it. That requirement, not general coverage, is what shapes this story.

## Scope

### 1. Runner — Karma + Jasmine

Approved (§4). Add as `devDependencies` only:

| Package | Why |
|---|---|
| `karma` | runner |
| `karma-jasmine` | framework adapter |
| `jasmine-core` | the framework (`@types/jasmine` is already present) |
| `karma-chrome-launcher` | Chrome and Edge are both installed on the dev machine |
| `karma-jasmine-html-reporter` | local debugging |
| `karma-coverage` | coverage, off by default |

The builder — `@angular-devkit/build-angular:karma` — ships inside the installed devkit 18.2.6.
Nothing new is added to `dependencies`; the production bundle is untouched.

### 2. Wiring

- `angular.json` → a `test` architect target: builder `@angular-devkit/build-angular:karma`,
  `polyfills: ["zone.js", "zone.js/testing"]`, `tsConfig: "tsconfig.spec.json"`, the same `styles`
  and `assets` entries as `build`.
- `tsconfig.spec.json` extending `tsconfig.json`: `types: ["jasmine"]`,
  `include: ["src/**/*.spec.ts", "src/**/*.d.ts", "src/testing/**/*.ts"]`, `resolveJsonModule: true`.
- `package.json` scripts: `"test": "ng test"`, `"test:ci": "ng test --watch=false --browsers=ChromeHeadless"`.
- `eslint.config.js` → add `"tsconfig.spec.json"` to `parserOptions.project`. The existing
  `**/*.spec.ts` override block stays as it is.

`azure-pipelines.yml` is **not** touched in this story — `npm run test:ci` becomes a blocking step in
C‑C, together with the lint gate, so that the pipeline goes red exactly once and for a stated reason.

### 3. Proof-of-life spec

`src/app/features/vessel-input/vessel-input-form/form-edit-tracker.service.spec.ts` — the pure
`FormEditTrackerService`, no TestBed ceremony, five assertions covering its actual contract:

1. an unset field reads as not edited;
2. `setOriginalValue` does not overwrite an existing baseline, `updateOriginalValue` does;
3. numeric comparison is by value, so `"3250"` and `3250` are the same value (the service coerces
   with `Number()` — this is load-bearing for the `(edited)` badge);
4. a `null`/`undefined` baseline reads as not edited, whatever the current value;
5. a non-numeric value falls back to strict comparison.

If this is green, the infrastructure works. It asserts current behaviour — it is a characterisation
test, not a judgement about whether that behaviour is right.

### 4. `ApiFixture` — the network, on the spec's clock

`src/testing/api-fixture.ts`. A thin, typed wrapper over `HttpTestingController`:

```ts
answerCatalogue(data?: Partial<AppInitialData>): void   // GET /api/app-data/initial
answerVesselConfig(data?: Partial<FullVesselData>): void // GET /api/app-data/vessel-config
pendingCatalogue(): boolean
pendingVesselConfig(): number      // how many are queued
postedBodies(): CalculatorInput[]  // every POST to /calculate-all-variants, in order
answerCalculation(index, result?): void
verifyNoOutstanding(): void
```

Requirements:

- The two GETs are answered **independently**. A spec must be able to answer one, `tick(n)`, and
  answer the other. This is the single most important property of the harness.
- POSTs are captured, counted and readable as parsed bodies — C‑B's emission count and I2's frozen
  request body both depend on it.
- Default responses are realistic: the catalogue must contain the engine ids the scenarios use
  (1, 2, 7, 8), grouped by maker, plus `fuelDefaultPrices` and the categories the scenarios name.
  The vessel-config default must carry the *vessel-type* defaults that conflict with scenario 03
  (ME id 2 @ 15 000, SG 2 000, AE id 7 @ 1 000) — the numbers must not overlap with the profile's,
  so a spec failure names the winning writer on sight.

### 5. `RestoreHarness` — the component tree, on the spec's clock

`src/testing/restore-harness.ts`. Mounts the real tree under `fakeAsync`:

```ts
mountForm(opts?)            // VesselInputFormComponent + real child sections
mountPage(opts?)            // CalculatorPageComponent (for POST counting / baselineIndex)
loadProfile(scenario)       // the Load-Profile-click path (catalogue may be warm or cold)
seedDraft(scenario)         // the hard-refresh path: localStorage draft, then mount
formValue(): Record<string, unknown>   // getRawValue(), incl. the disabled hotelLoad
emissions(): FormChangeEvent[]
settle(): void              // tick past every known timer, then flush()
```

Notes that will otherwise cost an afternoon:

- Provide `ConfigService` directly with a fixed `apiUrl`. Do **not** bootstrap `appConfig` — its
  `APP_INITIALIZER` calls the browser `fetch` for `/config.json`, which `HttpTestingController` does
  not intercept.
- `provideNoopAnimations()`. `MatDialog` and `MatSnackBar` are constructed by three of the sections.
- `settle()` must tick past 3 000 ms (the restore watchdog) **and** call `flush()`, otherwise
  `fakeAsync` throws on the 30 s draft interval. Clear the interval in `ngOnDestroy` — it already is
  ([vessel-input-form.component.ts:132](../../cl/src/app/features/vessel-input/vessel-input-form/vessel-input-form.component.ts#L132)) — and destroy the fixture inside the `fakeAsync` zone.
- `localStorage` is shared across specs: clear both `ksailcalc_profiles` and `ksailcalc_draft` in
  `beforeEach`.

### 6. Scenario fixtures — one source (I3)

`src/testing/scenarios.ts` exposes the scenario JSONs from `docs/qa/manual-test-scenarios/`.
**No copies.** Preferred mechanism: `resolveJsonModule: true` plus a relative import
(`../../docs/qa/manual-test-scenarios/03-no-battery-reference.json`); esbuild resolves JSON imports
directly.

If that fails on path resolution outside `src/` (a real possibility — flag it, do not work around it
silently), the fallback is a `pretest` script that generates `src/testing/scenarios.generated.ts`
from the same files. Still one source; still no hand-maintained copy. Whichever is used, record the
choice and the reason in the Completion Notes.

### 7. Out of scope

No production file under `cl/src/app/` is modified. No backend file is touched. No lint fixes, no
dead-code removal, no `tsconfig` strictness flags — those are C‑C and C‑D. The harness is *only* the
harness plus one trivial spec.

## Acceptance Criteria

1. **AC1 — Backend untouched (I1).** `git status --porcelain` shows zero changes outside `cl/` and
   `docs/`. Backend suite reproduced green at **441/441** with the `BaseOutputPath` workaround, and
   `KSailCalc.Tests/Golden/Expected/` is clean. `GOLDEN_UPDATE` is never set.
2. **AC2 — Runner works.** `npm test` and `npm run test:ci` both run and report
   **1 spec, 5 assertions, 0 failures**. `test:ci` exits 0 and does not hang waiting for a browser.
3. **AC3 — Production untouched.** `git status --porcelain cl/src/app/` shows zero modified files.
   Every new file lives under `cl/src/testing/` or is a `*.spec.ts`. `package.json` gains
   `devDependencies` and scripts only — `dependencies` is byte-identical.
4. **AC4 — Build unaffected.** `ng build --configuration production` green; bundle size within
   the existing budgets and not larger than before this story.
5. **AC5 — Independent responses.** A harness self-test proves the defining property: answer
   `/api/app-data/initial`, `tick(1000)`, assert `/api/app-data/vessel-config` is still pending,
   then answer it. Both orders (catalogue-first and vessel-config-first) are exercised.
6. **AC6 — Conflicting defaults.** `ApiFixture`'s default vessel-config carries ME id 2 @ 15 000,
   SG 2 000, AE id 7 @ 1 000, and its default catalogue contains ids 1, 2, 7 and 8 with distinct
   `maxCapacityKW`. A spec asserting on any of those five fields can name the writer from the value
   alone.
7. **AC7 — One fixture source (I3).** `src/testing/scenarios.ts` resolves to the files under
   `docs/qa/manual-test-scenarios/`. `grep` finds no duplicated scenario JSON under `cl/`.
8. **AC8 — Lint still clean-ish.** `npm run lint` reports **no new problems** beyond the 190
   recorded baseline. (It is not yet zero — that is C‑C.) Spec and testing files must be covered by
   the lint config, not excluded from it.

## Tasks / Subtasks

- [x] Task 0: Record baselines — backend 441/441 (I5 workaround), `npm run lint` count,
      `ng build` size. Into the Debug Log.
- [x] Task 1: Install the six devDeps. Add the `test` target, `tsconfig.spec.json`, the two npm
      scripts, the eslint `project` entry.
- [x] Task 2: Write `form-edit-tracker.service.spec.ts` (AC2). Green before anything else.
- [x] Task 3: `ApiFixture` + its self-test (AC5, AC6).
- [x] Task 4: `RestoreHarness` + a smoke spec that mounts the form, answers both requests, settles
      and reads `formValue()` — no assertions about correctness yet, only that the tree mounts and
      quiesces.
- [x] Task 5: `scenarios.ts` (AC7). Record which mechanism was used and why.
- [x] Task 6: Verify AC1, AC3, AC4, AC8 explicitly and record the commands in the Debug Log.

## Dev Notes

- **Read `docs/refactoring/client-refactor-design.md` §3 before starting.** I2 (the request body is
  the client's golden snapshot) is frozen in the *next* story, but the `postedBodies()` accessor it
  will use is built here — get it right now rather than retrofitting it.
- **Do not fix anything.** Every trap listed in §5 above is a real defect or a real awkwardness in the
  production code. Working around it in the harness is correct for this story; fixing it is C‑E.
- **`app-data.service.ts` has a hand-rolled promise wrapper** ([lines 36–63](../../cl/src/app/core/app-data.service.ts#L36-L63)). Subscribers registered while a load is in flight resolve as
  microtasks in registration order. `fakeAsync` handles microtasks on `tick(0)`/`flush()`, but a spec
  that only calls `tick(400)` may see a different interleaving than the browser. Prefer `settle()`
  over hand-picked ticks in every spec that is not deliberately probing one instant.
- The engine catalogue and the vessel categories come from the **same** `/api/app-data/initial`
  response. `ApiFixture.answerCatalogue()` therefore satisfies four subscribers at once — that is a
  faithful reproduction of production, not a shortcut.
- Angular 18 is not on signal forms; nothing here needs `TestBed.flushEffects`.

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (claude-opus-5[1m])

### Completion Notes

- **18 specs, all green, 2.3 s.** One proof-of-life spec (5 assertions), nine `ApiFixture`
  self-tests, four `RestoreHarness` smoke tests.
- **The JSON import worked on the first mechanism** (AC7). `resolveJsonModule: true` in
  `tsconfig.spec.json` plus a relative import from `cl/src/testing/` up to
  `docs/qa/manual-test-scenarios/` resolves under esbuild with no build step. The generated-barrel
  fallback was **not** needed and was not written. `cl/` holds no copy of any scenario file.
- **`src/testing/**` had to be excluded from `tsconfig.app.json`.** The app config `include`s
  `src/**/*.ts` and only excluded `*.spec.ts`, so the harness helpers were being type-checked as
  part of the application build — where `resolveJsonModule` is off. The production bundle content
  hash is identical before and after the exclusion (`main-ZS6ZQ45V.js` both times), which is also
  the proof for AC4: the bundle never contained them.
- **Two harness accommodations, both recorded as findings rather than smoothed over:**
  1. `detectChanges(false)` — the checked pass throws `NG0100` mid-restore because
     `onVesselEngineConfigSelected` assigns `vesselTypeName` from inside a subscription that runs
     during change detection. Logged as design **§7.8**. Letting it abort would bury every C‑B
     assertion; the call reverts to the checked form in C‑E and that revert is the fix's proof.
  2. `dispose()` must `flush()` after destroying the tree — the 800 ms profile-apply fallback
     `setTimeout` has no handle and survives `ngOnDestroy`. Logged as design **§7.9**.
- **`ApiFixture` owns the outstanding-request check, not `httpMock.verify()`.**
  `HttpTestingController.match()` *removes* what it returns from the backend's open list, so once
  the fixture drains, `verify()` would pass vacuously. `verifyNoOutstanding()` reimplements it over
  the fixture's own queues, and a self-test asserts that it actually throws.
- **Fixture numbers are deliberately non-overlapping.** The three possible sources of an engine
  rating — catalogue first entry (ME id 3 @ 9 000), vessel-type default (ME id 2 @ 15 000) and the
  restored profile (ME id 1 @ 24 000) — are pairwise distinct, and `categories[0]` is *Bulk Carrier*
  while every golden scenario is *Offshore Support*. A C‑B failure will name the winning writer from
  the observed value alone, with no instrumentation.
- **Finding, severity correction (design §7.1):** every one of the 35 scenario files stores a
  `fuelPrice` exactly equal to its main fuel's backend default (MDO 780, LNG 620, HFO 420, Ammonia
  1350). The fuel-price overwrite therefore replaces the value with itself and is invisible in all
  of them. `withInput()` was added to `scenarios.ts` so C‑B's spec 4 can synthesise a user-chosen
  price instead.
- **Finding, confirms a C‑B prediction (design §7.1 confirmed section):** matrix ordering O5 —
  "the catalogue returns after the profile has been applied" — is **unreachable on today's code**.
  Both `loadCategories` and `selectVessel` await `/api/app-data/initial`, so the per-vessel fetch is
  structurally downstream of it. Recorded as a characterisation spec in `restore-harness.spec.ts`.
- Nothing was committed.

### Debug Log References

| Run | Result |
|---|---|
| Backend baseline, before any change | **441/441 green**, 1 s (`-p:BaseOutputPath=<scratch>/testbin/`, `appsettings.json` copied two levels above) |
| `npm run lint` baseline | **190 problems (151 errors, 39 warnings)**, 102 auto-fixable |
| `ng build` baseline | `main-ZS6ZQ45V.js` 978.02 kB · initial total 1.27 MB · **pre-existing** budget warning (+14.25 kB over 1.26 MB) |
| `npm run test:ci` after Task 2 | **5/5 green**, exit 0, no hang (verified with and without `CHROME_BIN`) |
| `npm run test:ci` after Task 3 | **14/14 green** |
| `npm run test:ci` after Task 4, first attempt | **2 FAILED** — `NG0100` (→ §7.8) and "2 timer(s) still in the queue" (→ §7.9) |
| `npm run test:ci` final | **18/18 green**, 2.3 s |
| `ng build` final | bundle content hash **identical** to baseline (`main-ZS6ZQ45V.js`), same budget warning |
| `npm run lint` final | **190 problems (151 errors, 39 warnings)** — unchanged, no new problems |

Explicit AC verification:

- AC1 — `git status --porcelain` shows **zero** changes outside `cl/` and `docs/`.
  `KSailCalc.Tests/Golden/Expected/` untouched. `GOLDEN_UPDATE` never set.
- AC3 — `git status --porcelain cl/src/app/` → one untracked file, the new spec. `dependencies` in
  `package.json` byte-identical; only `devDependencies` and two scripts added.
- AC4 — identical bundle hash, above.
- AC8 — `npx eslint` run directly against the four new files → **exit 0**, so they are genuinely
  covered by the lint config rather than silently skipped over a missing tsconfig project.

### File List

New:
- `cl/karma.conf.js`
- `cl/tsconfig.spec.json`
- `cl/src/testing/scenarios.ts`
- `cl/src/testing/api-fixture.ts`
- `cl/src/testing/api-fixture.spec.ts` (9 specs)
- `cl/src/testing/restore-harness.ts`
- `cl/src/testing/restore-harness.spec.ts` (4 specs)
- `cl/src/app/features/vessel-input/vessel-input-form/form-edit-tracker.service.spec.ts` (5 specs)

Modified:
- `cl/package.json` (6 devDeps, `test` + `test:ci` scripts)
- `cl/package-lock.json`
- `cl/angular.json` (`test` architect target)
- `cl/tsconfig.app.json` (exclude `src/testing/**`)
- `cl/eslint.config.js` (`tsconfig.spec.json` added to `parserOptions.project`)
- `docs/refactoring/client-refactor-design.md` (§4 decision recorded, §7.1 severity correction,
  §7.8 and §7.9 added, §7.1-confirmed section added)

Deleted: none.

### Change Log

| Date | Change |
|---|---|
| 2026-08-04 | C‑A implemented: Karma + Jasmine wired, `ApiFixture` + `RestoreHarness` + scenario fixtures added, 18/18 green. Production bundle byte-identical, lint count unchanged, backend untouched. Three new findings logged (§7.1 severity correction, §7.8, §7.9). Status → Ready for Review. |

## QA Results

_(to be filled by Quinn)_

## Risk Assessment

- **Primary:** the harness quietly diverges from the browser, so C‑B's red test reproduces a
  *different* race than the one Kamen sees. **Mitigation:** AC5 and AC6 — the harness must be able to
  hold one response back while the other lands, and the fixture numbers must not overlap with the
  profile's, so any spec failure names the writer by value.
- **Secondary:** the JSON import from outside `src/` fails and gets "solved" by copying the scenario
  files into `cl/`. That silently breaks I3 and recreates the drift the manual scenarios exist to
  prevent. **Mitigation:** AC7 plus the explicit generated-barrel fallback.
- **Tertiary:** `fakeAsync` throws "N timer(s) still in the queue" because of the 30 s draft interval
  and the 3 s watchdog, and the fix becomes `discardPeriodicTasks()` sprinkled everywhere.
  **Mitigation:** one `settle()` in the harness, used by every spec.
- **Rollback:** single commit, `git revert`. No production code, no backend, no schema.
