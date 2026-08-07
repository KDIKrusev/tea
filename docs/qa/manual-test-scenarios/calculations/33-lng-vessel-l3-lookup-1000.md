# 33 — LNG carrier: L3 variation 1 000 kW, completing the lookup table

<!-- header:auto -->

> **Proves** · An LNG vessel picking up its 1 000 kW Level 3 variation from the lookup.
>
> **Mechanics this scenario turns on**
> - When the Level 3 variation field is empty the backend looks it up from the vessel type (Bulk 250 · Container 1 500 · LNG 1 000 · otherwise the 500 default), all from `appsettings.json`.
> - Level 3 (DRC) damps the hotel/mission swing: `variation × 0.8`, minus whatever the battery already shaved, so the same kilowatt is never counted twice.
> - ME and AE each use **their own fuel's** CO2 factor (MGO/MDO 3.93267 · HFO 3.114 · LNG 2.753 · Ammonia 0.35154). The two per-engine cards must sum to the panel total.
>
> **Panels described below** · Verified figures · The linearity check this enables
>
> **Anything not described here** — the mechanics above name the step that produced it; `00-ORIENTATION` Part 6 has the full number-to-step index.
>
> **Trust** · characterisation snapshot, generated from the code. It detects change; it does NOT prove correctness. Figures marked *pending reference verification* have never been checked against anything outside the application.
>
> **Read after** · scenario 14.

The last of the three `VesselVariations` entries. With 14 (Bulk Carrier 250), 25 (Container 1 500)
and 26 (default 500), the table and its fallback are now fully covered.

## Verified figures

```
variation          1 000.0 kW
reduced (× 0.80)     800.0 kW
L3 savings            92.83 t/yr
```

Same plant as 19/25/26, so the only moving part is the vessel name.

## The linearity check this enables

Three points on one plant, from three different lookup routes:

```
 500 kW →  45.25 t/yr      (ratio 0.0905 t per kW of swing)
1000 kW →  92.83 t/yr      (       0.0928)
1500 kW → 142.79 t/yr      (       0.0952)
```

Savings rise slightly **faster** than the swing. That is the expected shape: DRC earns its money from
the curvature of the SFOC curve, and a wider swing pushes the down-stroke further into the steep
low-load region where the curve bends hardest.

If a future change made these three points exactly proportional, the curvature term would have been
lost somewhere — the three scenarios together detect that, where any one alone would not.

**Takeaway:** covering a lookup table means one scenario per entry *and* checking they relate to each
other sensibly. The relationship is the stronger assertion.
