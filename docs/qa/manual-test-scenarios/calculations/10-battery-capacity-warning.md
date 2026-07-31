# 10 — Capacity Warning: 1000 kW on a 400 kWh Tank

Test 01's vessel; battery **power 1 000 / capacity 400** (Transit).

## The warning (yellow, not red)

```
Plausibility rule: capacity must sustain full power ≥ 30 minutes  ⇔  kWh ≥ 0.5 × kW
400 < 0.5 × 1000 = 500  →  WARNING: "Battery capacity cannot sustain the configured power
                            for 30 minutes — consider increasing capacity or reducing power."
```

A warning advises; it never blocks — results are computed normally (contrast with tests 09/17
where errors leave the panel empty).

## Why the results equal test 01 exactly

Two facts combine:

1. **kWh participates in NO calculation** (decision D4/Q1) — capacity is informational + this
   plausibility check only. Change 400 → 5 000 and not a single computed number moves.
2. **Saturation (invariant INV-2):** the whole cascade wants ΣH = 649.15 kW. Both 1 000 and 1 260
   exceed that, so both cover everything coverable:
   Propulsion I=573.15 (J 200.6/L 372.5) · Hotel I=76 (J 3.8/L 72.2) · leftover 350.85.

Tiles **SR 444.7 / PS 204.4**, benefit **173.7 t = $135 452**, IL1 **13 286.2** — all identical
to test 01.

**Takeaway:** beyond cascade saturation, extra battery power buys nothing — and the tank size
never was part of the math. Sales angle: for this vessel 1 000 kW ≈ 1 260 kW; money is better
spent on capacity (to clear the warning) or saved.
