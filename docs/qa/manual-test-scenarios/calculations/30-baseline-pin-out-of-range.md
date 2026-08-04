# 30 — Pinned baseline index out of range

Scenario 15 pins a **valid** row. This pins row **99** on a five-row table.

## The rule

```csharp
requestedIndex is int index && index >= 0 && index < sorted.Count
    ? index
    : defaultIndex
```

An index that does not address the list is **ignored**, not clamped. That distinction is the whole
scenario: clamping to the last row would look almost right (the default with no battery *is* the last
row) and be wrong the moment a battery is active, because the battery default is the **third**
highest, not the last.

## Verified against scenario 01

| | 01 (no pin) | **30 (pin = 99)** |
|---|---|---|
| valid combinations | 5 | 5 |
| `selectedBaselineIndex` | 2 | **2** |
| baseline FOC | 13 307.40 | **13 307.40** |

Byte-identical baseline block. Index 2 on a five-row list is `Count − 3` — the D1 battery rule — which
confirms the fallback went to the *battery* default and not to "last row".

## Where a bad index comes from

Not from the UI — the client only sends indices it rendered. It comes from **saved profiles**: pin
row 4, save, then reload the profile after changing the plant so fewer combinations exist. The stored
index now points past the end.

Scenario 15's card documents the client-side bug where the pin was *lost* on restore (finding 5,
since fixed). This one covers the opposite hazard: a pin that survives but no longer fits.

**Takeaway:** silently ignoring a stale pin is the right behaviour, and it is only correct because
the fallback re-derives the default rather than reusing a remembered one.
