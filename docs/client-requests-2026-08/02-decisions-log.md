# Client Requests 2026-08 — Decisions Log

Confirmed decisions carry the day they were made and by whom. Open items are listed at the bottom
and **block** their epic until resolved. Numbering continues the D-series style of
`docs/battery-feature-analysis/05-decisions-log.md` with a DE (diesel-electric) prefix.

## Confirmed (Kamen, 2026-08-13)

| # | Decision | Chosen option | Rationale / consequence |
|---|---|---|---|
| **D-DE1** | Scope of the 0-ME epic | **Installed MeCount = 0 only** ("vessels without main engines" — AEs + thrusters). "MEs off per mode" (Wärtsilä doc modes 6/9 on an ME-equipped vessel) is explicitly OUT — a possible future opt-in epic. | Per-mode ME shutdown changes the combination space for every existing input with PTI (goldens 08/09 would move). Installed-0 gates on `input.MeCount == 0` → goldens 01–35 untouched by construction. |
| **D-DE2** | Electric-drive loss (AE → converter → thruster) | **New config key `ElectricPropulsionLossFactor` in appsettings, default 0** (user enters demand at the switchboard). | No Excel authority for thruster losses; default 0 keeps the client's hand-checks against his workbook clean. Changing to e.g. 5 % (Excel I4 PTI precedent) is a config edit, no code. |
| **D-DE3** | Validation strictness at MeCount = 0 | **Blocking errors** for SG capacity > 0 and PTI > 0 (not silent zeroing). ME type + ME capacity become not-required. | Silent ignoring is the behaviour class that produced Finding 3 ("DP redundancy persists invisibly"). |
| **D-DE4** | 90 % `MaxAuxLoadFraction` cap at 0 ME | **Keep the cap** — it now applies to propulsion + hotel combined. | Spinning-reserve realism. Documented consequence: AE capacity must be ≥ (propulsion + hotel)/0.9 or the plant is infeasible. |
| **D-DE5** | Presentation at 0 ME | Values stay honest (ME 0 kW); **cosmetic "diesel-electric" label deferred to the end of the epic**. | `PowerDemandsBuilder` and client guards already handle 0 correctly; cosmetics must not block the model. |

## Logged limitations (not decisions to revisit now)

- **L2 at zero SG**: expected to return an empty redistribution; requires a characterization test
  in the epic (proof, not belief — lesson of Finding 5).
- **L3 DRC stays hotel-only** under diesel-electric; no Excel authority to extend DRC to the
  propulsion swing. Documented limitation.

## Open — blocking Epic 2 (battery input rows)

| # | Question | Options on the table |
|---|---|---|
| O-1 | Excel authority check: does the reference workbook include the Mission (crane) row in the Transit cascade, or only in DP? Scenarios 05/06/07 were verified against the workbook **with** Mission in Transit. | (a) Excel has it DP-only → our port over-generalized (bug per D4); (b) client is changing his model → requirement change superseding the workbook. Either way goldens 05/06/07 move. |
| O-2 | Golden unfreeze: which snapshots may be regenerated when Mission leaves Transit? | Explicit product decision required; `GOLDEN_UPDATE=1` stays forbidden otherwise. |
| O-3 | "Others" row semantics: each relevant non-DP mode receives the **full** value in its own cascade (modes never overlap in time — how the budget works today, scenario 07), or the value is **split** across modes? | Full-per-mode is consistent with the current model; split is a new rule. |
| O-4 | "Others" row: function (PeakShaving vs Reserve), coverage factor (new `appsettings.json` CoverageFactors entry), position in the priority queue, plant side (hotel, like Mission?). | — |
| O-5 | Which modes count as "remaining": Transit + Port only (battery modes are restricted to Transit/DP/Port per D4, `ValidationService.cs:14-15`), or does Others extend the battery to Anchor/Maneuvering? | — |

Consequences once O-1..O-5 are resolved: `BatteryLoadType` enum + client `functionLabel`, profile
schema v3 → v4 + import contract (`ScenarioImportContractTests` + the three client-only required
fields rule), Mission tooltip text, README/cards of affected scenarios, test23 customer note.
