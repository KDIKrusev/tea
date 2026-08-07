# 28 — Transit + Maneuvering

<!-- header:auto -->

> **Proves** · Transit plus Maneuvering — a second mode that has both propulsion and hotel.
>
> **Mechanics this scenario turns on**
> - Every active mode runs its **own** Level 1 — own demand, own combinations, own baseline, own optimum, own t/h. There is no single "tonnes per hour" for the vessel; the year is `Σ (mode t/h × mode hours)`.
> - Level 2 and Level 3 are computed for **Transit only** (decision D4/Q5 — no workbook counterpart elsewhere). Other modes get an empty result, and the pinned-baseline radio does not reach them.
> - With several modes the Power Demands tables gain one row per mode, and the header is an **hours-weighted average** (total energy ÷ total hours), not a sum.
> - Savings are a **difference**, so a mode whose baseline equals its optimum contributes zero — it is present on both sides and cancels, not excluded.
>
> **Panels described below** · What makes it distinct · The numbers · Why 800 hours matters to the annual figure
>
> **Anything not described here** — the mechanics above name the step that produced it; `00-ORIENTATION` Part 6 has the full number-to-step index.
>
> **Trust** · characterisation snapshot, generated from the code. It detects change; it does NOT prove correctness. Figures marked *pending reference verification* have never been checked against anything outside the application.
>
> **Read after** · scenario 07.

Maneuvering is the **only mode besides Transit and DP that carries propulsion**, and it had never
been tested alone.

## What makes it distinct

```
                 propulsion source          sea margin
Transit          PropulsionPower            applied (×(1 + SM/100))
DP               RequiredDPPowerKW          not applied
Maneuvering      ManeuveringPropulsionPowerKW   not applied
Port / Anchor    none                       —
```

The sea margin asymmetry is the thing to remember: a user who enters 6 000 kW of maneuvering
propulsion gets 6 000 kW, while 6 000 kW of transit propulsion at 15 % margin becomes 6 900.

## The numbers

Scenario 03's vessel plus **800 h maneuvering, 6 000 kW propulsion, 2 500 kW hotel**.

```
Transit       5 000 h   propulsion 12 036.2   SG 3 250.0   AE 626.0
Maneuvering     800 h   propulsion  6 000.0   SG 2 500.0   AE   0.0
```

Baseline **14 840.3 t/yr** · L1 savings **218.29 t/yr**.

Note the maneuvering row: the 2 500 kW hotel load fits inside the shaft generators' 3 250 kW, so the
aux fleet stays off and the main engine carries propulsion **plus** the shaft load — 8 500 kW total
on a 24 000 kW engine.

## Why 800 hours matters to the annual figure

Maneuvering is short but power-dense. Here it is 14 % of the operating hours and adds roughly 7 % to
the annual baseline over scenario 03 — a share large enough that leaving the mode untested was a real
gap, not a formality.

**Takeaway:** three modes carry propulsion and only one of them applies the sea margin. That is easy
to get wrong when adding a fourth.
