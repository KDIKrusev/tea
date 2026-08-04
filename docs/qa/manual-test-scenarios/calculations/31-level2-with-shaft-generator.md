# 31 — Level 2 with a shaft generator present

Scenario 19 proves Level 2 can save fuel, but on a plant with **no** shaft generator. This adds one,
because the SG travels through Level 2 on a different rule from the aux engines and that rule had no
end-to-end coverage.

## The two rules, side by side

```
SG  : load fixed by the main-engine shaft — REPORTED, never optimized
AE  : load redistributed by the sweep, inside the 10 %…90 % window
```

## The plant

```
ME 2 × 8 500 · SG 2 × 500 · AE 2 × 2 000 · propulsion 8 000 · hotel 3 000
```

The shaft generators cover 1 000 kW of the hotel load; the remaining 2 000 kW goes to the aux side —
which is exactly scenario 19's aux problem, so the sweep behaves identically there.

## The setpoint table

```
SG   1 000 kW  @ 100.0 %   sfoc 174.48
AE     200 kW  @  10.0 %   sfoc 228.47
AE   1 800 kW  @  90.0 %   sfoc 193.78
```

Two things to read carefully:

1. **The SG sits at 100 % load** — above the 90 % ceiling the aux engines must respect. That is
   correct, not a bug: the ceiling exists so Level 2 has room to redistribute, and there is nothing
   to redistribute on a shaft generator. Its load is whatever the main engine's shaft delivers.
2. **The SG uses the MAIN engine's SFOC curve** (174.48 g/kWh, far below the aux figures). A shaft
   generator burns main-engine fuel; `GeneratorType.SG → EngineCategory.Main` is what encodes that.

## Result

```
L1 388.04 · L2 8.14 · L3 45.25 t/yr
baseline 9 862.1 t/yr   =  ME 7 415.2 + AE 2 446.9
```

L2 is **8.14** — identical to scenario 19, because the aux-side problem is identical. That equality
is itself the assertion: adding a shaft generator must not change what the sweep does to the aux
engines.

**Takeaway:** the SG appears in the setpoint table so the client can draw it, and is otherwise
inert. A change that started optimizing it would move this snapshot immediately.
