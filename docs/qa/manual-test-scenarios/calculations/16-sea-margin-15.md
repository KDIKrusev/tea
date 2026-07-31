# 16 — Sea Margin 15 %: Heavier Sea Swells the Cascade

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
