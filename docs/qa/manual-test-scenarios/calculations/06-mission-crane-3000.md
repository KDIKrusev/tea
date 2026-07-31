# 06 — Mission Crane 3000 kW (budget devoured, plant reshuffles)

Test 01's vessel + **Mission Max = 3000** (battery 1260, Transit).

## Battery Contribution

| Row | H | I | J | L |
|---|---|---|---|---|
| Mission (D=0.5) | **3 000** | min(1260, 3000) = **1 260** (ALL of it) | **630** | **2 370** |
| Propulsion | 573.15 | min(0, …) = **0** | 0 | **573.15** |
| Hotel | 76 | **0** | 0 | **76** |

Tiles: **PS = 630 · SR = 3 019.2**. The crane, third in priority, drains the budget; everyone
below starves.

## Power Demands — the reshuffle

Hotel' = 3 800+2 370+76 = **6 246** — MORE than one SG (3 250) can carry. Options:
1 ME+SG+AEs (SG 3 250 + AE 2 996 — expensive aux running), or **2 ME + both SGs (6 500) + 0 AE**.

The optimizer picks the second: ME = 12 036.15+6 246 = **18 282** @ 38.1 %, **AE = 0**,
SG at 96.1 % (6 246/6 500). Energy ME 18 282.15×5 000 = **91 410 750**.

## Baseline & IL1

Sorted: **3.1095 (2/on/0 — now FIRST!)** / 3.1292 (1/1) / **3.1780 (1/2 — baseline)** /
3.2102 / 3.2210. The combo that was the WORST row in test 01 is now the best — the uncovered
reserve changed which configuration wins, not just how much fuel it burns.

- Baseline = 3.1780×5 000 = **15 890 t/yr** (AE 2 996/8 000 = 37.5 %)
- IL1 = 3.1095×5 000 = **15 547.3** · **Savings 342.7 t = $267 335** — 16× test 01's, because the
  candidate rows are now genuinely different machines, not ±1 idle AE.

## Battery Benefit & IL3

Benefit = covered 630 monetized: **631.3 t/yr = $492 407** — the ceiling for this battery
(630 = the whole 1260 × 50 %).
IL3 detail: the mission covered band (630, hotel side) fully absorbs the ±500 DRC variation →
Variation shows **0**, batteryShaved **500**, L3 adds **0** (anti-double-counting, rule Q4/D4).

**Takeaway:** the cascade runs BEFORE combination enumeration for a reason — uncovered reserve can
flip the optimal plant configuration entirely.
