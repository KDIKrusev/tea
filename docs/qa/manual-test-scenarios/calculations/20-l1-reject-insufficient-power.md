# 20 — Level 1 rejects DP: the installed engines cannot carry the demand

<!-- header:auto -->

> **Proves** · Level 1 rejecting a mode because the installed engines cannot carry its demand.
>
> **Mechanics this scenario turns on**
> - Every active mode runs its **own** Level 1 — own demand, own combinations, own baseline, own optimum, own t/h. There is no single "tonnes per hour" for the vessel; the year is `Σ (mode t/h × mode hours)`.
> - Validation runs **before** Level 1. A plant that cannot carry its load is rejected there, with a different message and a different code path from the Level 1 rejections.
>
> **Panels described below** · Why the failure has to be in DP, not Transit · The response
>
> **Anything not described here** — the mechanics above name the step that produced it; `00-ORIENTATION` Part 6 has the full number-to-step index.
>
> **Trust** · characterisation snapshot, generated from the code. It detects change; it does NOT prove correctness. Figures marked *pending reference verification* have never been checked against anything outside the application.
>
> **Read after** · scenario 17.

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
