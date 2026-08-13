# Story: Cosmetics A — Battery Panel Labels (Variation ± and the Covered total)

<!-- Source: PRD v1.0 docs/client-requests-2026-08/03-brownfield-prd.md §5 (Epic E3, story COS-A) -->
<!-- Context: client request 3 (± on Variation) + the relabel promised to the client in
     docs/qa/customer-notes/test23-battery-explained.md §4 ("We accept this reads like a broken
     sum and will relabel the column"). Client-only; no API, no goldens. -->

## Status: Done

## Story

As **the client reading the Battery Contribution tables**,
I want **the Variation column to carry the same ± mark as Covered, and the Covered total to say
what it actually sums**,
so that **a ± band reads as a band, and a DpReserve allocation (row Covered 800, total 0) no
longer looks like a broken sum**.

## Scope

All in `cl/src/app/features/results-display/battery-contribution-panel/`:

1. Header `Variation (kW)` → `Variation ± (kW)` (same style as `Covered ± (kW)` —
   the client asked for the ± "as it is for covered").
2. The totals row label names its one asymmetry: the Covered total sums **peak-shaving rows
   only** (`BatteryModeAllocation.peakShavingBandKw`); reserve rows are excluded by design.
   A `title` tooltip on the total cell repeats the reason.
3. The panel footnote (`.battery-note`) gains one sentence explaining the exclusion (readiness,
   not peak shaving) — the wording follows test23-battery-explained.md §4.
4. First DOM spec for this component: pins both headers, the totals label, and the
   reserve-vs-total behaviour with a DpReserve fixture (row covered 800, PS total 0).

## Out of scope

- Any change to `peakShavingBandKw` semantics or any backend value (the tiles and the Excel port
  stay exactly as they are — the sum is correct, only its label was misleading).
- The Bulgarian client note (updated when the client confirms the new wording reads right).

## Acceptance Criteria

1. **AC1:** Allocation-table header reads `Variation ± (kW)`; `Covered ± (kW)` unchanged.
2. **AC2:** Totals row makes the peak-shaving-only scope of the Covered total visible without
   hovering; a tooltip carries the one-line reason.
3. **AC3:** With a Reserve-row fixture (DpReserve covered 800, peak-shaving total 0) the rendered
   DOM shows 800 in the row and 0 in the total — and the spec asserts the label that explains it.
4. **AC4:** `ng build` clean; full client suite green (68 existing + new specs); no API change.

## Tasks / Subtasks

- [x] Task 1: Template edits (header, totals label, tooltip, footnote sentence)
- [x] Task 2: `battery-contribution-panel.component.spec.ts` — DOM spec with DpReserve fixture
- [x] Task 3: `ng test` CI run + `ng build`; record counts in Dev Agent Record

## Dev Agent Record

- Implementation confined to `battery-contribution-panel.component.html` (three edits: header,
  tfoot label + tooltip, footnote sentence) + one new spec file. No TypeScript, no API, no
  backend, no golden exposure.
- The totals label carries the scope inline (`Totals (Covered: peak-shaving rows only)`) so AC2's
  "visible without hovering" holds; the tooltip and footnote carry the *why* (readiness vs peak
  shaving, wording from test23-battery-explained.md §4).
- Spec fixture is the test23 DP allocation verbatim (DpReserve 800/800/800/0, Hotel 70/0/0/70,
  PS total 0, SR 70) — the exact shape the client questioned.
- Test results: `npm run test:ci` → **72/72 SUCCESS** (1 skipped, pre-existing xit); suite grew by
  the 3 new specs. `npm run build` → clean, initial total **1.20 MB** (unchanged).

## QA Results

**Gate: PASS** — `docs/qa/gates/cosmetics.a-battery-panel-labels.yml` (Quinn, 2026-08-13).
AC trace 1–4 covered, no gaps. Follow-up logged: refresh the two test23 customer notes' wording
(they still promise the relabel in future tense) once the client confirms the labels.
