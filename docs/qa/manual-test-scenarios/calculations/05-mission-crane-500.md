# 05 — Mission Crane 500 kW (cascade continues below)

Test 01's vessel + **Mission Heavy-Consumer Max = 500** (battery 1260, Transit).

## Battery Contribution (budget 1260)

The crane's H is its FULL kW — it can start at any moment, so the whole 500 is swing
(Excel G7 = I3), covered at 50 %:

| Row | H | I | J | L | Budget after |
|---|---|---|---|---|---|
| Mission (D=0.5) | **500** | 500 | **250** | **250** | 760 |
| Propulsion (D=0.35) | 573.15 | 573.15 | 200.60 | 372.55 | 186.85 |
| Hotel (D=0.05) | 76 | 76 | 3.8 | 72.2 | **110.85** left |

Tiles: **PS = 454.4 · SR = 694.7**. Everyone got paid — the small crane doesn't starve the queue.

## Power Demands — the crane lands on the HOTEL side

The crane is an electrical consumer on the switchboard, so its uncovered 250 goes to the
hotel/aux side, not the shaft:

- Propulsion' = 11 463+372.55 = **11 836** (identical to test 01!)
- Hotel' = 3 800+250+72.2 = **4 122** → SG 3 250 + AE **872**
- Energy: ME **75 427 738** (same as 01) · AE 872.2×5 000 = **4 361 000**

## Baseline & IL1

All FOCs shift up (AE side carries +250): 2.7108 / 2.7158 / **2.7181** (baseline, 3rd-from-worst)
/ 2.7193 / 2.7379. Baseline = **13 590.7 t/yr** (AE 872.2/12 000 = 7.3 %, AE fuel 1 005.2).
IL1 = 2.7108×5 000 = **13 554.1** (AE 21.8 %, fuel 968.6). **Savings 36.6 t = $28 542** — larger
than 01 (21.2) because more aux-side load = more value in choosing the AE count.
ME fuel is 12 585.6 in both — to the decimal the same as test 01: the ME never felt the crane.

## Battery Benefit

Covered band ΣJ = 454.4 (vs 204.4 in 01) → benefit scales with it:
**422.2 t/yr = $329 352**. Crane + battery is a commercially strong pairing — the crane feeds
the battery high-coverage material (50 % vs hotel's 5 %).

**Takeaway:** reserve routing is per-side (crane → hotel side), and battery value is proportional
to the covered band.
