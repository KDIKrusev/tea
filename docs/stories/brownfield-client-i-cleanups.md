# Story: Client I — Cleanups and consistency

<!-- Source: docs/refactoring/client-refactor-design.md §6.9 -->
<!-- Context: Brownfield refactoring of cl/. The last story of the epic. -->

## Status: Ready for Review

## Story

As a **developer who will add the next feature to this client**,
I want **one way to tell the user something went wrong**,
so that **two failures in the same session cannot behave differently for no reason**.

## Scope

### 1. `NotificationService`

Sixteen `snackBar.open(...)` call sites; ten of them repeated
`{ duration: 5000, panelClass: ['error-snackbar'] }` verbatim. Nothing was wrong with any one of
them — the risk is drift: a duration or a panel class changing on one of ten and nobody noticing
until two snackbars behave differently on the same page.

Four named shapes, using the durations the call sites already had:
`error` (5 s, Close) · `success` (2.5 s) · `successDetailed` (3 s) · `acknowledge` (2 s).

### 2. Deliberately left alone

- **`report-dialog`'s pop-up-blocked message keeps its 6 s duration.** It asks the user to change a
  browser setting, which takes longer to read than "Profile saved". Normalising it to 5 s would be
  a behaviour change smuggled into a cleanup story; it stays a direct `snackBar.open` and this is
  the note saying why.
- **The three one-line `getValidationError` delegations** in `additional-config`, `battery-config`
  and `weather-input`. They already forward to `FormValidationService`. Collapsing three one-line
  methods would need a base class or a mixin — more machinery than the duplication costs.
- **`savings-chart`'s two `setTimeout(…, 100)` calls.** They defer canvas creation until after the
  view settles, run `outsideAngular`, and have nothing to do with the cascade timers C‑E removed.
  Touching them without a rendering test would be a guess.

### 3. Logged, not fixed

Two findings, both behaviour-visible. See design §7.11 and §7.12.

## Acceptance Criteria

1. **AC1 — Backend frozen.** 441/441, `Golden/Expected/` clean. ✓
2. **AC2 — Suite green**, I2 bodies unchanged. ✓
3. **AC3 — Lint zero, build green.** ✓
4. **AC4 — No behaviour change**; anything that would change one is logged in §7 instead. ✓

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (claude-opus-5[1m])

### Completion Notes

**64/64, 441/441, lint 0, I2 bodies unchanged. `main` 909.39 → 909.21 kB.**

- **15 of 16 snackbar call sites now go through `NotificationService`.** The sixteenth is documented
  above rather than normalised.
- **A near-miss worth recording.** The first attempt guarded "add the import if missing" with
  `grep -q NotificationService` — which matched the `inject(NotificationService)` line the same
  script had just written, so the import was skipped in both files. `tsc` caught it immediately
  (`TS2304`, plus two `TS2571`s that were only symptoms of it). A guard that tests for the wrong
  thing is worse than no guard; the compiler was the real check.

**Finding §7.11 — the engine section shows validation errors by a different rule than the rest of
the form.** `FormValidationService.getErrorMessage` returns `''` unless the control has been
**touched**; `EngineConfigSectionComponent.getValidationError` is a hand-rolled copy with the same
messages and **no touched check**. So a pristine, untouched engine field displays "This field is
required" while an equally pristine battery or weather field stays quiet. Unifying them changes what
the user sees on a freshly loaded page, which is behaviour — logged, not fixed.

**Finding §7.12 — `savings-chart` swallows chart failures silently.** Two `catch {}` blocks (made
explicit in C‑C) mean a chart that fails to render leaves an empty panel with no indication that
anything went wrong. Now that `NotificationService` exists, telling the user is one line — but it is
a new user-visible message, so it needs a decision rather than a commit.

Nothing was committed.

### Debug Log References

| Check | Result |
|---|---|
| Backend | 441/441, `Golden/Expected/` clean |
| Client suite | **64 green** |
| Lint | **0 problems** |
| Build | green, `main` 909.21 kB, initial total 1.20 MB |
| I2 bodies | unchanged |

### File List

New: `cl/src/app/shared/services/notification.service.ts`

Modified: `shared/services/index.ts`, `profile-manager.component.ts` (10 call sites),
`vessel-config-section.component.ts` (3), `engine-config-section.component.ts` (2),
`docs/refactoring/client-refactor-design.md` (§7.11, §7.12)

### Change Log

| Date | Change |
|---|---|
| 2026-08-05 | C‑I implemented. 15 of 16 snackbar call sites unified behind `NotificationService`; the sixteenth documented as a deliberate exception. Two new findings logged (§7.11 validation-message divergence, §7.12 silent chart failures). Status → Ready for Review. |

## Risk Assessment

- **Primary:** a message or duration changes while being "unified". **Mitigation:** each of the four
  methods uses a duration that already existed at its call sites; the one site that did not fit was
  left alone and documented.
- **Rollback:** single commit, `git revert`.
