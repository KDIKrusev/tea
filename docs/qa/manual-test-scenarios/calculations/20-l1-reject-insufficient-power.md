# 20 — Level 1 rejects DP: the installed engines cannot carry the demand

One of three scenarios (20, 21, 22) added because **only 2 of the 4 rejection messages** a user can
receive had end-to-end coverage. `Level1RejectionTally.ExplainFor` picks one sentence based on which
counter fired, and that sentence is the entire actionable content of the 400.

## Why the failure has to be in DP, not Transit

`ValidationService` checks main-engine utilisation **for Transit only**. Any Transit plant short of
power is rejected there, before Level 1 runs — that path is scenario 17. To reach Level 1's *own*
rejection the shortfall must be in a mode validation does not inspect.

```
Transit : propulsion 4 000, hotel 800   → ME utilisation 40 %      ✔ passes validation
DP      : required thrust 10 200        → ME needs > 10 000 kW     ✘ no combination survives
```

The plant is 2 × 5 000 kW main engines with no shaft generator, so 10 200 kW of thrust cannot be
produced by any on/off combination, and PTI is not configured.

## The response

```
400
No feasible engine configuration: the installed engines cannot carry the DP demand.
Increase engine capacity or engine count, or reduce the propulsion/hotel power for this mode.
```

Two details worth noticing:

- **The mode is named.** A user with four modes configured needs to know which one failed.
- **The advice is about capacity**, not about the battery or PTI — those branches take precedence in
  `ExplainFor` and did not fire here.

**Takeaway:** validation guards Transit; Level 1 guards every other mode. A vessel can pass every
form check and still be impossible to operate in DP.
