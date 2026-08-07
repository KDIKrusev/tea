# 13 (v2) — LNG Main Fuel: Per-Fuel CO2 Split (D6 fix #3)

<!-- header:auto -->

> **Proves** · Per-fuel CO2: ME and AE burning different fuels must produce two splits that sum to the panel total.
>
> **Mechanics this scenario turns on**
> - ME and AE each use **their own fuel's** CO2 factor (MGO/MDO 3.93267 · HFO 3.114 · LNG 2.753 · Ammonia 0.35154). The two per-engine cards must sum to the panel total.
> - SFOC (g/kWh) depends on **load**: a large 2-stroke burns ~167 at 63 % and over 230 at 5 %. That curve, not the arithmetic, is why running spare engines at low load is expensive.
>
> **Panels described below** · Battery Contribution — UNCHANGED · Power Demands · Baseline — THE core check: two fuels, two factors, cards must sum · IL1 · Battery Benefit
>
> **Anything not described here** behaves exactly as in `01-excel-baseline` — same plant, same hours; only what this scenario changes is worked through below.
>
> **Trust** · verified against the reference workbook. These figures are proof.
>
> **Read after** · scenario 01.

Excel loads, but the plant is LNG-capable: **Dual Fuel Engine id 5, 2×22 000, SG 2 800** ·
AE 4×4 000 (MGO) · fuel price $620 (LNG default).

> v1 of this file paired LNG with a Liquid-family diesel — the client's fuel-family guard
> correctly coerced it back to MGO (and reset the price to MGO's default 950). Working as
> intended; the scenario was fixed to use a Gas-family engine.

## Battery Contribution — UNCHANGED

SR **444.7** / PS **204.4**, identical allocation table to test 01: the cascade knows nothing
about engines or fuels. Clean module isolation.

## Power Demands

Hotel 3 872.2 = SG **2 800** (the smaller Dual-Fuel SG) + AE **1 072**. ME = 11 835.5+2 800 =
14 635.5 (header ≈ 14 636).

## Baseline — THE core check: two fuels, two factors, cards must sum

Combos reshuffle on the new SFOC curve: 2.6778 (1/1) / **2.6831 (2/0 — second now!)** /
2.6865 (1/2 ← baseline) / 2.6897 / 2.6914.

- Baseline FOC = 2.6865×5 000 = **13 432.4 t/yr** = ME 12 219.3 + AE 1 213.2
- CO2 per engine, each with ITS OWN factor:
  ME 12 219.3 × **2.753** (LNG) = **33 639.6** · AE 1 213.2 × **3.93267** (MGO) = **4 771.1**
  → total **38 410.7** ✓ (the pre-D6 single-constant bug cannot reproduce this)
- Cost = 13 432.45 × 620 = **$8 328 116**

## IL1

**13 389.0 t/yr** (savings 43.4 t = $26 935) · CO2 **38 239.8** = ME 33 639.6 + AE 4 600.2.
Note ME CO2 identical in baseline and IL1 — the ME load doesn't change between them.

## Battery Benefit

**173.4 t/yr = $107 516** (× the $620 LNG price) — nearly test 01's tonnage: same cascade.

**Takeaway:** fuel type never touches the FOC math — it prices CO2 and dollars per engine.
Engine type DOES touch FOC (new SFOC curve reshuffles the ranking).
