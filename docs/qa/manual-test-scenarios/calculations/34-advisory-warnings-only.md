# 34 — Advisory warnings on an otherwise valid input

<!-- header:auto -->

> **Proves** · Advisory warnings only — results are still computed and shown.
>
> **Mechanics this scenario turns on**
> - Validation runs **before** Level 1. A plant that cannot carry its load is rejected there, with a different message and a different code path from the Level 1 rejections.
>
> **Panels described below** · The two triggers · The response · Why both warnings carry `type: "battery"`
>
> **Anything not described here** — the mechanics above name the step that produced it; `00-ORIENTATION` Part 6 has the full number-to-step index.
>
> **Trust** · characterisation snapshot, generated from the code. It detects change; it does NOT prove correctness. Figures marked *pending reference verification* have never been checked against anything outside the application.
>
> **Read after** · scenario 17.

Warnings come in two severities and they behave completely differently:

```
Severity.Error    → promoted into ValidationResult.Errors → the request 400s
Severity.Warning  → advisory only → the request succeeds, the client shows a banner
```

Before this scenario only **two** advisory warnings had golden coverage (battery capacity, scenario
10; operating hours, scenario 14). This adds the remaining two, on one otherwise perfectly valid
input.

## The two triggers

```json
"battery": { "powerKw": 500, "capacityKwh": 1000, "relevantModes": [] },
"dpRedundancyRequirementKw": 400        // with dpEnabled absent
```

**Battery without modes.** `BatteryConfigurationInput.IsActive` requires `PowerKw > 0` **and** at
least one relevant mode. With an empty list the battery is inert — no cascade, no panel, no effect on
any number. A user who typed 500 kW and expected something to change gets told why nothing did.

**DP redundancy without DP.** The redundancy figure only participates in a DP-mode allocation. With
DP off it is stored, exported to the saved profile, and ignored. The warning says so.

## The response

```
200
baseline FOC 13 652.6 t/yr    (identical to scenario 03 — the battery really is inert)
warnings:
  [Warning] battery: Battery power is configured but no relevant modes are selected …
  [Warning] battery: DP redundancy requirement is set but DP mode is not enabled …
```

Note `batteryDetails` is **null**, not an empty panel — decision G2/B10, the same rule scenario 18
covers from the zero-hours angle.

## Why both warnings carry `type: "battery"`

They are grouped for the client's banner. If a future change needs to style them differently, the
type field is the thing to split — and this snapshot is where that change would show up.

**Takeaway:** the calculator answers even when part of the input is pointless, and says which part.
Silence would be worse than a wrong number here.
