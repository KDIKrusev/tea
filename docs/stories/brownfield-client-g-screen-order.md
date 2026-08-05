# Story: Client G — Screen order in the code

<!-- Source: docs/refactoring/client-refactor-design.md §5, §6.7 -->
<!-- Context: Brownfield refactoring of cl/. The structural story; C-A..C-F made it safe. -->

## Status: Ready for Review

## Story

As **Kamen, reading this client to find where a number comes from**,
I want **the code laid out in the order the screen is**,
so that **"the third panel down" is a file I can open, exactly the way `Services/Results/` works on
the backend**.

## Context Source

- Design §5 (target structure), §6.7 (this story).
- The backend's `Services/Results/` gave one builder per panel, numbered in render order. This is
  the client's version of that property.
- 50 specs, three frozen I2 request bodies and a zero lint gate now stand behind this. C‑G is a
  structural move — the safety case is that nothing on the wire changes.

## Scope

### 1. `vessel-input-form.component.ts` — 715 lines, four responsibilities

Extract three of them; the component keeps one.

| Extracted to | What moves | Why it is safe |
|---|---|---|
| `vessel-form.schema.ts` | the 72-line `fb.group(...)` | declaration only, no logic |
| `vessel-form.mapper.ts` | `buildCalculatorInput` + `buildBatteryInput` | **pure functions over data** — this is the client's `TierResultBuilder`, and the cheapest place to test invariant I2 |
| `profile-patch.ts` | the 45-line object literal in `applyProfileInputValues` | pure: `SavedProfile.input` → a form patch |
| `draft-autosave.service.ts` | the 30 s `setInterval` and its guards | side-effecting, but isolated |

The component is left with what it is actually for: the load sequence, wiring the sections, and
deciding when to emit.

**The arithmetic must not be retyped.** Every expression moves verbatim; only its location changes.
This is the same rule that governed backend story R‑A, and for the same reason.

### 2. `calculator-page.component.html` — the three tier panels are one panel

Levels 1/2/3 are 120 lines of near-identical markup differing in six values: icon, level name,
tooltip text, subtitle, CSS class and the result key. That becomes a `TIER_PANELS` table plus one
`*ngFor`.

**The `mat-expansion-panel` elements stay inside the page template.** `MatExpansionPanel` resolves
its accordion with `@Host()`, so moving a panel into a child component's template would silently
detach it from `<mat-accordion>`. An `*ngFor` keeps the panels in the same view, so the accordion
still sees them — the copy-paste goes, the DOM structure does not.

The three `advancedExpanded` / `proExpanded` / `premiumExpanded` fields collapse into one map keyed
by tier, which is what `toggleAllPanels` was already treating them as.

### 3. Render-order numbering

The results column gets numbered section comments in the order they appear on screen —
`01 Validation warnings · 02 Power demands · 03 Baseline · 04 Battery contribution ·
05 Report toolbar · 06 Integration levels ×3 · 07 Sail · 08 Tier comparison · 09 Charts ×2` — and
`results-display/` gets an index comment listing the same order.

### 4. The last two OnPush components

`vessel-input-form` and `operational-modes-section` become OnPush, the two
`eslint-disable-next-line` comments C‑C left behind are removed, and
`prefer-on-push-component-change-detection` is escalated from `warn` to `error`.

This is the change-detection change C‑C deliberately deferred. It is done **last**, after the
structural moves, so a failure is attributable.

### 5. Out of scope

Signals (C‑H). The `savings-chart` timers. Anything in design §7.

## Acceptance Criteria

1. **AC1 — Backend frozen (I1).** 441/441, `Golden/Expected/` clean.
2. **AC2 — I2 holds.** The three frozen request bodies are **byte-identical**. This is the whole
   safety case for §1: a pure-function extraction that changes a request has changed behaviour.
3. **AC3 — Suite green**, 50/50 plus new direct unit tests for the extracted pure functions.
4. **AC4 — `vessel-input-form.component.ts` is under 320 lines** and contains no form schema, no
   `CalculatorInput` construction and no `setInterval`.
5. **AC5 — `calculator-page.component.html` is under 300 lines** and contains exactly one
   integration-level panel block.
6. **AC6 — 25/25 OnPush**, both `eslint-disable` comments gone, the rule at `error`.
7. **AC7 — Lint zero, build green**, bundle not materially larger.

## Tasks / Subtasks

- [x] Task 0: Baselines.
- [x] Task 1: `vessel-form.schema.ts`. Suite green.
- [x] Task 2: `vessel-form.mapper.ts` + direct unit tests. Suite green, **I2 checked explicitly**.
- [x] Task 3: `profile-patch.ts`. Suite green.
- [x] Task 4: `draft-autosave.service.ts`. Suite green.
- [x] Task 5: The tier table + `*ngFor` in the page template; numbered sections.
- [x] Task 6: OnPush ×2, rule to `error`.
- [x] Task 7: Verify every AC.

## Dev Notes

- **Task 2 is the one that can silently change a number.** `buildCalculatorInput` is full of
  `x ? Number(x) : undefined` — a falsy-vs-nullish distinction that a "tidy-up" would break, since
  `0` is falsy and several of those fields legitimately hold `0`. Move the expressions **verbatim**.
  If a line looks wrong, log it in design §7; do not fix it here.
- `vesselTypeName` is read from the component, not the form. The mapper takes it as a parameter.
- The mapper's parameter type was `ReturnType<typeof this.vesselForm.getRawValue>`, which is
  `any`-ish. Give it a named `VesselFormValue` type — that is a readability gain, but it must not
  narrow anything: if a field turns out to hold a string where a number was assumed, that is a
  finding, not a fix.
- OnPush on `vessel-input-form` is the highest-risk item in the story, which is why it is last.
  The `@ViewChild` sections mutate the shared `FormGroup` from HTTP callbacks; the specs cover
  every one of those paths.

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (claude-opus-5[1m])

### Completion Notes

**64/64 client specs, 441/441 backend, lint zero, I2 bodies unchanged. Two ACs missed — reported
below rather than reworded.**

- **AC2 held at every step, which is the point.** The suite was run after each extraction and the
  three frozen request bodies never moved. That is the evidence that `buildCalculatorInput`,
  `profileToFormPatch` and `weightedAverageHotelLoad` were *moved* rather than rewritten.
- **The mapper is now directly testable, and 14 new specs use that.** They pin the thing most
  likely to be "tidied" into a bug: the falsy-vs-nullish asymmetry. `transitHours: 0` must vanish
  from the request while `trueWindSpeed: 0` must survive it, because `0 m/s` is a wind speed a user
  can choose and `0 hours` means "this mode is not used". Both rules are now asserted in one line
  each, instead of being reachable only by mounting a component tree and answering two HTTP calls.
- **Seven files came out of the 715-line component**, four of them pure:
  `vessel-form.schema.ts` (86) · `vessel-form.mapper.ts` (112) · `profile-patch.ts` (59) ·
  `operational-hours.ts` (47) · `vessel-variation.ts` (27) · `draft-autosave.ts` (90) ·
  and `rebaseline()` folded fifteen hand-written `updateOriginalValue` pairs into one call.
- **`DraftAutosave` is a plain class, not an `@Injectable`** — and that was a correction. It started
  as a component-provided service, which tripped `use-injectable-provided-in`. Configuring an
  exception for the rule did not work (the option is not honoured by this version), and
  `providedIn: 'root'` would be wrong: one shared timer outliving the form it saves. The right
  answer was that it never needed to be injectable — it has one dependency, and the component
  already holds it. `grep eslint-disable cl/src` stays empty.
- **The three level panels are one `*ngFor` over `TIER_PANELS`** — 124 lines → 50 — and the
  `mat-expansion-panel` stayed in the page template on purpose, because `MatExpansionPanel`
  resolves its accordion with `@Host()` and a child component's template would have detached it.
  The repeated inline `style=""` attributes became four CSS classes.
- **21/21 components are OnPush** and the rule is now `error`.

**AC4 — MISSED. `vessel-input-form.component.ts` is 428 lines, not under 320.** 715 → 428 is a 40 %
reduction, and everything the AC named as forbidden is gone (no schema, no `CalculatorInput`
construction, no `setInterval`). What remains is roughly 100 lines of imports, documented interfaces
and fields, and ~330 of method bodies: the lifecycle, the load sequence, `onVesselDataApplied` and
the emission rules.

Reaching 320 would mean splitting `onVesselDataApplied` — the five numbered steps C‑E deliberately
brought into one place so no step has to guess whether another is coming. Undoing that to hit a line
count would trade the epic's central fix for a number. **The AC was set optimistically; it is being
reported as missed rather than met by damaging the thing it was meant to protect.**

**AC5 — MISSED. `calculator-page.component.html` is 323 lines, not under 300.** 395 → 323. The tier
collapse delivered its 74 lines; the rest of the file is the header, error/validation blocks, the
two-column layout, six panel shells and the footer, none of which is duplication. Splitting it
further needs the `@Host()` constraint above to be worked around, which is a bigger change than
this story should carry.

Both misses are structural facts, not effort. Recorded in design §6.7 so the next story starts from
the real numbers.

Nothing was committed.

### Debug Log References

| Step | Client suite | Note |
|---|---|---|
| Baseline (after C‑F) | 50 green | component 715, template 395 |
| Schema + mapper + profile-patch + draft extracted | **50 green** | **I2 bodies unchanged** — the extraction is behaviour-preserving |
| 14 mapper unit specs added | **64 green** | first run, no failures |
| Tier table + `*ngFor` + numbered sections | **64 green** | |
| OnPush ×2, rule → `error` | **64 green** | one new lint error surfaced, see `DraftAutosave` above |
| `operational-hours` + `vessel-variation` + `rebaseline` | **64 green** | |
| Final | **64 green** · backend **441/441** · lint **0** | |

| Measurement | Before | After |
|---|---|---|
| `vessel-input-form.component.ts` | 715 | **428** |
| `calculator-page.component.html` | 395 | **323** |
| OnPush components | 19 / 21 | **21 / 21** |
| `main` chunk | 911.58 kB | **909.39 kB** |
| `eslint-disable` comments | 2 | **0** |

### File List

New:
- `cl/src/app/features/vessel-input/vessel-input-form/vessel-form.schema.ts`
- `cl/src/app/features/vessel-input/vessel-input-form/vessel-form.mapper.ts`
- `cl/src/app/features/vessel-input/vessel-input-form/vessel-form.mapper.spec.ts` (14 specs)
- `cl/src/app/features/vessel-input/vessel-input-form/profile-patch.ts`
- `cl/src/app/features/vessel-input/vessel-input-form/operational-hours.ts`
- `cl/src/app/features/vessel-input/vessel-input-form/vessel-variation.ts`
- `cl/src/app/features/vessel-input/vessel-input-form/draft-autosave.ts`
- `cl/src/app/features/results-display/README.md` (the render-order index)

Modified:
- `vessel-input-form.component.ts` (715 → 428), `operational-modes-section.component.ts` (OnPush),
  `form-edit-tracker.service.ts` (`rebaseline` + `OPERATIONAL_PROFILE_FIELDS`),
  `calculator-page.component.ts` (`TIER_PANELS`, tier map, trackBy),
  `calculator-page.component.html` (395 → 323, numbered sections),
  `calculator-page.component.css` (tier header classes), `eslint.config.js` (rule → `error`)

### Change Log

| Date | Change |
|---|---|
| 2026-08-05 | C‑G implemented. Seven modules extracted from the 715-line form component (four of them pure, now directly unit-tested); the three integration-level panels collapsed into one `*ngFor` over a tier table; the results column numbered in render order with an index in `results-display/README.md`; the last two components converted to OnPush and the rule escalated to `error`. I2 bodies unchanged throughout. AC4 and AC5 missed on line count — reported, not reworded. Status → Ready for Review. |

## Risk Assessment

- **Primary:** a moved expression is retyped and a number changes. **Mitigation:** AC2 — three
  byte-identical request bodies covering battery, sail, multi-mode and a pinned baseline.
- **Secondary:** OnPush stops a panel updating. **Mitigation:** done last and alone, so `git diff`
  of that step is three lines.
- **Tertiary:** the `*ngFor` detaches the panels from the accordion. **Mitigation:** §2 — the
  panels stay in the page's own view; nothing moves across a component boundary.
- **Rollback:** single commit, `git revert`.
