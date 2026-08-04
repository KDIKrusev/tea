# 33 — LNG carrier: L3 variation 1 000 kW, completing the lookup table

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
