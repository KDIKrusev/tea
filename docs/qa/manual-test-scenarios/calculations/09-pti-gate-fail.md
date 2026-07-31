# 09 — PTI 50: the Gate Blocks (expected 400 error, no results)

Test 01 + **PTI = 50**. This scenario is SUPPOSED to fail — the red banner is the pass criterion.

## The rejection arithmetic

```
PTI capacity = 2 × 50 = 100 kW
Required     = battery propulsion band = 200.6 kW
100 < 200.6  →  EVERY combination is rejected by the battery-PTI gate
             →  zero valid combinations  →  NoValidCombinationException  →  HTTP 400
```

## Expected screen

- Red "Validation Failed" banner with the QA-C-1 message containing the REAL numbers:
  *"…the battery needs 200.6 kW of PTI capacity to shave propulsion peaks in Transit mode, but
  only 100 kW is available. Increase the PTI capacity per main engine (currently 50 kW), reduce
  the battery power, or clear the PTI field…"*
- **Empty results panel** ("Results will appear here") — no stale numbers left behind.

## Why 400 and not 500

An infeasible configuration is a USER-input problem, answered in the validation shape so the
client renders the reason. Before fix QA-C-1 (D6) this path returned a bare 500.

**Takeaway:** the error is actionable — exact deficit, exact cause, three concrete ways out.
Compare with test 17, which fails EARLIER (input validation) with a different message.
