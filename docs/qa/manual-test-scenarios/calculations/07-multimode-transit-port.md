# 07 — Multi-Mode: Transit + Port

Test 01's vessel + Port 1 000 h / hotel 500. Battery 1260 for **both** Transit and Port.

## Battery Contribution — one cascade PER MODE, each with the FULL budget

Modes never overlap in time (the ship is either at sea or at the quay), so each mode's cascade
starts with the whole 1260 — the budget is not split.

- **Transit table**: identical to test 01 (573.15 → 200.6/372.5 · 76 → 3.8/72.2).
- **Port table**: a single row — no propulsion (moored), no mission. Hotel: H = 500×2 % = **10**
  → I=10, J=**0.5**, L=**9.5**; 1 250 kW of budget left unused.

Tiles are SUMS across modes: **SR = 444.7+9.5 = 454.2 · PS = 204.4+0.5 = 204.9**.

## Power Demands

- Port row: propulsion 0, hotel 500+9.5 = 509.5 — carried by **SG 510 / AE 0**: the SG-forced
  rule makes the main engine run in port (~2 % load) to spin its shaft generator
  (logged observation #1; real vessels would run an AE).
- Energy: Transit 75 427 738 + Port 509.5×1 000 = 509 500 → header ME = 81 622 000/…
  actually (75 427 738+509 500)/6 000 = **12 656** · AE 3 111 000/6 000 = **518**.

## Baseline & IL1

Transit part identical to 01; Port adds ~equal amounts to both sides:
Baseline **13 396.6** · IL1 **13 375.4** · savings **21.2 t** (unchanged — port has nothing to
optimize in this plant).

## Battery Benefit

Per-mode dual scenarios, summed: Transit 173.66 + Port ≈ 0.09 = **173.74 t/yr = $135 520**.
The port contribution is ~$68/year — the model honestly says a battery bought for a 500-kW port
hotel is pointless.

**Takeaway:** per-mode full budget, per-mode cascade shape (port = one row), tiles and benefit
are sums — and the SG-forced quirk becomes directly visible in the Port row.
