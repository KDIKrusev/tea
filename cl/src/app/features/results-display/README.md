# results-display — one component per panel, in screen order

The client's equivalent of the backend's `Services/Results/`: the folder is laid out so that
"the third panel down" is a directory you can open.

The numbering below matches the numbered comments in
`features/calculator-page/calculator-page.component.html`. A number is a **place on the page**,
top to bottom, not a priority.

| # | On screen | Lives in |
|---|---|---|
| 01 | Validation warnings | `validation-warnings/` |
| 02 | Power demands | `power-demands-panel/` |
| 03 | Baseline / assumed configuration | `baseline-panel/` |
| 04 | Battery contribution *(only when a battery is active)* | `battery-contribution-panel/` |
| 05 | Report + expand/collapse toolbar | inline in the page (two buttons) |
| 06 | Integration levels 1–3 | `variant-detail-panel/`, rendered three times from `TIER_PANELS` |
| 07 | Sail contribution | inline in the page |
| 08 | Integration level comparison | `tier-comparison/` |
| 09 | Fuel consumption chart | `charts/savings-chart/` |
| 10 | CO2 chart | `charts/savings-chart/` (same component, different input) |

## Why the `mat-expansion-panel` shells stay in the page template

`MatExpansionPanel` resolves its accordion with `@Host()`. A panel moved into a child component's
template would stop finding `<mat-accordion>` and silently detach from it. So the *shell* of each
panel — the expansion panel and its header — lives in the page, and the *body* is a component here.

That is a real constraint of the framework, not a preference, and it is why this folder holds panel
bodies rather than whole panels.
