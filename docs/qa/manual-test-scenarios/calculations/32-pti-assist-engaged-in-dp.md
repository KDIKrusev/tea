# 32 — PTI assist actually engaged

<!-- header:auto -->

> **Proves** · PTI assist actually engaging, inside DP mode.
>
> **Mechanics this scenario turns on**
> - PTI is the shaft motor: the battery pushing power through the main engine's shaft. A gate checks that the installed PTI capacity can carry the propulsion band the battery is asked to shave; if not, the whole calculation is refused with a 400.
> - Every active mode runs its **own** Level 1 — own demand, own combinations, own baseline, own optimum, own t/h. There is no single "tonnes per hour" for the vessel; the year is `Σ (mode t/h × mode hours)`.
> - A **Reserve** row is not a swing: `H` = the full requirement, `CoverageFactor` = 1.00 (kW for kW), and it counts toward Spinning Reserve but **not** toward Peak Shaving. A covered reserve is readiness, so it never appears in the demand.
>
> **Panels described below** · Why it had to be DP · The plant · The arithmetic, verified in the response · What this scenario would catch
>
> **Anything not described here** — the mechanics above name the step that produced it; `00-ORIENTATION` Part 6 has the full number-to-step index.
>
> **Trust** · characterisation snapshot, generated from the code. It detects change; it does NOT prove correctness. Figures marked *pending reference verification* have never been checked against anything outside the application.
>
> **Read after** · scenario 08.

Scenarios 08 and 09 are named after the PTI gate, but neither ever **engages** the shaft motors —
their plants have no main-engine deficit, so PTI capacity sits there unused. Until this scenario the
whole assist mechanism (Increment C / G) had only unit-test coverage.

## Why it had to be DP

`ValidateSystemCapacity` computes main-engine utilisation **for Transit, without accounting for PTI**.
So any Transit plant that needs shaft-motor assist is rejected as "ME utilisation > 100 %" before
Level 1 is ever asked. PTI can therefore only engage in a mode validation does not inspect.

*This is logged as an open question — see the coverage matrix. It may be intended; the effect is that
the shaft-motor feature is DP-only in practice.*

## The plant

```
ME 2 × 5 000 · SG 2 × 500 · AE 3 × 800 · maxPtiPerEngine 700
Transit : propulsion 4 000, hotel 1 500, 5 000 h
DP      : thrust    10 200, hotel 1 500, 1 000 h
```

## The arithmetic, verified in the response

```
DP thrust                                   10 200
+ shaft-generator load carried by the ME     1 000
= main-engine demand                        11 200
− main-engine capacity (2 × 5 000)          10 000
= deficit                                    1 200  ← moved to the shaft motors

PTI capacity  2 × 700                        1 400  ≥ 1 200  ✔
aux-side load  1 200 × 1.05 (5 % loss)       1 260
+ hotel remainder (1 500 − 1 000)              500
= aux power                                  1 760
```

And the snapshot's DP breakdown reads exactly:

```
DP  1 000 h   propulsionMainEngine 9 000.0   SG 1 000.0   AE 1 760.0
```

`propulsionMainEngine` is `MePowerKw − SgPowerKw` = 10 000 − 1 000 = **9 000** — the main engine
pinned at its capacity, with the missing 1 200 kW arriving through the shaft.
The **1 760 kW** on the aux side is the transmission loss made visible: 1 260 of it is imported
thrust, not hotel load.

## What this scenario would catch

- The 5 % loss factor (`BatterySettings.PtiLossFactor`) silently changing → 1 760 moves.
- The deficit no longer being capped at capacity → `propulsionMainEngine` moves.
- PTI being applied before the aux-headroom check → the combination would survive when it should not.

**Takeaway:** a feature named in two scenario titles was never actually executed by either. "Sets the
field" and "reaches the code" are different claims, and only the snapshot can tell them apart.
