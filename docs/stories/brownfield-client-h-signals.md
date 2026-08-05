# Story: Client H — Signals: the decision

<!-- Source: docs/refactoring/client-refactor-design.md §6.8 -->
<!-- Context: Brownfield refactoring of cl/. A decision story. Outcome: no code change. -->

## Status: Ready for Review

## Story

As **Kamen, who asked for an argument rather than a fashion**,
I want **the signals question answered with measurements from this codebase**,
so that **"we did not adopt signals" is a recorded decision with a date and a trigger for revisiting
it, not an omission**.

## Context Source

- Design §6.8 already argued against migrating *form state* to signals. This story tests the other
  half of that recommendation — the "contained win" of converting `CalculatorPageComponent`'s
  derived view state — and reports what the code actually shows.

## The measurement

Design §6.8 estimated the contained win as "eight manual `markForCheck()` calls disappear".
**That figure is wrong.** Counted across the client after C‑G:

| File | `markForCheck()` calls | Would signals remove them? |
|---|---|---|
| `engine-config-section` | 10 | **No** |
| `profile-manager` | 8 | **No** |
| `vessel-config-section` | 5 | **No** |
| `battery-config-section` | 4 | **No** |
| `calculator-page` | **2** | Yes |
| `weather-input-section` | 1 | **No** |
| **Total** | **30** | **2** |

The 28 that signals cannot touch are all in components whose state is an `@Input() parentForm:
FormGroup` — six components share one form. Angular 18 has no signal-based forms, so a `FormGroup`
remains an observable-and-callback object. Converting those components' *other* fields to signals
would leave the `markForCheck()` calls exactly where they are, because the calls exist for the form,
not for the fields.

The second candidate benefit was memoising `allResults`, a getter that builds a fresh object on
every read; the template reads it 10 times. Under OnPush this component runs change detection only
on an input change, an event, or one of those two `markForCheck()` calls — so the measured cost is
about ten object literals on the rare cycles that happen at all.

## The decision

**Do not adopt signals in this epic. No code change.**

The honest summary: the contained win is **2 of 30** `markForCheck()` calls and ten object
allocations per infrequent change-detection cycle. That is not nothing, but it is a fraction of what
§6.8 estimated, and it is bought with churn on the one component that orchestrates every
calculation — covered by 64 specs that would all need re-reading.

The reasoning from §6.8 stands and is reinforced rather than replaced:

1. **The bug this epic existed for was never a change-detection bug.** Every component was already
   OnPush, and the values on screen were correct at the instant they were written. What was wrong
   was *which writer wrote last* — and signals do not arbitrate that. C‑E fixed it with an explicit
   sequence, which is what the problem actually needed.
2. **`ReactiveFormsModule` is the state container here, and Angular 18 has no signal forms.**
   "Signals for state" on 18.2 means running two state systems side by side and syncing them — one
   more writer, in a codebase whose whole epic was about getting to one.
3. **18.2 has `toSignal`/`toObservable` and little else that matters.** No signal forms, no
   `linkedSignal`, no `resource()`. The ergonomics that justify the churn arrive in 19/20.

## When to revisit

This decision has an expiry, not an argument for never. Revisit when **both** hold:

- the app is on **Angular 19+ with signal-based forms available**, so the six form-driven components
  can drop `FormGroup` rather than wrap it; and
- that upgrade is being done for its own reasons, so signals ride along with a migration that is
  already happening rather than justifying one.

At that point the 28 untouchable `markForCheck()` calls become touchable, and the arithmetic
reverses. Until then, adopting signals here would mean paying the cost in the one place it is
cheapest to pay and hardest to benefit from.

Tracked in design §7 as the Angular 19/20 upgrade item.

## Acceptance Criteria

1. **AC1 — The question is answered with numbers from this repo**, not with general advice. ✓
2. **AC2 — The design document's incorrect estimate is corrected**, not quietly dropped. ✓
3. **AC3 — The decision carries a concrete condition for revisiting it.** ✓
4. **AC4 — No production change**, therefore backend, suite, lint and bundle all unchanged. ✓

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (claude-opus-5[1m])

### Completion Notes

The story was written expecting to implement the contained conversion described in §6.8. Counting
the `markForCheck()` calls first changed the answer: the estimate of eight was really two, because
the other 28 belong to the shared `FormGroup` that Angular 18 gives no signal-based alternative to.

Recommending against a change I had already planned is the right outcome of measuring first, and it
is recorded here rather than folded silently into C‑I.

**Nothing was changed. 64/64, 441/441, lint 0, bundle unchanged — trivially, because no file was
touched.**

### File List

Modified: `docs/refactoring/client-refactor-design.md` (§6.8 corrected).
Production: none.

### Change Log

| Date | Change |
|---|---|
| 2026-08-05 | C‑H closed as a decision. Measured: signals would remove 2 of 30 `markForCheck()` calls, not the 8 estimated — the other 28 belong to the shared `FormGroup`, which Angular 18 offers no signal alternative for. Recommendation: do not adopt, revisit on Angular 19+ with signal forms. No production change. Status → Ready for Review. |

## Risk Assessment

- **Primary:** "we decided not to" quietly becomes "we forgot to". **Mitigation:** the revisit
  condition above is concrete and testable — Angular 19+ *and* an upgrade already under way.
- **Rollback:** nothing to roll back.
