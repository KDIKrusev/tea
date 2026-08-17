# 40 — Others 500 kW in Transit: the crane's arithmetic under its new name

<!-- header:auto -->

> **Proves** · the **Others** row (Epic E2, D-BI4): battery demand of the non-DP modes, entered as
> a full kW that can start at any moment. This scenario exists because E2 left the mechanism with
> unit-test coverage only — scenarios 05/06 became inertness pins when Mission moved to DP.
>
> **Mechanics this scenario turns on**
> - `H` = the entered kW **as-is** (not average × factor) — like a heavy consumer, it is either
>   off or pulling its whole load.
> - Function PeakShaving, coverage **0.50**, **hotel side**, priority right after Mission (so in
>   Transit, where no Mission row exists, Others is served first).
> - The cascade continues below it: whatever budget is left flows to Propulsion, then Hotel.
>
> **Trust** · **proof by inheritance.** Inputs are scenario 05's, with `othersConsumerMaxKw: 500`
> where it had `missionHeavyConsumerMaxKw: 500`, and every figure below reproduces 05's
> workbook-verified snapshot **exactly**. The row changed its name and its mode list; its
> arithmetic did not.
>
> **Read after** · 01 (the same plant without the extra row), 41 (Mission in its new DP home).

## Inputs that matter

```
Excel plant: propulsion 11 463 (SM 0) · hotel 3 800 · transit 5 000 h
ME 2 × 24 000 · SG 3 250/engine · AE 4 × 4 000       battery 1 260 kW, Transit
Others 500                                           (no mission input — it would be inert here)
```

## The cascade

| Row | H | I | J | L |
|---|---|---|---|---|
| **Others** | **500** (full value) | 500 | **250** | 250 |
| Propulsion | 11 463 × 5 % = 573.15 | 573.15 | **200.6025** | 372.5475 |
| Hotel | 3 800 × 2 % = 76 | 76 | **3.8** | 72.2 |
| | | Σ 1 149.15 | **454.4025** | **694.7475** |

Budget left: **110.85** — everyone was served, which is what makes this the readable case
(scenario 06's 3 000 kW twin, where the row devours the whole budget, now lives in the Others
unit tests with the same numbers).

## Where it lands

Others is an electrical load, so its uncovered 250 goes to the **hotel** side:

```
propulsion' = 11 463 + 372.5475 = 11 835.5   hotel' = 3 800 + 250 + 72.2 = 4 122.2
AE 872.2 · ME 15 085.5 — the main engine never felt it
```

## The panel figures

```
Spinning Reserve 694.75 · Peak Shaving 454.40
Baseline FOC 13 590.7 t/yr · IL1 13 554.1 t/yr
Battery Benefit 422.25 t/yr
```

Compare with card 05: identical, digit for digit. That equality **is** the assertion — it says the
E2 rename moved the row without touching the model.
