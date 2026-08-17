# 41 — Mission crane 500 kW in DP: the client's actual crane case

<!-- header:auto -->

> **Proves** · the **Mission** row in its one remaining home (Epic E2, D-BI1: mission operations
> are a DP affair). Added because after E2 no scenario exercised Mission with a value at all —
> 05/06 carry one but the battery is Transit-only there, so the row no longer exists.
>
> **Mechanics this scenario turns on**
> - Mission `H` = the heavy consumer's full rating; coverage 0.50; hotel side.
> - **Priority bites:** the class reserve is served first and 1:1, so the crane gets only what is
>   left — the clearest demonstration in the suite of the queue actually rationing a budget.
> - A partially covered row still splits: `J + L = H` holds on every line.
>
> **Trust** · characterisation snapshot — the mechanism is the workbook's (verified in the
> pre-E2 scenario 06), the DP context is not. *Pending reference verification.*
>
> **Read after** · 04 (this plant without the crane), 40 (the non-DP twin row).

## Inputs that matter

```
DP: hotel 1 500 · thrust 2 500 · 2 000 h · redundancy 400     battery 500 kW, DP only
Transit: propulsion 11 463 (SM 0) · hotel 3 800 · 5 000 h     Mission 500
```

## The DP cascade — the queue rations 500 kW

| Row | Function | H | I | J | L |
|---|---|---|---|---|---|
| DpReserve | **Reserve** | 400 | 400 | **400** (1:1) | 0 |
| DpDemand | PeakShaving | 0 | 0 | 0 | 0 |
| **Mission** | PeakShaving | **500** | **100** | **50** | **450** |
| Hotel | PeakShaving | 1 500 × 2 % = 30 | 0 | 0 | 30 |
| | | | Σ **500** | | |

The reserve takes 400 of the 500 kW budget before the crane is looked at; the crane takes the
remaining **100**, of which half counts as covered. Nothing is left for hotel.

## The tiles, and why they read the way they do

```
Peak Shaving      50   ← the crane's J only; the reserve's 400 is deliberately excluded
Spinning Reserve 480   ← 0 (reserve) + 450 (crane) + 30 (hotel)
```

This is the scenario to point at when someone asks why Peak Shaving looks small next to a
battery that is fully committed: **committed ≠ peak-shaved**. The totals row on screen now says
so in words (story COS-A).

## Where the uncovered part goes

```
thrust' = 2 500 + 0 = 2 500          hotel' = 1 500 + 450 + 30 = 1 980
```

The crane is electrical, so its uncovered 450 raises the hotel demand, not the thrust.

## The panel figures

```
Baseline FOC 14 692.1 t/yr · Battery Benefit 156.69 t/yr
```
