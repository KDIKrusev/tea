# 03 — No-Battery Reference World (the hand-built "twin ship")

No battery; loads pre-inflated to average + FULL swing: propulsion **12 036.15** (11 463+573.15),
hotel **3 876** (3 800+76). This is exactly the internal "world B" the Battery Benefit is
computed against — built by hand so you can see it.

## Battery Contribution

Absent — no battery configured. (Tiles in the form show nothing.)

## Power Demands

SG 3 250 · AE 626 · ME = 12 036.15+3 250 = **15 286** (63.69 %) ·
Energy ME 15 286.15×5 000 = **76 430 750** (remember: ME energy includes driving the SG).

## Baseline & IL1

No battery ⇒ baseline default = **last row** (worst: 2 ME+SG, 2.7305 → **13 652.6 t/yr**), not
3rd-from-worst — the Krishna rule applies only when a battery is active.
IL1 optimal = 2.69198 → **13 459.9 t/yr**; savings 192.7 t.

## The proof this file exists for

```
13 459.9  −  13 286.2 (IL1 of test 01)  =  173.7  = test 01's green badge  ✓
13 459.9  −  13 371.2 (IL1 of test 02)  =   88.7  = test 02's green badge  ✓
```

One file proves both benefits, because the no-battery twin is the same ship in both comparisons.

**Takeaway:** the dual-scenario (R3a) mechanism reproduced with two imports and a subtraction —
no trust in hidden code required.
