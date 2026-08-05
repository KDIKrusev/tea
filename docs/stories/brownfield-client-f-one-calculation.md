# Story: Client F — One calculation per intent

<!-- Source: docs/refactoring/client-refactor-design.md §6.6 -->
<!-- Context: Brownfield refactoring of cl/. Mostly delivered by C-E; this closes and gates it. -->

## Status: Ready for Review

## Story

As **a user waiting for the panels to fill in**,
I want **exactly one calculation per thing I actually did**,
so that **the results I look at are never a stale answer to a question I did not ask**.

## Context Source

- Design §6.6. C‑E's load sequence and value-dedup already took the restore path from three
  calculations to one; C‑B proved the first of those three was computed on the *vessel type's*
  plant rather than the user's.
- What is **not** yet asserted anywhere: the baseline re-pick path. It is the one calculation that
  is deliberately *silent* — no spinner, panels untouched — so a duplicate there is invisible in
  the UI and would only show up as a second request on the wire.

## Scope

### 1. The four intents, each worth exactly one calculation

| Intent | Where it enters | Silent? | Covered by |
|---|---|---|---|
| Cold start | `beginLoad('startup')` | no | `load-emits-once.spec.ts` ✓ |
| Restore a profile or draft | `beginLoad('restore')` | no | `load-emits-once.spec.ts` ✓ |
| Edit a field / size / speed / category | debounced `valueChanges` | no | `user-edits.spec.ts` ✓ |
| **Re-pick the assumed baseline** | `onBaselineIndexChanged` | **yes** | **nothing — this story** |

### 2. What the baseline path must guarantee

- Exactly **one** POST per re-pick.
- The `baselineIndex` on the wire is the one just picked.
- The request is `silent`: `isCalculating` stays false and the previously rendered results stay on
  screen until the answer arrives.
- Re-picking does **not** invalidate a restored pin by re-entering the `'user'` branch of
  `onFormChange` — the form is not touched at all by this path.
- Picking the *same* index twice still recalculates. The value-dedup in `emitFormValues` guards
  the form's emissions; the baseline path does not go through it, and must not start.

### 3. Out of scope

No production change is expected. If the specs pass on the current code, the story's outcome is the
gate, not a diff. If they do not, the fix belongs here.

## Acceptance Criteria

1. **AC1 — Backend frozen (I1).** 441/441, `Golden/Expected/` clean.
2. **AC2 — One POST per re-pick**, carrying the picked index.
3. **AC3 — Silent.** The re-pick does not clear the panels or raise the spinner.
4. **AC4 — A restored pin survives a re-pick and a subsequent re-pick.**
5. **AC5 — Repeating the same pick still calculates.**
6. **AC6 — Suite green**, I2 bodies unchanged, lint still zero.

## Tasks / Subtasks

- [x] Task 0: Baselines.
- [x] Task 1: `baseline-repick.spec.ts` covering AC2–AC5.
- [x] Task 2: Fix anything the specs expose.
- [x] Task 3: Verify AC1 and AC6.

## Dev Notes

- `recalculateBaseline` reuses `this.currentInput` — the *form values*, not a fresh read. That is
  intentional: a re-pick must not pick up a half-typed field. Assert it, do not change it.
- `requestCalculation` captures `selectedBaselineIndex` at queue time
  (`apiInput: { ...input, baselineIndex: this.selectedBaselineIndex }`). Two rapid re-picks must
  therefore produce two requests with *different* indices, the second cancelling the first through
  `switchMap` — not two identical ones.

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (claude-opus-5[1m])

### Completion Notes

**50/50 client specs, 441/441 backend. Zero production changes — the specs passed on the first run.**

That is the outcome the story's Risk Assessment anticipated: C-E's load sequence and value-dedup
had already made every intent produce exactly one calculation, and the baseline path was correct
because it never went near the form. The value of this story is the gate, not a diff — five specs
now stand between C-G/C-H and a silently reintroduced second request.

Two behaviours are now pinned that were previously only implied by reading:

- **A genuine field edit invalidates a pinned baseline.** This is a real product rule
  (`calculator-page.component.ts` clears the pin on any `'user'` emission), and it is now asserted
  rather than assumed. If it is ever meant to change, a spec fails and the change is deliberate.
- **Picking the same row twice recalculates.** The C-E dedup guards the form's emissions; the
  baseline path deliberately does not run through it, because re-picking is an explicit action
  rather than an incidental re-render. Asserted so a future "optimisation" cannot quietly break it.

Nothing was committed.

### Debug Log References

| Run | Result |
|---|---|
| Backend | 441/441, `Golden/Expected/` clean |
| Client, before | 45 green |
| Client, after | **50 green** — five new specs, all passing first run |
| Lint | 0 problems, unchanged |

### File List

New: `cl/src/testing/behaviour/baseline-repick.spec.ts` (5 specs)

Modified: none.

### Change Log

| Date | Change |
|---|---|
| 2026-08-05 | C-F closed. The baseline re-pick path — the only silent calculation, and the only one no spec covered — is now gated by five assertions. No production change was needed. Status → Ready for Review. |

## Risk Assessment

- **Primary:** the specs pass and the story looks empty. That is a legitimate outcome — C‑E did the
  work — but the gate is what stops C‑G or C‑H reintroducing a second request unnoticed.
- **Rollback:** test-only unless Task 2 finds something.
