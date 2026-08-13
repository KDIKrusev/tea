# 39 — Diesel-electric infeasible: the AE fleet cannot carry the load

<!-- header:auto -->

> **Proves** · the diesel-electric capacity validation (story DE-A): a 0-ME plant whose
> auxiliaries cannot carry propulsion + hotel is rejected **in validation**, with one actionable
> message — the ME-shaped checks (utilisation, hotel-vs-SG+AE, shaft-capacity) are skipped so
> no misleading advice co-fires.
>
> **Mechanics this scenario turns on**
> - Validation branch at `meCount == 0`: `effective propulsion + transit hotel > total AE
>   capacity` → Error-severity warning promoted to a 400.
> - Same failure family as scenario 17 (validation, before Level 1) — different plant shape,
>   different message.
>
> **Trust** · characterisation snapshot; the 400 text is pinned verbatim.
>
> **Read after** · 17 (the conventional twin), 20 (the Level 1 rejection family).

## Inputs that matter

```
propulsion 11 463 (SM 0)   hotel 3 800   →  demand 15 263 kW
AE 2 × 4 000 = 8 000 kW    meCount 0, SG 0, PTI 0
```

## The response

```
400
Auxiliary engine capacity cannot carry propulsion and hotel load. Consider reducing propulsion
power, decreasing sea margin, reducing hotel/mission load or increasing auxiliary engine capacity.
```

Contrast with scenario 17's conventional twin ("Main engine utilization > 100%…"): same Excel
loads, same early exit, but the advice names the levers a diesel-electric operator actually has.
Note what is absent — no "hotel exceeds SG+AE" and no shaft-capacity message, although both
checks would technically match this input's numbers; they are skipped by design at 0 ME.

**Takeaway:** the 90 % AE cap makes the *feasible* threshold stricter than this validation check
(capacity ≥ demand/0.9, per combination) — a plant can pass validation and still lose every
combination to the cap; that case gets the Level 1 diesel-electric sentence instead
(`Level1RejectionTally`, covered by unit test, no scenario needed).
