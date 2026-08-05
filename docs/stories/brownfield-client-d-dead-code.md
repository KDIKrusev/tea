# Story: Client D — Dead code and unused-symbol strictness

<!-- Source: docs/refactoring/client-refactor-design.md §6.4 -->
<!-- Context: Brownfield refactoring of cl/. Runs after C-E, before C-C (see Dev Notes). -->

## Status: Ready for Review

## Story

As a **developer about to reorganise this client**,
I want **the code that ships but does nothing removed, and the compiler told to keep it that way**,
so that **C‑C's lint pass and C‑G's restructuring are spent on code that is actually used**.

## Context Source

- Design §6.4. Ordering deviates from the document deliberately: **C‑D runs before C‑C.** The dead
  components carry their own lint errors (`result-metric-card` alone has six), so deleting first
  shrinks C‑C instead of spending it on files that are about to disappear.
- On the backend, the equivalent sweep (story R‑E) found five dead things that eight stories of
  manual review had walked past.

## Scope

### 1. Compiler strictness

`tsconfig.json`: `noUnusedLocals` and `noUnusedParameters` → `true`. Ten errors surface; each is
fixed, none is suppressed with a blanket `_`-rename where the symbol should simply not exist.

### 2. Components that ship and are never rendered

Verified by selector search across every `.html` and `.ts` — zero occurrences:

| Component | Lines (ts + html + css) |
|---|---|
| `shared/components/result-metric-card` | 137 + 81 + 266 |
| `features/results-display/savings-breakdown-card` | 169 (inline template) |
| `features/results-display/sail-indicator` | 71 (inline template) |
| `shared/components/form-select-field` | 55 + html + css |

`sail-indicator` and `form-select-field` are listed in a parent's `imports[]` array — Angular does
not warn about a standalone import that no template uses, and neither does the compiler, which is
why they survived. The `imports[]` entries go with them.

### 3. Symbols with no callers

- `shared/pipes/index.ts` — a 0-byte file, exported by nothing
- `CO2_EMISSION_FACTOR` — already marked `@deprecated`, zero uses
- `DEFAULT_VALUES.ANNUAL_HOURS`, `VALIDATION_LIMITS.ANNUAL_HOURS`
- `DEBOUNCE_TIMES.SEARCH`, `DEBOUNCE_TIMES.RESIZE`
- `AppDataService.getCurrentData()`, `AppDataService.getAppData()`
- `OperationalModesSectionComponent.operationalProfileLoaded` — an `@Output` no template binds;
  the parent binds the *vessel-config* section's event, which is a different one
- `OperationalModesSectionComponent.componentsLoaded` — set to `true` in `ngOnInit`, never read
- `OperationalModesSectionComponent`'s `appDataService` injection and its `takeUntil` import
- `app.routes.ts` (an empty `Routes`), `provideRouter` — there is no `<router-outlet>` anywhere
- the `cypress-run` / `cypress-open` / `ct` / `e2e` targets in `angular.json` —
  `@cypress/schematic` is not installed, so all four are broken today

### 4. `pendingEngineConfig`

C‑E deferred this here. `setEngineConfiguration` / `setEngineTypeReferences` only stash a pending
config when the engine arrays are empty; both callers run from a vessel-config response, which
cannot arrive before the catalogue that fills those arrays — the fetch is downstream of it and
debounced 400 ms behind.

**Delete the branch, and pin the invariant it relied on with a spec** rather than an argument: after
the catalogue is answered, both engine lists are populated before the first vessel-config request is
even issued.

### 5. Redundant event payloads

Three `@Output`s carry data the consumer never reads. Narrowed to `EventEmitter<void>` — the event
itself is load-bearing, only its cargo is not:

- `ProfileManagerComponent.saveRequest` — the parent calls back into the child, which re-reads its
  own `newProfileName`
- `WeatherInputSectionComponent.weatherChanged` — the parent ignores the payload entirely; the
  `sailEnabled` form control is what actually reaches the request (design §7.3)
- `OperationalModesSectionComponent.onDPModeToggle(event)` — the method reads `this.dpModeEnabled`,
  not the event

### 6. Log, do not fix

`weather-input-section.onSailToggle` assigns `this.sailContribution = null` — a child writing to its
own `@Input()`, i.e. to parent-owned state. Design §7.2. Behaviour-visible; not touched here.

## Acceptance Criteria

1. **AC1 — Backend frozen (I1).** 441/441, `Golden/Expected/` clean.
2. **AC2 — Strictness on.** `noUnusedLocals` and `noUnusedParameters` are `true` and
   `ng build --configuration production` is green with zero TS errors.
3. **AC3 — Everything in §2 and §3 is gone**, verified by `grep` returning no hits.
4. **AC4 — The suite still passes**, plus a new spec pinning §4's invariant.
5. **AC5 — I2 holds.** All three frozen request bodies unchanged.
6. **AC6 — The bundle shrinks.** Record the before/after figures; deleting ~800 lines and the
   router must show up.
7. **AC7 — Lint does not grow.** It will not reach zero here — that is C‑C.

## Tasks / Subtasks

- [x] Task 0: Baselines — backend, client suite, lint count, bundle size.
- [x] Task 1: Strictness on; fix the ten errors properly.
- [x] Task 2: Delete the four unrendered components and their `imports[]` entries.
- [x] Task 3: Delete the unused symbols in §3.
- [x] Task 4: Remove `pendingEngineConfig`; add the invariant spec.
- [x] Task 5: Narrow the three event payloads.
- [x] Task 6: Verify every AC; record the bundle delta.

## Dev Notes

- **A `_`-prefix rename is not a deletion.** `trackBy(index, item)` genuinely needs the positional
  parameter to keep Angular's signature — rename those. Everything else that is unused should stop
  existing.
- Removing `provideRouter` drops `@angular/router` from the bundle. If any Material component turns
  out to inject `Router` non-optionally, the suite fails loudly — put it back and log it rather than
  working around it.
- `getCurrentData()`/`getAppData()` are public API on a service. They have no callers in this
  repo and this repo is the only consumer.

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (claude-opus-5[1m])

### Completion Notes

**942 lines deleted, 77 added, across 28 files. 45/45 client specs green, 441/441 backend.**

- **The production bundle dropped out of its own error budget.** Initial total 1.27 MB → **1.20 MB**;
  `main` 977.57 → **910.65 kB**. The build had been emitting
  *"bundle initial exceeded maximum budget"* on every run — that warning is **gone**, not raised.
  Roughly two thirds of the saving is `@angular/router`, which was in the bundle to serve an empty
  route table.
- **Lint fell 189 → 169** without a single lint fix being made. Every one of those twenty problems
  lived in code that had no reason to exist. This is the argument for running C‑D before C‑C,
  and it held.
- **`pendingEngineConfig` was deleted and replaced with a proof, not an assertion.**
  `catalogue-precedes-vessel-config.spec.ts` asserts the two facts the branch depended on: no
  vessel-config request can be issued before the catalogue is answered, and both engine lists are
  populated the instant it is. If that ordering ever changes, the spec fails — instead of engine
  ratings silently reverting, which is the failure mode this whole epic started with.
- **Three `@Output`s were carrying cargo nobody read.** `saveRequest` duplicated a name the parent
  immediately handed back; `weatherChanged` duplicated three form controls the parent ignored
  outright; `onDPModeToggle` took a `MatCheckboxChange` and read `this.dpModeEnabled` instead. All
  three are now `EventEmitter<void>` — the events matter, the payloads did not.
- **`OperationalModesSectionComponent` lost its `OnInit`, `OnDestroy`, `Subject`, `takeUntil` import
  and `AppDataService` injection.** It had a `destroy$` nothing unsubscribed from, a
  `componentsLoaded` flag nothing read, and an `operationalProfileLoaded` `@Output` no template
  bound — the parent binds the *vessel-config* section's event of the same name. Two identically
  named events, one of them inert, sitting inside the cascade this epic exists to untangle.
- **A near-miss worth recording.** `VALIDATION_LIMITS` and `DEBOUNCE_TIMES` live in
  `defaults.constants.ts`, not in `calculator.constants.ts` as the file names suggest.
  `calculator.constants.ts` held nothing but the dead `CO2_EMISSION_FACTOR`. A first attempt
  rewrote the wrong file and would have exported both symbols twice; caught before running
  anything. The file is now deleted outright rather than emptied.
- Nothing was committed.

### Debug Log References

| Measurement | Before | After |
|---|---|---|
| Client specs | 43 green | **45 green** (2 new invariant specs) |
| Backend | 441/441 | **441/441**, `Golden/Expected/` clean |
| Lint | 189 (150 errors, 39 warnings) | **169 (132 errors, 37 warnings)** |
| `main` chunk | 977.57 kB | **910.65 kB** |
| Initial total | 1.27 MB (**over budget**) | **1.20 MB** (within budget, no warning) |
| Transfer size | 272.86 kB | **256.73 kB** |

Explicit AC verification:

- **AC1** — 441/441; `git status --porcelain KSailCalc.Tests/` → empty.
- **AC2** — `noUnusedLocals` and `noUnusedParameters` are `true`;
  `npx tsc -p tsconfig.app.json --noEmit` → no output.
- **AC3** — `grep` for every deleted selector and symbol → no hits.
- **AC4** — 45/45, including `catalogue-precedes-vessel-config.spec.ts`.
- **AC5** — `git status --porcelain cl/src/testing/golden/` → empty. The three frozen request
  bodies are untouched.
- **AC6** — bundle table above.
- **AC7** — lint went *down* by 20.

### File List

Deleted:
- `cl/src/app/shared/components/result-metric-card/` (ts + html + css)
- `cl/src/app/shared/components/form-select-field/` (ts + html + css)
- `cl/src/app/features/results-display/savings-breakdown-card/`
- `cl/src/app/features/results-display/sail-indicator/`
- `cl/src/app/shared/pipes/index.ts` (0 bytes)
- `cl/src/app/shared/constants/calculator.constants.ts` (only `CO2_EMISSION_FACTOR`)
- `cl/src/app/app.routes.ts`

Modified:
- `cl/tsconfig.json` — `noUnusedLocals`, `noUnusedParameters` → `true`
- `cl/angular.json` — the four broken `@cypress/*` targets and the schematic collection removed
- `cl/src/app/app.config.ts` — `provideRouter` removed
- `cl/src/app/core/app-data.service.ts` — `getCurrentData()`, `getAppData()` removed
- `cl/src/app/shared/constants/{index,defaults}.constants.ts` — `ANNUAL_HOURS` ×2,
  `DEBOUNCE_TIMES.SEARCH`, `DEBOUNCE_TIMES.RESIZE` removed
- `cl/src/app/shared/components/index.ts` — barrel rewritten
- `operational-modes-section` (ts + html), `weather-input-section`, `profile-manager`,
  `calculator-page` (ts + html), `vessel-input-form` (ts + html), `engine-config-section`,
  `vessel-config-section`, `power-demands-panel`, `validation-warnings`

New:
- `cl/src/testing/behaviour/catalogue-precedes-vessel-config.spec.ts` (2 specs)

### Change Log

| Date | Change |
|---|---|
| 2026-08-04 | C‑D implemented. 942 lines removed, `noUnusedLocals`/`noUnusedParameters` on, `pendingEngineConfig` deleted and its invariant pinned by spec, three event payloads narrowed. Bundle 1.27 → 1.20 MB (out of the error budget it had been breaching), lint 189 → 169, 45/45 green. Status → Ready for Review. |

## Risk Assessment

- **Primary:** something "unused" is reached from a template by a name `grep` did not match.
  **Mitigation:** the selectors were searched across `.html` and `.ts`; `strictTemplates` is on, so
  a missing component in a template is a build error, not a runtime surprise.
- **Secondary:** `pendingEngineConfig` turns out to be reachable in an ordering no spec covers.
  **Mitigation:** the replacement spec asserts the invariant directly rather than trusting the
  argument for it.
- **Rollback:** single commit, `git revert`. No backend, no wire-contract change.
