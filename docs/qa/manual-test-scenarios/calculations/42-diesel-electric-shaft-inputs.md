# 42 — Diesel-electric with shaft-bound inputs: SG and PTI refused

<!-- header:auto -->

> **Proves** · the two blocking rules of decision **D-DE3**: on a plant with no main engines,
> a shaft generator and a PTI motor have nothing to hang off, and the app says so instead of
> silently ignoring the values.
>
> **Mechanics this scenario turns on**
> - Validation at `meCount == 0`: SG capacity > 0 and PTI capacity > 0 are **errors**, not
>   warnings and not silent zeroing.
> - Both fire in one response, in declaration order — the pinned 400 list is a contract.
>
> **Trust** · characterisation; the message texts are pinned verbatim.
>
> **Read after** · 39 (the other diesel-electric 400 — capacity, not configuration).

## Why this scenario exists at all

The client UI cannot produce this input: at `meCount = 0` the form **parks** the SG and PTI
controls — disabled and cleared (story DE-C). The rules are therefore unreachable by clicking,
which is exactly why they need a file-level scenario: an imported profile, a hand-written request
or a future UI change can all deliver it, and then the backend must be the one that refuses.

Defence in depth, pinned: the client prevents it, the backend rejects it, this scenario proves
the rejection still happens.

## Inputs that matter

```
meCount 0 · meCapacityPerEngine 0        (a legal diesel-electric plant)
sgCapacityPerEngine 500                  ← nothing to drive it
maxPtiPerEngineKw 300                    ← no shaft to put a motor on
propulsion 8 000 · hotel 3 000 · AE 4 × 4 000   (otherwise identical to scenario 36)
```

## The response

```
400
Shaft generators require a main engine. Set shaft generator capacity to 0 for a diesel-electric plant.
PTI requires a main engine shaft. Clear the PTI capacity for a diesel-electric plant.
```

Note what is **absent**: no "shaft generator capacity cannot exceed main engine capacity" (which
0 < 500 would technically trigger on a mechanical plant) and no main-engine capacity or type
errors. At `meCount = 0` the ME-shaped checks are skipped by design, so the user is told the one
thing worth acting on.
