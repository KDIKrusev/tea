# 19 — Two Aux Engines: the only scenario that exercises Level 2

**Why it exists.** Level 2 produced **zero savings in all 18 original scenarios**. A whole
optimization level — the recursive sweep — had no end-to-end coverage: break it so it returns zero
and every snapshot would still pass.

## Why Level 2 was invisible

Read the aux SFOC curve (engine 8) and it falls monotonically:

```
10 % → 228.47      50 % → 198.07      74–80 % → 193.00      100 % → 194.63
```

More load is always better. Level 1 already evaluates every engine **count** and picks the cheapest,
so "run fewer engines harder" is a decision Level 1 has made before Level 2 is asked.

Level 2's only remaining lever is an **asymmetric** split — and that needs a demand a single engine
cannot legally carry.

## The plant, built for exactly that

```
ME 2 × 8 500 · no SG · AE 2 × 2 000 · propulsion 8 000 · hotel 2 000 · 5 000 h
```

- No shaft generator ⇒ the aux side must carry the whole 2 000 kW hotel load.
- 1 aux engine ⇒ 2 000 / 2 000 = **100 %** — above the 90 % ceiling, rejected.
- 2 aux engines ⇒ 50 % each. Legal, and squarely in the steep part of the curve.

## Level 1

Two valid combinations (ME = 0 is invalid in Transit, aux count is forced to 2):

| # | ME | SG | AE | FOC t/h |
|---|---|---|---|---|
| 0 | 2 | off | 2 | 1.734727 ← optimal |
| 1 | 1 | off | 2 | 1.772322 ← baseline |

Baseline **8 861.6 t/yr** · L1 savings **187.97 t/yr**.

## Level 2 — the point of the scenario

Level 1 handed the aux side an even split: 2 × 1 000 kW = 50 % each.
The sweep searches 10 %…90 % in 2 % steps and finds:

```
AE₁   200 kW  @ 10 %   sfoc 228.47
AE₂ 1 800 kW  @ 90 %   sfoc 193.78
```

**Hand-check**

```
even split : 2 000 kW × 198.07 g/kWh                    = 0.396140 t/h
asymmetric :   200 × 228.47  +  1 800 × 193.78          = 0.394498 t/h
difference × 5 000 h                                    ≈ 8.21 t/yr
reported                                                = 8.14 t/yr
```

Agreement to within rounding of the curve read-off. **The exact figure is pending reference
verification** — it came from the code, not the workbook.

## Result

| | |
|---|---|
| L1 | 187.97 |
| **L2** | **8.14** |
| L3 | 45.25 |
| Advanced / Pro / Premium | 187.97 / **196.11** / **241.36** |

The first scenario in the suite where all three tiers differ.

## Open question for the workbook

The model's answer is one generator idling at 10 % and one at 90 %. A crew would more likely run a
**single** generator at 100 % — which the 90 % ceiling forbids. If the workbook disagrees with 10/90,
the floor or the ceiling is mis-set. Worth confirming before this figure is treated as correct.

**Takeaway:** Level 2 only has room when a single engine cannot legally carry the aux demand. That is
a narrow window, and no scenario had ever landed in it.
