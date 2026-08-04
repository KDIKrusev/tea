# Story: Refactor I — Immutability, a truthful input contract, and Level 2's guarantees

<!-- Source: Architect review after R-H — the three rough edges named as "what you will still trip on" -->
<!-- Context: Brownfield. No calculation changes; golden snapshots frozen. -->

## Story

As a **developer stepping through Level 1 in a debugger**,
I want **values that belong to exactly one stage, an input model where every field is actually read,
and Level 2's guarantees written down as tests**,
so that **nothing changes under me while I look at it, and I can understand the densest algorithm
without reading its loop**.

## Scope and outcome

### I1 — `EngineCombination` is immutable

Was a mutable class written by three different places. Now a `record` built in stages, each returning
a new instance with `with`:

```
candidate (counts) → TryDistribute (powers) → TryApply (PTI) → WithFuelConsumption (fuel)
```

`Level1CandidateBuilder.TryEvaluate` → `TryDistribute` returning `EngineCombination?`;
`Level1PtiAssist.TryApply` returns the assisted combination or `null`; `CalculateFoc` →
`WithFuelConsumption` returning the priced combination.

Verified first that nothing relies on identity or equality: only two `BeSameAs` assertions, both
satisfied because the final instance is the one added to the list.

### I2 — Four fields deleted from `CalculatorInput`

Checked which properties the backend actually reads. `HotelLoad`, `SailInstalled` and
`DPWeatherCondition` had **zero** usages; `BatteryCapacity` had exactly one — a validation guarding a
field nothing else read.

Deleted `HotelLoad`, `SailInstalled`, `BatteryCapacity`. **Kept `DPWeatherCondition`** — deleting it
would foreclose QA finding #2 (the DP weather factor), and it is one line.

Wire-safe: the client keeps sending the removed properties and JSON deserialization ignores them, so
saved profiles still load. The golden scenarios still carry all three in their JSON and the snapshots
did not move — which is the proof that the fields never mattered.

One acknowledged behaviour change: `batteryCapacity < 0` no longer produces a validation error.

Test `G4_LegacyBatteryCapacityField_HasNoEffectWhateverItsValue` was replaced rather than deleted:
the field's absence is now a structural guarantee, so the test asserts the half that still needs
proving — that an unknown wire property is ignored rather than rejected.

### I3 — Level 2 was NOT refactored; its invariants were written down

Deliberate decision: the recursive sweep is a cohesive ~100-line search, and splitting it would
scatter something that only makes sense whole. What makes an algorithm understandable is knowing what
it guarantees. `Level2InvariantTests` runs four genuinely different plants through it and asserts:

- every running auxiliary stays inside the 10–90% window
- an engine is either running or fully off, never idling below the minimum
- the running auxiliaries cover exactly the demand Level 1 assigned
- reported savings are never negative
- the shaft generator is passed through unchanged
- one setpoint per engine Level 1 had running
- no aux demand ⇒ Level 1 passes straight through

## What the invariant tests found

`Level2FocTonPerHour` can sit **fractionally above** `Level1FocTonPerHour` — measured 7.7e-07 t/h on
the tightest plant.

Not floating-point noise and not a defect. The sweep searches on a 2% grid; Level 1's own split is not
necessarily on that grid, so the best grid point can be a hair more expensive.
`Level2SavingsTonPerHour` is clamped with `Math.Max(0, …)` so the client can never see a Pro tier
costing more than Advanced, and `Level2FocTonPerHour` is **not on the wire** at all — `Level2Details`
exposes only `OptimalTotalSfoc` and the clamped savings.

My first assertion was stronger than the actual guarantee. I corrected the test to assert what is
guaranteed and added a second one that documents the grid artefact explicitly, with a tolerance
derived from one grid step rather than from the observed number. **Fixing the overshoot would mean
changing the algorithm** — out of scope, and not worth it for under a gram per hour.

## Verification

| Run | Result |
|---|---|
| Baseline (end of R-H) | 338/338 |
| After I1 (immutable record) | **338/338**, `Expected/` empty |
| After I2 (field removal) | **338/338**, `Expected/` empty |
| After I3 (invariant tests) | 362/363 — **one FAIL**, which found the grid artefact |
| After correcting the assertion | **367/367**, 0 warnings, `Expected/` byte-identical |

Golden snapshots unchanged across every step. `cl/` untouched by this story.
