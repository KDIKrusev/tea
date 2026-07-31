# 02 — Small Battery 300 kW (budget exhausts mid-cascade)

Same vessel as 01; only battery power 1260 → **300**.

## Battery Contribution

| Row | H | I | J | L | Budget after |
|---|---|---|---|---|---|
| Propulsion | 573.15 | min(300, 573.15) = **300** | 300×0.35 = **105** | **468.15** | **0** |
| Hotel | 76 | min(0, 76) = **0** | **0** | **76** | 0 |

Tiles: **PS = 105 · SR = 544.15**. Invariant check: 105 + 544.15 = 649.15 — same total swing as 01;
the smaller battery only moved the split toward the gensets.

## Power Demands

Propulsion' = 11 463+468.15 = **11 931** · Hotel' = 3 800+76 = **3 876** (hotel got nothing!)
SG 3 250 · AE **626** · ME = **15 181** (63.25 %) · Energy 75 905 750 / 3 130 000.

## Baseline & IL1

Same 5 combos at slightly higher loads: optimal 2.67423 → IL1 **13 371.2 t/yr**; baseline
(3rd-from-worst) 2.67850 → **13 392.5**; savings **21.4 t = $16 664**.

## Battery Benefit

World B is IDENTICAL to test 01's (the battery doesn't exist there): 2.69198 t/h.
Benefit = (2.69198 − 2.67423)×5 000 = **88.74 t/yr = $69 218**.

Cross-check with 01: IL1 rose by 13 371.2−13 286.2 = 85.0 t, and the benefit fell by
173.66−88.74 = 84.9 t — the fuel you stop saving is exactly the fuel you burn extra. Consistency ✓

**Takeaway:** priority matters (propulsion took everything, hotel starved), and battery value has
diminishing returns — the first kW of budget is worth the most.
