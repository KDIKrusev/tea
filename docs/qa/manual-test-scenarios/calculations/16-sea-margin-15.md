# 16 — Sea Margin 15 %: Heavier Sea Swells the Cascade

<!-- header:auto -->

> **Proves** · Sea margin multiplies propulsion BEFORE the swing is computed, so the whole cascade grows.
>
> **Mechanics this scenario turns on**
> - Cascade per row: `H` = what it wants · `I = min(remaining budget, H)` · `J = I × CoverageFactor` (covered) · `L = H − J` (left to the gensets). Priority: DpReserve → DpDemand → Mission → Propulsion → Hotel. Invariant `ΣJ + ΣL = ΣH` — the sea sets the swing, the battery moves the split.
> - Peak-shaving `H` = average × VariationFactor (propulsion 5 %, hotel 2 %). CoverageFactor is a modelling assumption about how much of a swing a battery can realistically catch — propulsion 0.35, hotel 0.05. Both live in `appsettings.json`, not in code.
> - Only the **uncovered** part rejoins the demand: `propulsion' = propulsion + L`, `hotel' = hotel + L`. Covered power (`J`) is never subtracted from anything.
>
> **Panels described below** · The chain, number by number · Baseline & IL1 · Battery Benefit
>
> **Anything not described here** behaves exactly as in `01-excel-baseline` — same plant, same hours; only what this scenario changes is worked through below.
>
> **Trust** · verified against the reference workbook. These figures are proof.
>
> **Read after** · scenario 01.

Test 01's vessel with **SM = 15** — the only Vessel-section field that enters the FOC math
directly (type/size/speed only prefill the calm-water value).

## The chain, number by number

```
Effective propulsion = 11 463 × 1.15 = 13 182.45
Cascade:  H = 13 182.45 × 5 % = 659.1  →  I = 659.1  →  J = 659.1×0.35 = 230.7  →  L = 428.4
          Hotel unchanged: 76 → 3.8 / 72.2
Tiles:    PS = 230.7+3.8 = 234.5   ·   SR = 428.4+72.2 = 500.6
Demand:   propulsion' = 13 182.45+428.4 = 13 611  →  ME = 13 611+3 250 = 16 861 (header)
Energy:   16 860.9 × 5 000 = 84 304 398 kWh
```

## Baseline & IL1

Combos: 2.9577 / 2.9607 / **2.9619** (baseline) / 2.9625 / 2.9825 — the whole floor rose
~0.3 t/h vs test 01 (ME now at **70.3 %**). Baseline = **14 809.7 t/yr** · IL1 = **14 788.5** ·
savings 21.2. The ~1 500 t/yr jump vs test 01 is the price of the margin itself, before any
battery discussion.

## Battery Benefit

Bigger swing ⇒ bigger covered band (ΣJ 234.5 vs 204.4) ⇒ **199.7 t/yr = $155 742**.
Rougher sea makes the battery MORE valuable — the model derives it, no special rule needed.

**Takeaway:** mirror image of test 12 — wind shrinks the cascade and devalues the battery; sea
margin inflates it and appreciates the battery. Same chain, opposite sign.
