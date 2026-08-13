# Battery Contribution and Battery Benefit — how the numbers in your case are produced

For the example you sent, this note explains how each figure in the result panel was produced, what
it includes, and how you can reproduce the Battery Benefit yourself. It follows your own scenario
throughout: **800 kW battery**, active in Transit and DP, four modes — Transit 2 500 h · DP 1 000 h
· Port 500 h · Manoeuvring 500 h.

---

## 1. What you enter, and what is derived

You enter **average loads only**. Nothing in your input carries any battery correction:

```
propulsionPower        11 463      seaMargin  10 %
transitHotelPowerKW     3 800      transitHours       2 500
dpHotelPowerKW          3 500      requiredDPPowerKW  1 000     dpHours  1 000
missionHeavyConsumerMaxKw  250
dpRedundancyRequirementKw  800
battery  800 kW / 800 kWh          active in: Transit, DP
ME 1 × 15 000    SG 500    AE 4 × 4 000
```

Everything else is derived. The factors come from configuration, not from your input:

```
variation:  Propulsion 5 %   Hotel 2 %   DpDemand 0 %   Mission and DpReserve — entered explicitly
coverage:   DpReserve 100 %  DpDemand 50 %  Mission 50 %  Propulsion 35 %  Hotel 5 %
priority:   DpReserve → DpDemand → Mission → Propulsion → Hotel
```

Worth noting up front: the Power Demands you see **already include the battery**. They are the
average load plus whatever the battery could not cover. The no-battery comparison is a second,
parallel run that is never displayed — you only see the difference, on the Battery Benefit badge.

---

## 2. The four rules

Every number below comes from one of these four rules. There is no fifth.

**Rule 1 — how much a load swings (H).** Three different formulas:

```
ordinary load        H = average load × variation factor
mission consumer     H = full rating                  (the crane is either on or off)
class reserve (DP)   H = the stated requirement       (not a percentage — a requirement)
```

**Rule 2 — who gets battery power (I).** The budget is allocated in priority order, top down. Each
row takes what it asks for or whatever is left, whichever is smaller. Once the budget is exhausted,
the remaining rows take **zero**.

**Rule 3 — how much of that counts (J).**

```
J = I × coverage factor
```

**Rule 4 — what the engines still carry (L), and where it goes.**

```
L = H − J
```

Split by plant side:

```
Propulsion, DpReserve, DpDemand   →  added to the propulsion demand (the shaft)
Mission, Hotel                     →  added to the hotel demand (the switchboard)
```

A mission consumer is an electrical load, which is why its uncovered part goes to the hotel side
even though it has its own row.

The identity `J + L = H` holds on every row and every total. The battery does not shrink the swing;
it only changes who carries it.

---

## 3. Transit · 2 500 hours

### Step 1 — effective propulsion

```
11 463 × 1.10  =  12 609.3 kW
```

### Step 2 — how much swings

| Row | Formula | H |
|---|---|---|
| Mission | full rating | **250** |
| Propulsion | 12 609.3 × 5 % | **630.465** |
| Hotel | 3 800 × 2 % | **76** |
| | **total** | **956.465** |

The battery is 800 kW. The loads want 956.5. **Not enough** — so the priority order decides who
goes without.

### Step 3 — allocating the 800 kW

```
budget 800
  Mission      wants 250       →  takes 250      550 left
  Propulsion   wants 630.465   →  takes 550        0 left
  Hotel        wants  76       →  takes   0      budget exhausted
```

### Step 4 — covered and uncovered

| Row | takes | × factor | covered J | uncovered L = H − J |
|---|---|---|---|---|
| Mission | 250 | × 0.50 | **125.0** | 125.0 |
| Propulsion | 550 | × 0.35 | **192.5** | 437.965 |
| Hotel | 0 | × 0.05 | **0** | 76.0 |
| | | | **317.5** | **638.965** |

317.5 is the *Peak Shaving* tile. 639 is this mode's contribution to *Spinning Reserve*.

### Step 5 — where the uncovered part goes

```
propulsion'  =  12 609.3  +  437.965                =  13 047.265 kW
hotel'       =   3 800    +  125.0     +  76.0      =   4 001.0  kW
                              ↑            ↑
                        from mission   from hotel
```

### Step 6 — across the machinery

```
the shaft generator is filled first (the shaft is turning anyway)   SG  =    500
the rest goes to the auxiliaries      4 001 − 500            =     AE  =  3 501     →  1 × 87.5 %
the main engine carries propulsion + SG   13 047.265 + 500   =     ME  = 13 547.265 →  90.3 %
```

The main engine at 90.3 % is correct but worth noting — it leaves little margin in heavier weather.

---

## 4. DP · 1 000 hours

### Step 1 — how much swings

| Row | Formula | H |
|---|---|---|
| DpReserve | the stated requirement | **800** |
| DpDemand | 1 000 × 0 % | **0** |
| Mission | full rating | **250** |
| Hotel | 3 500 × 2 % | **70** |
| | **total** | **1 120** |

`DpDemand` has a zero factor — the thrust itself is not modelled as a fluctuating load, only the
class reserve behind it.

### Step 2 — allocating the 800 kW

```
budget 800
  DpReserve    wants 800  →  takes 800      0 left
  DpDemand     wants   0  →  takes   0
  Mission      wants 250  →  takes   0      nothing left
  Hotel        wants  70  →  takes   0      nothing left
```

The class reserve is first in the queue **and is the only row covered 1:1** — the requirement is
either met or it is not, there is no percentage. So it consumes the entire budget on its own.

### Step 3 — covered and uncovered

| Row | takes | × factor | covered J | uncovered L |
|---|---|---|---|---|
| DpReserve | 800 | × 1.00 | **800** | 0 |
| DpDemand | 0 | — | 0 | 0 |
| Mission | 0 | × 0.50 | 0 | **250** |
| Hotel | 0 | × 0.05 | 0 | **70** |
| | | | **800** | **320** |

> **Why the Covered total reads 0 in the panel.** The *Peak Shaving* tile sums only rows whose
> function is *PeakShaving*. `DpReserve` has function *Reserve* and is deliberately excluded — a
> reserve is a readiness requirement, not a peak being flattened. The 800 kW is real and carries
> most of the benefit; it simply is not part of that particular total. We accept this reads like a
> broken sum and will relabel the column.

### Step 4 — where the uncovered part goes

```
thrust'  =  1 000  +   0                =  1 000 kW
hotel'   =  3 500  + 250   +  70        =  3 820 kW
                      ↑        ↑
                from mission from hotel
```

### Step 5 — across the machinery

```
SG  =    500
AE  =  3 820 − 500  =  3 320   →  1 × 83 %
ME  =  1 000 + 500  =  1 500   →  10 %
```

---

## 5. The three rows you marked

**Transit · Mission · 250 / 250 / 125 / 125.** A mission consumer's variation is its full rating,
not a percentage of it — a crane is either off or pulling its whole load, and it can start at any
moment. The battery covers it in full, and 50 % of that counts as covered.

**Transit · Hotel · 76 / 0 / 0 / 76.** Hotel is last in the priority order, and the 800 kW was
consumed by mission (250) and propulsion (550). Its full 76 kW therefore stays with the engines.
Note that even if it had been served, the 5 % hotel factor would have credited only 3.8 kW of it.

**DP · Mission · battery used 0.** The 800 kW class reserve outranks it and takes the whole budget.
With a larger battery the mission row would be the next one served.

---

## 6. Port and Manoeuvring

Neither is in the battery's mode list, so there is no allocation, no variation and no rows at all.

```
Port          hotel 2 000                  ME =     0 + 500 =   500     AE = 1 500
Manoeuvring   prop 8 000 · hotel 2 500     ME = 8 000 + 500 = 8 500     AE = 2 000
```

---

## 7. The two tiles

```
Peak Shaving      =  Σ J over peak-shaving rows only     317.5  +    0  =  317.5 kW
Spinning Reserve  =  Σ L over all rows                 638.965  +  320  =  959   kW
```

DP adds nothing to Peak Shaving despite having consumed the entire budget — because by definition a
reserve is not peak shaving.

---

## 8. Battery Benefit — the two worlds

This answers a different question from the integration levels. The levels ask what better control
would save on the vessel as configured. Battery Benefit asks what **the battery itself** saves
against the same vessel without one.

The same chain is run twice, and both runs are independently optimised — the engine combination is
re-selected in each world, so it is a like-for-like comparison:

```
World A  (as configured)   budget 800   →  the engines carry only the uncovered part L
World B  (no battery)      budget 0     →  nothing is covered, so L = H
```

World B is **not** "remove the battery". The allocation still runs; it simply covers nothing, so the
full variation lands on the engines. Which gives the single rule you need:

> **World B = World A + the "Covered" column, row by row, on the side that row belongs to.**

| Mode | Row | covered in A | goes to | World A | World B |
|---|---|---|---|---|---|
| Transit | Propulsion | 192.5 | shaft | 13 047.265 | **13 239.765** |
| Transit | Mission | 125.0 | switchboard | 4 001 | **4 126** |
| Transit | Hotel | 0 | switchboard | | |
| DP | DpReserve | 800 | shaft | 1 000 | **1 800** |
| DP | Mission | 0 | switchboard | 3 820 | **3 820** |
| DP | Hotel | 0 | switchboard | | |

Note the asymmetry: in Transit the hotel demand changes (4 001 → 4 126) because the mission
consumer was covered by 125 kW and a mission consumer sits on the switchboard. In DP the hotel
demand does not change, because there both mission and hotel received nothing.

The difference in fuel between the two worlds is the benefit:

**379.9 t/yr, or $296 318/yr.**

The saving is not linear in kW: carrying the extra load can force another generator to start, and
the engines then run at load points with worse specific consumption. The figure is therefore taken
from the optimised fuel consumption of the two worlds, not from a flat `kW × SFOC × hours`
multiplication.

---

## 9. Reproducing it in the tool

Load the scenario. You change **five fields**, and in both runs the battery is **switched off** — so
the application adds nothing of its own and takes your figures literally. Set the sea margin to
**0** so you can enter effective values directly.

| Panel | Field | Run A | Run B |
|---|---|---|---|
| Battery Configuration | Enable Battery | off | off |
| Vessel / Propulsion | Sea Margin (%) | 0 | 0 |
| Vessel / Propulsion | Propulsion Power | **13047.265** | **13239.765** |
| Transit Mode | Hotel/Mission Power | **4001** | **4126** |
| DP Mode | Required DP Power | 1000 | **1800** |
| DP Mode | Hotel/Mission Power | **3820** | **3820** |

Everything else — hours, port, manoeuvring, plant, fuel price — stays unchanged.

If you would rather not touch the sea margin, leave it at 10 % and enter `11861.15` (A) and
`12036.15` (B) instead; multiplied by 1.10 these give the same effective values.

### Which figure to read

**Integration level 1 → Total Fuel & Emissions → Fuel Consumption.**

```
Run A   9 477.49 ton/yr
Run B   9 857.39 ton/yr
        ──────────────
          379.90 ton/yr      × $780/t  =  $296 318
```

### Which figures NOT to read

- **Not the Savings badge, and not Baseline FOC.** With the battery off, the baseline row is chosen
  by a different rule (`count − 1` instead of `max(0, count − 3)`), so the baseline moves between
  the two runs and the savings are not comparable.
- **Not IL2 or IL3.** The benefit is computed from the Level 1 optimum only.
- Mission Heavy Consumer and DP Redundancy have no effect while the battery is off — the allocation
  does not run at all.

### The check that tells you it worked

Run A must return the **same** Level 1 fuel consumption as the original run with the battery
enabled — 9 477.49. If it matches, you have reconstructed World A faithfully and the difference is
trustworthy.

---

## 10. Two easy mistakes

**Switching the battery off is not World B.**

```
World B            =  battery with budget 0    →  the engines carry the WHOLE variation
battery switched off =  no variation at all      →  nobody carries it
```

The second burns *less* than World A and makes the benefit look negative. That is why Run B uses
13 239.8 rather than 12 609.3 — you have to add back by hand the variation the battery was
absorbing.

**Editing only DP.** The battery is active in Transit too. Switch it off and Transit quietly drops
by 639 kW, which sends the result in the opposite direction.

---

## 11. Verification of the whole chain

The header figures in the panel are **hours-weighted averages**, not sums. If the chain above is
right, they come out exactly:

```
ME:  13 547.265 × 2 500  =  33 868 162.5
      1 500     × 1 000  =   1 500 000
        500     ×   500  =     250 000
      8 500     ×   500  =   4 250 000
                            ────────────
                            39 868 162.5  /  4 500 h  =  8 860 kW  ✓

AE:   3 501 × 2 500  =  8 752 500
      3 320 × 1 000  =  3 320 000
      1 500 ×   500  =    750 000
      2 000 ×   500  =  1 000 000
                        ──────────
                        13 822 500  /  4 500 h  =  3 072 kW  ✓
```

Both match the panel header — from the raw inputs through the variations and the priority queue to
Power Demands and the engine loads, with no unexplained number anywhere.

---

## 12. If more of the load should be covered

The battery is the binding constraint in both modes: Transit asks for 956.5 kW and DP for 1 120 kW,
against 800 kW installed. At 800 kW the model serves the highest-priority rows in full and reports
the remainder honestly as uncovered, rather than spreading the budget thinly across all of them.
