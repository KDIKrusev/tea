# 14 — Bulk Carrier: L3 Variation from Vessel-Type Lookup

<!-- header:auto -->

> **Proves** · The Level 3 variation looked up from the vessel type when the field is left empty.
>
> **Mechanics this scenario turns on**
> - When the Level 3 variation field is empty the backend looks it up from the vessel type (Bulk 250 · Container 1 500 · LNG 1 000 · otherwise the 500 default), all from `appsettings.json`.
> - Level 3 (DRC) damps the hotel/mission swing: `variation × 0.8`, minus whatever the battery already shaved, so the same kilowatt is never counted twice.
> - Baseline rule: **no battery → the worst combination** (`count − 1`); **battery active → the third from worst** (`Math.Max(0, count − 3)`). It models what the ship is assumed to do today.
>
> **Panels described below** · The point of the test: the L3 lookup · Power Demands · Baseline & IL1 — an honest zero
>
> **Anything not described here** behaves exactly as in `01-excel-baseline` — same plant, same hours; only what this scenario changes is worked through below.
>
> **Trust** · verified against the reference workbook. These figures are proof.
>
> **Read after** · scenario 11.

Small bulk carrier from the DB row: curve 12 kn → **1 365 kW**, SM **20 %**, ME 1×12 000
(Diesel 4-stroke Medium), SG 200, AE 2×500. Profile modes: Transit 5 717 h/165 · Port 2 592/110 ·
Anchor 451/180 · Maneuvering 175 (400/190). No battery. **Load Variation field left EMPTY.**

## The point of the test: the L3 lookup

```
HotelLoadVariationKw = empty → backend looks up VesselTypeName:
"Bulk Carrier 10,000 dwt" contains "Bulk Carrier" → appsettings VesselVariations → ±250
DRC reduction 20 %:  250 × 0.8 = 200
IL3 Optimization Details must read:  "Variation: ±250 kW → ±200 kW"   ← the proof
```

(Config map: Bulk Carrier 250 · Container 1500 · LNG 1000 · default 500.)

## Power Demands

Transit propulsion = 1 365×1.20 = **1 638**. All hotels via SG 200 (AE = 0 everywhere — the
SG-forced rule again; the 12 MW ME idles at ~1 % in port/anchor to spin a 110–200 kW hotel).
Energies: Transit (1 638+165)×5 717 = **10 307 751** · Port 110×2 592 = **285 120** ·
Anchor **81 180** · Maneuvering (400+190)×175 = **103 250**. Total hours 8 935.

## Baseline & IL1 — an honest zero

Transit has exactly **one** valid combination (SG 200 covers hotel 165; both 500-kW AEs would sit
idle → rejected). One row ⇒ baseline = optimal ⇒ **IL1 = Baseline = 1 883.4 t/yr, savings
0 t / $0** against a $110k investment. The calculator refuses to invent value where a
single-engine plant offers no choices. L3 also nets 0 at these load points (SG capacity 200
clamps the spike cycle); the lookup display is the deliverable, not the savings.

**Takeaway:** VesselTypeName's only direct role in the math is this L3 lookup — and the UI shows
it verbatim in the IL3 details line.
