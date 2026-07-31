# 04 — DP Redundancy 400 kW (the RESERVE function)

Two modes: Transit 5 000 h (11 463/3 800, NO battery there) and DP 2 000 h (thrust 2 500,
hotel 1 500, redundancy 400). Battery 500 kW, **DP only**.

## Battery Contribution (DP cascade, budget 500)

| Row | H | I | J | L | Note |
|---|---|---|---|---|---|
| DpReserve (RESERVE, D=1.0) | **400** (the FULL requirement) | 400 | **400** | **0** | covered kW-for-kW, zero factor loss |
| DpDemand (D=0.5, vf=0) | 2 500×0 = 0 | 0 | 0 | 0 | thrusters have no swing in the model |
| Hotel (D=0.05) | 1 500×2 % = 30 | 30 | 1.5 | 28.5 | budget left: 70 |

Tiles: **SR = 28.5 · PS = 1.5** (PS counts peak-shaving rows only; the reserve's 400 is full
coverage, not a ± band — Excel's J10 would show 401.5).

## Power Demands (two rows per table now)

- Transit: propulsion **11 463** (clean!), hotel 3 800 = SG 3 250 + AE **550**
- DP: propulsion **2 500**, hotel 1 500+28.5 = **1 529** — all via SG (the 400 is NOT in the
  demand: a reserve is readiness, not load)
- Energy: Transit ME (11 463+3 250)×5 000 = **73 565 000** · DP ME (2 500+1 528.5)×2 000 =
  **8 057 000** · AE 550×5 000 = **2 750 000**
- Header = hours-weighted: ME (73 565 000+8 057 000)/7 000 = **11 660** · AE 2 750 000/7 000 = **393**

## Baseline — two modes, two different rules

- Transit (no battery there) → default = last row: 2 ME+SG, 2.62547 → 13 127.3
- DP (battery active) → 3rd-from-worst clamped: only 2 combos ⇒ index 0 = its own optimal, 0.703744×2 000 = 1 407.5
- Baseline total = **14 534.8 t/yr**

## IL1

Transit optimal 2.578548×5 000 = 12 892.7 + DP 1 407.5 = **14 300.2 t/yr**.
Savings = **234.6 t = $182 996** — ALL from Transit (DP baseline = DP optimal after the clamp).

## Battery Benefit

DP only: the twin must spin 430 kW of reserve (400+30) vs our 28.5.
Benefit = ΔFOC × 2 000 h = **139.9 t/yr = $109 113**.

**Takeaway:** RESERVE ≠ PEAK SHAVING. A reserve wants the full amount, is covered 100 %, adds
nothing to demand when covered — the battery's strongest use-case (a class requirement satisfied
with zero extra spinning iron).
