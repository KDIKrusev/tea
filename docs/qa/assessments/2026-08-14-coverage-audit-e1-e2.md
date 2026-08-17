# Coverage audit — Epics E1/E2, 2026-08-14

**Reviewer:** Quinn (Test Architect) · **Trigger:** Kamen asked, after the manual pass, whether the
tests actually cover what the wave changed — with emphasis on the golden suite.

## What the audit found

Two holes, both in Epic E2, both material — and one of them was a **net loss** of coverage rather
than a missing addition:

1. **`othersConsumerMaxKw` appeared in no scenario.** The client's new input — the one manual
   testing exercised today — had no end-to-end pin at all. A wrong coverage factor, plant side or
   queue position would have passed the golden suite untouched.
2. **The Mission row lost every value-carrying scenario.** Scenarios 05 and 06 were its only ones,
   and since D-BI1 made Mission DP-only while their battery is Transit-only, both became proofs
   that the field is *ignored*. Nothing exercised a crane in DP — the client's actual case.

Also noted: the client had no spec for the field-visibility rule (Mission/DP-Redundancy shown only
with DP), verified manually today but unpinned.

The E1 (diesel-electric) side audited clean: 36–39 cover distribution, DP, battery and the
capacity 400; the remaining gaps there (loss factor ≠ 0, the 90 % cap wiping the list) are
structurally unreachable as scenarios and are named in the coverage matrix.

## What was added

| # | Scenario | Closes |
|---|---|---|
| **40** | Others 500 kW in Transit (scenario 01's plant) | hole 1 — and reproduces 05's workbook figures **exactly**, so the Excel-verified arithmetic returns as end-to-end proof under the new row name |
| **41** | Mission crane 500 kW in DP (scenario 04's plant) | hole 2 — plus the clearest budget-rationing case in the suite: reserve takes 400 of 500, crane takes the leftover 100 |
| **42** | `meCount 0` + SG 500 + PTI 300 → 400 with both D-DE3 messages | the two blocking rules the UI makes unreachable by parking the fields |

Plus `cl/src/testing/behaviour/battery-input-fields.spec.ts` — three specs pinning the visibility
rule and the wire contract of the new field.

Snapshot generation was again additive only: `git diff` on `Expected/` showed the same 15 E2 files
as before this audit and **no new modifications**; the three new files are the only additions.

## Verification

- Backend **485/485** (was 476 + 3 golden scenarios + 3 contract/theory rows + reruns)
- Client **83/83** (was 80 + 3 new specs)
- Numbers checked against hand-derivations before approval: 40 vs the pre-E2 05 snapshot
  (identical), 41 vs the cascade computed by hand (400/400/400/0 · 500/100/50/450 · 30/0/0/30,
  tiles 50 / 480), 42 vs the two message texts verbatim.

## Left open, deliberately

- **Others in Port** — unit-tested (`OthersRow_ExistsInPort_ButNotInDp`), no scenario. Port +
  battery + Others would be a fourth file; the mechanism is identical to Transit's, so the
  marginal value is low. Named in the coverage matrix rather than hidden.
- **Battery panel spec has no Others row in its fixture** — rendering of a zero Others row is
  visible in every battery scenario's snapshot; the DOM spec uses the DpReserve fixture because
  its subject is the Covered-total wording.
- The provisional status of D-BI1..D-BI5 is unchanged by this audit: better coverage of a
  decision does not make the decision confirmed. If the client vetoes it, 40/41 move with it.
