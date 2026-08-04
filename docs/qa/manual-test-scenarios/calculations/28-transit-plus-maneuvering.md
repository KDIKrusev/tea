# 28 — Transit + Maneuvering

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
