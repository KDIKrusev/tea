# Story: Battery Inputs A — Mission DP-Only and the Others Row

<!-- Source: PRD v1.0 §7 (Epic E2, stories BI-A..BI-D consolidated); decisions D-BI1..D-BI5
     (provisional, Kamen 2026-08-13 — "следвай препоръките, после при нужда ще променяме") -->
<!-- Context: the client's request 2 — "Mission heavy consumer and DP redundancy are DP-only;
     a new Others field distributes battery demand to the remaining selected modes." -->

## Status: Done

## Story

As **the client whose model puts mission operations in DP**,
I want **the Mission row confined to DP mode and a new Others input carrying battery demand for
the remaining relevant modes (Transit/Port)**,
so that **the cascade matches how my vessel actually operates**.

## Scope (as implemented)

1. `BatteryAllocationService.GetLoadInputs`: Mission row `Transit or DP` → **DP only** (D-BI1,
   removed — not left as a zero row); new **Others** row in `Transit or Port` (D-BI5),
   `H = OthersConsumerMaxKw` as-is (mission-mirror semantics, D-BI4).
2. `BatteryLoadType.Others` enum value (hotel side — `IsThrustSide` unchanged);
   `CreateDefaultLoadPriorities` + `appsettings.json` gain the row right after Mission,
   PeakShaving, coverage **0.50** (all four knobs are configuration).
3. `CalculatorInput.OthersConsumerMaxKw?` + validation "Others battery demand cannot be
   negative" (appended — pinned 400 order preserved).
4. Client: `batteryOthersMaxKw` control + Others (kW) field; Mission field now rendered **only
   when DP mode is available** (alongside DP Redundancy — the client's model made visible);
   Mission hint rewritten ("DP mode only"); battery-disable reset includes the new control;
   mapper/profile round-trip (`othersConsumerMaxKw`, truthiness contract like its siblings —
   schema stays v3, field is optional).

## The golden unfreeze (D-BI2) — reviewed diff, 15 files

- **Numbers moved in exactly two:** 05 and 06 — their mission input is inert in Transit now, and
  both were verified **numerically identical to 01 to full precision** (baselineFOC, PS, SR,
  Benefit). Their cards carry SUPERSEDED banners; the Excel-verified arithmetic survives verbatim
  as the Others row and is re-pinned by unit tests with the original numbers
  (`OthersMax_VariationIsFullValue_AndOutranksPropulsion`, `H3_OthersMax_EndToEnd`).
- **Structural-only in the rest:** the zero `"Mission"` row renamed to `"Others"` in every
  battery-in-Transit table (±2 lines each: 01, 02, 08, 10, 11, 12, 13, 15, 16, 29, 30, 38), and
  Port tables gained the zero Others row (07: +12 lines). No other number moved anywhere.

## Acceptance Criteria → evidence

1. Mission value in Transit changes nothing → `MissionInTransit_IsInert_TheRowDoesNotExist` +
   cards 05/06 comparison tests vs 01 (1e-9).
2. Others carries the full-kW/0.50/hotel-side behaviour in Transit and Port; absent in DP →
   `OthersMax_*`, `H3_OthersMax_*`, `OthersRow_ExistsInPort_ButNotInDp`.
3. Mission keeps the full-value behaviour in its one remaining home →
   `MissionInDp_KeepsTheFullValueBehaviour` (new coverage pin — the old value-carrying mission
   tests all lived in Transit).
4. Config pinned → `BatterySettingsConfigurationTests` (6 rows, Others after Mission, 0.50).
5. Wire contract → mapper spec (300 mapped; absent/0 dropped, so pre-E2 frozen request bodies
   stay byte-identical); profile optionalNumbers accepts the field.
6. Suites: backend **476/476** · client **77/77** · `ng build` + `ng lint` clean.

## Dev Agent Record

- The Others row deliberately reuses Mission's exact arithmetic so the Excel-verified numbers
  from the old Transit-mission scenarios survive as regression pins under the new name — nothing
  Excel-proven was thrown away, it was re-homed.
- Docs: cards 05/06 banners, README sections rewritten, ORIENTATION (priority queue, H-table
  with the Others row, reverse index), test23 customer note banner (his own case's Transit
  numbers change — flagged before he reruns it), COVERAGE-MATRIX untouched this story except via
  the Approval-status text (already 19–39).
- **Provisional-decision watermark:** everything here sits on D-BI1..D-BI5; each names its revert
  lever in the decisions log. If the client's answers differ, the revert is one matrix line +
  config rows + a golden regeneration with the same diff-review discipline.

## QA Results

**Gate: PASS** — `docs/qa/gates/bi.a-mission-dp-only-and-others.yml` (Quinn, 2026-08-13).
