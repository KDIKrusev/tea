# 29 — Saturated battery: the surplus does nothing

<!-- header:auto -->

> **Proves** · A battery budget past saturation: more power, identical result.
>
> **Mechanics this scenario turns on**
> - Past saturation extra battery power buys nothing: once `ΣH` is fully taken, the remaining budget has no swing left to cover.
> - Cascade per row: `H` = what it wants · `I = min(remaining budget, H)` · `J = I × CoverageFactor` (covered) · `L = H − J` (left to the gensets). Priority: DpReserve → DpDemand → Mission → Propulsion → Hotel. Invariant `ΣJ + ΣL = ΣH` — the sea sets the swing, the battery moves the split.
>
> **Panels described below** · The invariant under test · Verified, side by side · Why this is worth a scenario rather than a unit test · The remaining 9 350.9 kW
>
> **Anything not described here** — the mechanics above name the step that produced it; `00-ORIENTATION` Part 6 has the full number-to-step index.
>
> **Trust** · characterisation snapshot, generated from the code. It detects change; it does NOT prove correctness. Figures marked *pending reference verification* have never been checked against anything outside the application.
>
> **Read after** · scenario 10.

Scenario 01's vessel with the battery power raised from **1 260 kW to 10 000 kW**. Everything else
identical, which makes this a controlled comparison against 01.

## The invariant under test (INV-2)

The cascade hands the budget out row by row: `I = min(remaining, H)`. Once the budget exceeds the
**total swing ΣH**, every row takes everything it wants and the leftover is simply never used.

Beyond that point the covered band and the spinning reserve stop depending on the budget entirely.

## Verified, side by side

| | 01 (1 260 kW) | **29 (10 000 kW)** |
|---|---|---|
| Peak shaving ΣJ | 204.40 | **204.40** |
| Spinning reserve ΣL | 444.75 | **444.75** |
| Remaining, unused | 610.9 | **9 350.9** |
| Battery benefit | 173.66 t/yr | **173.66 t/yr** |

**Identical** on every line that matters. Only the unused surplus grows.

## Why this is worth a scenario rather than a unit test

The unit test `A5_SaturatedBudget_ReserveIndependentOfBudget_INV2` already asserts this at the
allocation layer. What it cannot show is that the invariant survives the **whole pipeline** — that a
battery eight times larger produces a byte-identical Level 1 result, the same tiers, the same CO2 and
the same benefit figure.

That is a commercially meaningful statement: past saturation, a bigger battery buys nothing in this
model. If someone later makes coverage scale with budget, this snapshot changes and the two numbers
diverge immediately.

## The remaining 9 350.9 kW

Worth reading as a diagnostic, not as waste. It says the vessel's total swing is 649.15 kW and any
battery above that is oversized **for peak shaving and spinning reserve**. Other reasons to buy one —
endurance, blackout recovery, harbour operation — are outside this model.

**Takeaway:** the calculator will honestly tell you a battery is too big. Scenario 29 makes sure it
keeps doing so.
