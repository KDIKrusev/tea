# 11 — OSV 10 kn: the Full Five-Mode Pipeline

Parametric Offshore Support vessel: curve 10 kn → **1 500 kW**, SM 15 % → effective **1 725**.
Plant: ME 2×15 000 (Diesel 2-stroke Medium, id 2) · SG 2 000 · AE 4×1 000. Modes: Transit 4 000 h
(hotel 220) · DP 2 360 h (thrust 3 500, hotel 300, Calm) · Port 1 200/150 · Anchor 800/200 ·
Maneuvering 400 (500/250). Battery 1260, Transit only. Total 8 760 h.

## Battery Contribution (Transit only)

Propulsion H = 1 725×5 % = **86.25** → J 86.25×0.35 = **30.2**, L **56.1** · Hotel H = 4.4 →
J 0.22, L 4.18. Tiles: **SR 60.2 / PS 30.4**. (A 1 260 kW battery against a 90.7 kW total swing —
saturated 14× over.)

## Power Demands (five rows)

| Mode | Propulsion' | Hotel (all SG, AE=0!) | ME power | Energy kWh |
|---|---|---|---|---|
| Transit | 1 725+56.1 = 1 781 | 224.2 | 2 005.2 (13.37 %) | 8 020 970 |
| DP | 3 500 | 300 | 3 800 (25.33 %) | 8 968 000 |
| Port | 0 | 150 | 150 (1 %) | 180 000 |
| Anchor | 0 | 200 | 200 (1.33 %) | 160 000 |
| Maneuvering | 500 | 250 | 750 (5 %) | 300 000 |

Header: ME 17 628 970/8 760 = **2 012** · **AE = 0 for all 8 760 h** (SG-forced rule: the ME spins
even in port/anchor at ~1 % load; the 4 AEs never run — observation #1 at its clearest).

## Per-mode FOC with the actual curve (id 2: 5 %→175, 25 %→174.476, 30 %→172.568 …)

| Mode | SFOC(load) | FOC t/h | × hours = t |
|---|---|---|---|
| Transit | 174.78 @13.37 % | 0.35048 | 1 401.9 |
| DP | 174.35 @25.33 % | 0.66252 | 1 563.6 |
| Port | 175.10* @1 % | 0.02627 | 31.5 |
| Anchor | 175.10* @1.33 % | 0.03502 | 28.0 |
| Maneuvering | 175.0 @5 % | 0.13125 | 52.5 |

*extrapolated below the curve's lowest point. **IL1 total = 3 077.5 t/yr** ✓

## Baseline, savings, tiers

Transit has only **2 valid combos** (SG covers the hotel → AEs idle → rejected) ⇒ the
3rd-from-worst rule clamps to index 0 = the optimal itself. Baseline **3 081.6** → savings
**4.1 t/yr (0.1 %)**, negative ROI — flat SFOC + 2 near-identical rows = nothing to optimize.
IL2 adds 0 (single generator carries hotel). The **Premium chip reads 36.3 t/yr** — the only tier
with real value — of which the **DRC component itself is 32.2** (the other 4.1 is L1 carried over):
variation 500 − 0.22 (battery) → ×0.8 → **±400**, 30 cycles/h over 8 760 h.

## Battery Benefit

Covered band only 30.4 kW, Transit hours only: **21.2 t/yr = $16 548**. A heavily oversized
battery for this vessel — the model refuses to flatter it.

**Takeaway:** the whole pipeline end-to-end on a "boring" plant: tiny cascade, clamped baseline,
zero L1/L2 value, DRC as the only earner, and the AE fleet parked by the SG-forced rule.
