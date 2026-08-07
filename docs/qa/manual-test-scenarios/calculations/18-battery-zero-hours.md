# 18 — Battery for Port, Port Hours = 0: the Zero-Effect Guard (G2/B10)

<!-- header:auto -->

> **Proves** · The zero-effect guard: a battery assigned to a mode with no hours must change nothing, silently.
>
> **Mechanics this scenario turns on**
> - A battery assigned to a mode with zero hours must change nothing and say nothing — no panel, no warning, no effect on the result.
> - Every active mode runs its **own** Level 1 — own demand, own combinations, own baseline, own optimum, own t/h. There is no single "tonnes per hour" for the vessel; the year is `Σ (mode t/h × mode hours)`.
>
> **Panels described below** · What "no effect" must look like
>
> **Anything not described here** behaves exactly as in `01-excel-baseline` — same plant, same hours; only what this scenario changes is worked through below.
>
> **Trust** · verified against the reference workbook. These figures are proof.
>
> **Read after** · scenario 01.

Test 01's vessel; battery 1260 assigned to **Port only**, but Port hours = **0**.

## What "no effect" must look like (all four observed)

1. Form tiles show **"–" / "–"** with the yellow hint *"The battery has no effect: select at
   least one relevant mode with operating hours."*
2. **The Battery Contribution panel is entirely absent** from the results — the backend returns
   no battery details (null), so there is nothing to render. Not an empty box: no box.
3. No crash, no error — a configured-but-idle battery is legal.
4. Every number equals the PURE no-battery calculation of this vessel:

```
Propulsion 11 463 (no reserve added) · hotel 3 800 = SG 3 250 + AE 550
Energy 73 565 000 / 2 750 000 · header ME 14 713 · AE 550
Baseline: NO battery active ⇒ default = LAST row (2 ME+SG, 2.6255) = 13 127.3 t/yr
IL1 = 2.57854 × 5 000 = 12 892.7 t/yr  ·  savings 234.6 t = $182 996
IL3: variation ±500 → ±400 (nothing shaved by a battery that never runs)
```

Note the baseline rule flip: with the battery inactive, the "3rd-from-worst" pre-selection does
NOT apply — the default reverts to the worst row, which is why savings (234.6) are much larger
than test 01's (21.2). Same ship, different assumed "before".

**Takeaway:** battery participation requires BOTH a relevant mode AND hours in it; otherwise the
entire battery machinery steps aside without a trace — and the baseline default follows the
no-battery rule.
