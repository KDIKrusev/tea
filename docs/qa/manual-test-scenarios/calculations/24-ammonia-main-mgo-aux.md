# 24 — Ammonia Main + MGO Aux: an 11× factor gap on one vessel

The widest CO2 spread the model can produce, on scenario 19's plant so both engines burn.

```
Ammonia  0.35154 kg CO2 / kg fuel
MGO      3.93267
ratio    11.2 ×
```

## Why this is the strongest fuel guard in the suite

Epic 3 replaced a single `Co2Factor` with a per-engine lookup. A regression that quietly falls back
to one constant would be invisible on every MDO/MGO scenario — the two factors are identical. It
would also be nearly invisible on 13 (LNG/MGO, 2.753 vs 3.93267, a 30 % gap).

Here it is unmissable.

## Verified figures

```
baseline FOC   8 861.6 t/yr   =  ME 6 880.9  +  AE 1 980.7
baseline CO2  10 208   t/yr   =  ME  2 419   +  AE  7 789
```

**Hand-check**

```
ME : 6 880.9 × 0.35154 =  2 419   ✔
AE : 1 980.7 × 3.93267 =  7 789   ✔
sum                    = 10 208   ✔
```

Note the inversion: the main engine burns **3.5× more fuel** than the aux side yet emits **3.2× less
CO2**. A single-constant model cannot produce that shape at all — if this snapshot ever shows
`baselineCO2 ≈ totalFOC × one factor`, the per-engine path has regressed.

Fuel price is Ammonia's default **$1 350/ton**, the most expensive in the table — so the cost panel
and the CO2 panel move in opposite directions, which is the real-world trade this feature exists to
show.

**Takeaway:** cheapest ≠ cleanest, and the model must keep the two axes independent. This scenario
fails loudly if they are ever collapsed.
