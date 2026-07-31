# 17 — Infeasible Plant: ME Too Small (expected 400, no results)

Excel loads (11 463 / 3 800) but ME **2×5 000**, SG 0, AE 4×4 000, no battery. Supposed to fail —
the red banner is the pass criterion.

## The rejection arithmetic

```
Installed ME = 2 × 5 000 = 10 000 kW  <  propulsion demand 11 463 kW
→ ME utilization would exceed 100 % no matter which combination runs
→ INPUT VALIDATION promotes the capacity warning to an error (before Level 1 even starts)
→ HTTP 400: "Main engine utilization > 100%. Consider reducing propulsion power, decreasing
   sea margin, reduce hotel/mission load or increasing main engine capacity."
```

## Two different guards — compare with test 09

| | Test 09 (PTI 50) | Test 17 (ME 2×5 000) |
|---|---|---|
| Where it fails | Level-1 enumeration (battery-PTI gate kills every combo) | Input validation, before L1 |
| Message | battery needs 200.6 kW PTI, only 100 available… | ME utilization > 100 %… |
| Physics | plant can carry the load, battery has no path to the shaft | plant simply too small |

Both: HTTP 400 (user problem, not server fault), empty results panel, actionable message listing
the levers that would fix it (note "decreasing sea margin" — the SM chain from test 16).

**Takeaway:** the app distinguishes WHY a configuration is impossible and says so with the actual
numbers — no generic "error occurred".
