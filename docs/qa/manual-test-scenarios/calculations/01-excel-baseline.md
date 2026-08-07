# 01 — Excel Baseline (battery 1260 kW, Transit)

<!-- header:auto -->

> **Proves** · The whole pipeline on the reference plant — every Excel "Load Demands" cell reproduced 1:1.
>
> **Mechanics this scenario turns on**
> - Cascade per row: `H` = what it wants · `I = min(remaining budget, H)` · `J = I × CoverageFactor` (covered) · `L = H − J` (left to the gensets). Priority: DpReserve → DpDemand → Mission → Propulsion → Hotel. Invariant `ΣJ + ΣL = ΣH` — the sea sets the swing, the battery moves the split.
> - Peak-shaving `H` = average × VariationFactor (propulsion 5 %, hotel 2 %). CoverageFactor is a modelling assumption about how much of a swing a battery can realistically catch — propulsion 0.35, hotel 0.05. Both live in `appsettings.json`, not in code.
> - Only the **uncovered** part rejoins the demand: `propulsion' = propulsion + L`, `hotel' = hotel + L`. Covered power (`J`) is never subtracted from anything.
> - The shaft generator is filled before any auxiliary starts (the main engine is already turning), and its output is a load **on** that main engine — which is why the ME figure exceeds propulsion. SG capacity scales with the number of running MEs.
> - SFOC (g/kWh) depends on **load**: a large 2-stroke burns ~167 at 63 % and over 230 at 5 %. That curve, not the arithmetic, is why running spare engines at low load is expensive.
> - Baseline rule: **no battery → the worst combination** (`count − 1`); **battery active → the third from worst** (`Math.Max(0, count − 3)`). It models what the ship is assumed to do today.
> - Battery Benefit runs the pipeline **twice**: world A (demand = average + uncovered `L`) and world B (demand = average + the full swing `H`). `Benefit = (FOC_B − FOC_A) × hours`. Both are **optima**; a pinned baseline is ignored. Unticking "Enable Battery" in the UI gives a *third* world — raw demand, swing carried by nobody — which burns less than A and is not the comparison.
>
> **Panels described below** · Battery Contribution · Power Demands · Baseline · Integration Levels · Battery Benefit
>
> **This is the reference card.** The other Excel-plant scenarios (02–10, 12–18) describe only what they change and refer back to this one for the rest.
>
> **Trust** · verified against the reference workbook. These figures are proof.

The workbook's saved scenario: propulsion 11 463 · hotel 3 800 · SM 0 · ME 2×24 000 · SG 3 250 ·
AE 4×4 000 · Transit 5 000 h · battery 1260/2000 (Transit).

## Battery Contribution

Budget 1260. DpReserve/DpDemand/Mission want 0 (no DP, no crane). Then:

| Row | H (wants) | I (takes) | J = I×D (covered) | L = H−J (gensets) | Budget after |
|---|---|---|---|---|---|
| Propulsion (D=0.35) | 11 463×5% = **573.15** | 573.15 | 573.15×0.35 = **200.60** | **372.55** | 686.85 |
| Hotel (D=0.05) | 3 800×2% = **76** | 76 | **3.8** | **72.2** | **610.85** unused |

Tiles: **PS = 204.4** · **SR = 444.7** (573.15+76 = 649.15 total swing = 204.4+444.7 ✓).
Budget is beyond saturation — 610.85 kW buy nothing extra.

## Power Demands

- Propulsion' = 11 463 + 372.55 = **11 836** · Hotel' = 3 800 + 72.2 = **3 872**
- SG = min(3 872.2, 3 250) = **3 250** · AE = **622** · ME = 11 835.5 + 3 250 = **15 086** (header)
- Energy: ME 15 085.5×5 000 = **75 427 738** · AE 622.2×5 000 = **3 111 000** kWh

## Baseline (No iEMS)

Loads per combo (ME load identical for 1-ME rows: 15 085.5/24 000 = **62.9 %**):

| ME/SG/AE | AE load | FOC t/h |
|---|---|---|
| 1/on/1 | 622.2/4 000 = 15.6 % | **2.6572** ← optimal |
| 1/on/2 | 7.8 % | 2.6603 |
| 1/on/3 | 5.2 % | **2.6615** ← baseline (3rd-from-worst, battery rule) |
| 1/on/4 | 3.9 % | 2.6621 |
| 2/on/0 | — (SG 6 500 covers hotel) | 2.6975 |

FOC example: SFOC_ME(62.9 %) ≈ 166.86 → ME 15 085.5×166.86/1e6 = 2.51712 t/h; AE(5.2 %) ≈ 232.0
→ 0.14436; sum 2.6615.

- Baseline FOC = 2.6615×5 000 = **13 307.4 t/yr** (ME 12 585.6 + AE 721.8)
- CO2 = 13 307.4 × 3.93267 = **52 333.6** (ME 49 494.9 + AE 2 838.7 — cards must sum to the total)
- Cost = 13 307.4 × 780 = **$10 379 775**

## Integration Levels

- IL1 = 2.6572×5 000 = **13 286.2 t/yr** (AE 700.7) · CO2 **52 250.4** · cost **$10 363 271**
- **Savings = 13 307.4 − 13 286.2 = 21.2 t/yr (0.2 %) = $16 504**
- IL2 adds **0**: L2 redistributes hotel between SG and AE — the split (SG maxed at 100 %) is
  already optimal. IL3 adds **0** here: the ±(500−3.8)×0.8 spike cycle produces no FOC delta at
  these load points. All three tier chips show 21.2 — correct, not a bug.

## Battery Benefit (green badge)

World B (no battery) demand = 11 463+573.15 / 3 800+76 → optimal 2.69198 t/h.
Benefit = (2.69198 − 2.65725) × 5 000 = **173.66 t/yr** × 780 = **$135 452**.

**Takeaway:** the whole pipeline on the reference scenario — every Excel Load-Demands cell
(I/J/K/L rows 8–9) matches the allocation table 1:1.
