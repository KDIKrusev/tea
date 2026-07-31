# 08 — PTI 3250: the Discharge Gate Passes Silently

Test 01 + **PTI Capacity per Main Engine = 3 250**. Everything else identical.

## What the PTI field switches on

With PTI = 0/empty the battery is modelled "at switchboard level" (no feasibility question,
ADR-5). A positive PTI enables the Excel-fidelity check: the battery's covered PROPULSION band
must physically reach the shaft through the shaft machines (motor mode).

## The gate arithmetic (per combination)

```
PTI capacity  = installed MEs × MaxPti = 2 × 3 250 = 6 500 kW   (union rule, decision D5)
PTI used for propulsion assist = 0    (ME 2×24 000 has no deficit at 11 836 kW)
Headroom      = 6 500 − 0 = 6 500
Required      = battery's propulsion covered band = 200.6 kW

6 500 ≥ 200.6  →  every combination passes  →  nothing is filtered
```

## Result panels

**Identical to test 01 in every number** (SR 444.7 / PS 204.4, same 5 combos, baseline 13 307.4,
IL1 13 286.2, benefit 173.7). The gate is a guard, not a feature — a wide-open door changes
nothing. That silence IS the test result.

**Takeaway:** paired with test 09 (same field = 50 → hard 400 error), this pins both sides of the
gate's threshold. Manual boundary if curious: 100/engine still fails, 101/engine passes
(2×101 = 202 ≥ 200.6).
