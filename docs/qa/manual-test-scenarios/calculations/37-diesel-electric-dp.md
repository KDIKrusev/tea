# 37 — Diesel-electric DP: the class reserve rides on the auxiliaries

<!-- header:auto -->

> **Proves** · DP on a 0-ME plant (the Wärtsilä reference's "Mode 6: Diesel electric DP mode"):
> thrust is an electric load like everything else, and the uncovered part of a DP redundancy
> requirement raises the AE demand — with no PTI gate anywhere (PTI is refused at 0 ME by
> validation).
>
> **Mechanics this scenario turns on**
> - Per-mode cascade with a DP-only battery (as scenario 04) — but both L piles now land on one
>   plant side, the AEs.
> - DpReserve is covered 1:1 and excluded from Peak Shaving (the "Covered reads 0" story the
>   panel labels now explain — story COS-A).
>
> **Trust** · characterisation snapshot — *pending reference verification*.
>
> **Read after** · 04 (the conventional DP reserve), 36.

## Inputs that matter

```
Transit: propulsion 8 000 (SM 0), hotel 3 000, 2 500 h
DP:      thrust 1 000, hotel 3 500, 1 000 h, redundancy 800
battery 500 kW / 500 kWh — DP only          AE 4 × 4 000 (id 8)
```

## DP allocation (the battery's whole budget goes to the reserve)

| Row | H | I | J | L |
|---|---|---|---|---|
| DpReserve (Reserve, 1.00) | **800** | **500** | **500** | **300** |
| DpDemand | 0 | 0 | 0 | 0 |
| Mission | 0 | 0 | 0 | 0 |
| Hotel (0.05) | **70** | 0 | 0 | **70** |

Tiles: **Peak Shaving 0** (a covered reserve is readiness, not peak shaving — the row shows 500,
the total shows 0, and the totals label now says why) · **Spinning Reserve 370** (300 + 70).

## Where the uncovered part goes — all of it to the AEs

```
DP AE demand      = (1 000 + 300) + (3 500 + 70) = 4 870 kW
Transit AE demand = 3 000 + 8 000               = 11 000 kW → 2.140875 t/h (same plant as 36)
```

No shaft exists, so the thrust-side L (300) does not raise an ME figure — it raises the same AE
figure the hotel-side L (70) raises. One plant side is the entire diesel-electric simplification.

## Battery Benefit

```
92.63 t/yr · $72 251/yr   (× fuel price 780)
```

Two worlds as always (budget 500 vs budget 0), both optimised, per mode × hours. In world B the
AEs carry the full 800 + 70 instead of 300 + 70.
