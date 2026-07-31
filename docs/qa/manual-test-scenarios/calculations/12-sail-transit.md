# 12 — Sail On: Wind 10 m/s @ 90° Shrinks the Cascade

Test 01's vessel + sail enabled, true wind 10 m/s at 90°, vessel 12.5 kn.

## The wind → cascade chain (test point B8)

```
Wind (speed+angle) + vessel speed → apparent wind → sail thrust = 539.93 kW
Sail-adjusted propulsion avg = 11 463 − 540 = 10 923 kW
Cascade uses the ADJUSTED value:  H = 10 923 × 5 % = 546.2   (was 573.15!)
  → I = 546.2 → J = 546.2×0.35 = 191.2 → L = 355.0
Hotel unchanged: 76 → 3.8 / 72.2   (wind doesn't shake the galley)
Tiles:  PS = 191.2+3.8 = 195   ·   SR = 355+72.2 = 427.2
```

## Power Demands (new column: SAIL REDUCTION)

ME would carry 11 463+355 = **11 818**; sail takes **−540** → net **11 278**.
ME total = 11 278+3 250 = **14 528** (header) · Energy 14 528.1×5 000 = **72 640 366**.
IL1 Optimization Details shows: "Sail Contribution: 11 463 → 10 923 kW · Sail Power 540 kW (4.7 %)".

## Baseline & IL1

All combo FOCs drop (lighter ME): 2.5631 … 2.6072; baseline (3rd-from-worst) 2.5673 →
**12 836.6 t/yr**; IL1 2.5631 → **12 815.4**; savings 21.2.

## Battery Benefit — and why it FELL

Canonical backend values at $780: **165.5 t/yr = $129 105**.
The sail and the battery compete for the same swing: the covered band shrank (195 vs 204.4), so
the battery's relative value dropped from 173.7 — even though the ship as a whole burns much less
(baseline 12 837 vs 13 307). Mirror image of test 16, where heavier sea inflates both.

> ⚠ On the live screen this test displayed **$132 416** = benefit × **800** while IL1 costs used
> 780 — two responses mixed on one screen. Cause: the profile-restore double-fire (finding #5)
> let an early calculation run with the form DEFAULT fuel price (constant
> `DEFAULT_VALUES.FUEL_PRICE = 800`, which also explains the "$800/ton" Financial badge —
> finding #4). The backend math itself is consistent; the display race is the bug.

**Takeaway:** sail power feeds the cascade (sail-adjusted propulsion), shrinking both what the
battery covers and what the gensets reserve.
