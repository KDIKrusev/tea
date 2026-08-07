# 26 — Unknown vessel type: L3 falls back to the 500 kW default

<!-- header:auto -->

> **Proves** · An unrecognised vessel type falling back to the 500 kW default variation.
>
> **Mechanics this scenario turns on**
> - When the Level 3 variation field is empty the backend looks it up from the vessel type (Bulk 250 · Container 1 500 · LNG 1 000 · otherwise the 500 default), all from `appsettings.json`.
> - Level 3 (DRC) damps the hotel/mission swing: `variation × 0.8`, minus whatever the battery already shaved, so the same kilowatt is never counted twice.
>
> **Panels described below** · The input · Why it shares a plant with 19, 25 and 33
>
> **Anything not described here** — the mechanics above name the step that produced it; `00-ORIENTATION` Part 6 has the full number-to-step index.
>
> **Trust** · characterisation snapshot, generated from the code. It detects change; it does NOT prove correctness. Figures marked *pending reference verification* have never been checked against anything outside the application.
>
> **Read after** · scenario 14.

The fourth branch of the variation lookup, and the one a real user hits most often — the vessel
catalogue is far larger than the three-entry variation table.

## The input

```
vesselTypeName      "Fishing Vessel 800 gt"
hotelLoadVariationKw  (omitted)
```

"Fishing Vessel 800 gt" matches no key exactly and contains none of "Bulk Carrier", "Container" or
"LNG" as a substring, so `DefaultVesselVariationKw` = **500** applies.

## Why it shares a plant with 19, 25 and 33

Identical plant, identical hours, identical fuels — **only the vessel name differs**. That makes the
four snapshots directly comparable and turns the lookup into a controlled experiment:

| Scenario | vessel name | source | variation | L3 |
|---|---|---|---|---|
| 19 | Offshore Support | explicit field = 500 | 500 | 45.25 |
| **26** | **Fishing Vessel 800 gt** | **default** | **500** | **45.25** |
| 33 | LNG Carrier 170,000 m³ | lookup → LNG | 1 000 | 92.83 |
| 25 | Container 5,000 TEU | lookup → Container | 1 500 | 142.79 |

19 and 26 land on the same number by different routes — one from an explicit field, one from the
default. The snapshots cannot tell them apart, but the **scenario files** can: 19 sets
`hotelLoadVariationKw`, 26 has no such key. That is the distinction under test.

**A note on the substring rule.** It is generous by design — "Bulk Carrier 10,000 dwt" is meant to
match "Bulk Carrier" (scenario 14). The flip side is that a future vessel named, say, "LNG Bunkering
Barge" would silently inherit the LNG carrier's 1 000 kW. Worth knowing before adding names.

**Takeaway:** an unrecognised vessel does not fail and does not warn — it quietly gets 500 kW. That
is a reasonable default and an easy thing to forget.
