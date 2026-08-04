# 23 — HFO on Both Engines

Scenario 19's plant with both fuels set to **HFO** and the fuel price at its default **$420/ton**.

## What it guards

`FuelCo2Factors` carries five fuels. Before this scenario only MDO/MGO (3.93267) and LNG (2.753)
had end-to-end coverage — **HFO (3.114) and Ammonia were only ever constants in a config file.**

The plant is deliberately the no-SG one from 19, so the **aux side burns fuel too**. On the original
Excel plant the shaft generator covers the whole hotel load and `baselineAE` is zero, which means the
aux fuel factor is never applied and a regression there would go unseen.

## Verified figures

```
baseline FOC   8 861.6 t/yr   =  ME 6 880.9  +  AE 1 980.7
baseline CO2  27 595   t/yr   =  ME 21 427   +  AE  6 168
```

**Hand-check**

```
ME : 6 880.9 × 3.114 = 21 427   ✔
AE : 1 980.7 × 3.114 =  6 168   ✔
sum                  = 27 595   ✔  equals baselineCO2
```

Both engines use the same factor here, so the sum also equals `totalFOC × 3.114` — which is exactly
the legacy single-constant behaviour. That is the point: **HFO on both sides must collapse to the
old arithmetic**, and 24 then proves it does *not* collapse when the fuels differ.

**Takeaway:** the cheapest fuel per ton is not the cleanest per ton — HFO is 21 % below MDO on CO2
while costing 46 % less. The model reports both independently.
