# 38 — Diesel-electric with a Transit battery: the cascade lands on the AEs

<!-- header:auto -->

> **Proves** · the battery machinery is plant-shape-neutral: at 0 ME the cascade, the two
> Benefit worlds and the with-battery baseline rule all run unchanged — only the final carrier
> differs (AEs instead of ME + AEs).
>
> **Mechanics this scenario turns on**
> - Cascade rows and factors identical to a conventional plant (H/I/J/L, priority order).
> - `J + L = H` per row; world B = world A + the covered J, per side — both sides being the same
>   AE side here.
> - With-battery baseline `max(0, count − 3)` clamps on a one-row list ⇒ IL1 savings 0 while the
>   Benefit is large — the scenario-04/11 lesson in diesel-electric form.
>
> **Trust** · characterisation snapshot — *pending reference verification*.
>
> **Read after** · 01 (the same cascade on a conventional plant), 36.

## Inputs that matter

```
propulsion 10 000 (SM 0)   hotel 3 000   transit 5 000 h
battery 800 kW / 800 kWh — Transit        AE 4 × 4 000 (id 8)
```

## The cascade (unchanged arithmetic)

| Row | H | I | J | L |
|---|---|---|---|---|
| Mission | 0 | 0 | 0 | 0 |
| Propulsion (0.35) | 10 000 × 5 % = **500** | **500** | **175** | **325** |
| Hotel (0.05) | 3 000 × 2 % = **60** | **60** | **3** | **57** |

Tiles: **Peak Shaving 178** (175 + 3) · **Spinning Reserve 382** (325 + 57). Budget left: 240.

## World A on the auxiliaries

```
AE demand = (10 000 + 325) + (3 000 + 57) = 13 382 kW   → ae=4 @ 83.6375 %
FOC       = 2.586254 t/h → 12 931.27 t/yr
```

ae=3 (12 000 kW) cannot carry 13 382 ⇒ one survivor ⇒ the with-battery baseline
`max(0, 1 − 3) = 0` clamps to the optimum ⇒ **IL1 savings 0**.

## Battery Benefit

```
177.47 t/yr · $138 428/yr
```

World B carries the full swing: demand grows by exactly the covered J — 175 on the propulsion
side + 3 on the hotel side = **178 kW**, the Peak Shaving tile. The battery's value lives
entirely in the green badge here, not in the IL1 chip — same split of meaning as scenario 04,
new plant family.
