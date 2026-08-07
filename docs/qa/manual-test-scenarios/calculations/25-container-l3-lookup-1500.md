# 25 — Container vessel: L3 variation 1 500 kW from the lookup

<!-- header:auto -->

> **Proves** · A Container vessel picking up its 1 500 kW Level 3 variation from the lookup table.
>
> **Mechanics this scenario turns on**
> - When the Level 3 variation field is empty the backend looks it up from the vessel type (Bulk 250 · Container 1 500 · LNG 1 000 · otherwise the 500 default), all from `appsettings.json`.
> - Level 3 (DRC) damps the hotel/mission swing: `variation × 0.8`, minus whatever the battery already shaved, so the same kilowatt is never counted twice.
>
> **Panels described below** · How the variation is chosen · What 1 500 kW does downstream
>
> **Anything not described here** — the mechanics above name the step that produced it; `00-ORIENTATION` Part 6 has the full number-to-step index.
>
> **Trust** · characterisation snapshot, generated from the code. It detects change; it does NOT prove correctness. Figures marked *pending reference verification* have never been checked against anything outside the application.
>
> **Read after** · scenario 14.

`CalculatorSettings.VesselVariations` holds three entries. Only Bulk Carrier (250) had a scenario;
this adds Container, and 33 adds LNG.

## How the variation is chosen

`Level3DrcService.GetVesselVariation` runs three steps:

1. Explicit `hotelLoadVariationKw` on the input wins outright. **This scenario omits the field.**
2. Exact match on the vessel type name.
3. Substring match — "Container 5,000 TEU" contains "Container" → **1 500 kW**.
4. Otherwise the 500 kW default (scenario 26).

## What 1 500 kW does downstream

```
variation          1 500.0 kW
reduced (× 0.80)   1 200.0 kW
L3 savings           142.79 t/yr
```

Compare with scenario 26 — the same plant, the same everything, only the variation differs:

| | variation | L3 savings |
|---|---|---|
| 26 (default) | 500 | 45.25 |
| **25 (Container)** | **1 500** | **142.79** |

Three times the swing, **3.16×** the saving. Slightly super-linear, because DRC monetizes the
*curvature* of the SFOC curve and a wider swing reaches further into the steep low-load region.

That relationship is the real assertion here: a lookup that silently returned the default would drop
this scenario from 142.79 to 45.25 — a 97 t/yr difference no one could miss.

**Takeaway:** the vessel-type lookup is a live input to the numbers, not a label. It deserves a
scenario per entry, and now has one.
