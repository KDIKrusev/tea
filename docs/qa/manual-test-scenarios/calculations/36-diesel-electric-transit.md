# 36 — Diesel-electric transit: 0 main engines, the AEs carry everything

<!-- header:auto -->

> **Proves** · the diesel-electric plant family (Epic E1): at `meCount = 0` the whole demand —
> propulsion and hotel — is one electric load on the auxiliary engines. And, unexpectedly but
> correctly: **Level 2 is live on such a plant** (unequal genset dispatch).
>
> **Mechanics this scenario turns on**
> - Distribution at 0 ME: `AE power = hotel + propulsion × (1 + ElectricPropulsionLossFactor)`;
>   the factor is configuration (`CalculatorSettings:ElectricPropulsionLossFactor`, default 0).
> - The 90 % AE cap polices the whole electric load, not just hotel.
> - Level 2 sweeps **unequal** splits across the active AEs — with everything on the aux side it
>   has real room to work (this suite's only other non-zero L2 is scenario 19).
>
> **Trust** · characterisation snapshot, generated from the code. It detects change; it does NOT
> prove correctness. All figures *pending reference verification*.
>
> **Read after** · 01 (the conventional pipeline), 19 (Level 2's other non-zero case).

## Inputs that matter

```
propulsion 8 000 (SM 0)   hotel 3 000   transit 5 000 h
ME 0 (capacity 0, type id 0 — the parked form sends exactly this)   SG 0
AE 4 × 4 000 (id 8)   no battery   MGO aux fuel, price 780
```

## Level 1 — the survivor space collapses to one row

Electric demand = 3 000 + 8 000 × (1 + 0) = **11 000 kW**.

| AEs on | capacity | load | verdict |
|---|---|---|---|
| 1–2 | ≤ 8 000 | — | cannot carry 11 000 |
| 3 | 12 000 | **91.7 %** | rejected — above the 90 % cap |
| 4 | 16 000 | **68.75 %** | the only valid combination |

```
FOC = 11 000 × SFOC₈(68.75 %) / 1e6 = 2.140875 t/h → × 5 000 h = 10 704.375 t/yr
```

One survivor ⇒ baseline = optimal ⇒ **IL1 savings 0** — the familiar clamp behaviour
(scenarios 04/14), reached by a new road.

## The tier chips

| Tier | Adds | Cumulative savings |
|---|---|---|
| IL1 | nothing (single combination) | 0 |
| IL2 | **41.675 t/yr** — the unequal AE split beats Level 1's equal split | 41.675 |
| IL3 | DRC on the vessel-type variation (OSV → ±500 default) | **68.735** |

**The IL2 line is the point of this scenario.** On a conventional plant Level 2 redistributes
hotel between SG and AEs; with no SG that reads like "nothing to do". It is not — Level 2 also
chooses *how unequally* the running AEs share their load, and on a diesel-electric plant that is
the entire dispatch question. Design §8.3 originally assumed L2 would be empty here; the DE-B
characterization test disproved it, and this snapshot pins it in production numbers.

## Power Demands

```
Main Engine 0 kW · Shaft Generators 0 kW · Auxiliary Engines 11 000 kW  (AE load 68.75 %)
```

The ME row shows an honest zero — cosmetic labelling deliberately deferred (D-DE5).
