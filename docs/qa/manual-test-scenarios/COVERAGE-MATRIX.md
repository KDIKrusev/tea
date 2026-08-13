# Golden Scenario Coverage Matrix

*Built 2026-08-04 by Quinn (Test Architect). Every "covered" cell below was verified by reading the
approved snapshot, not by reading the scenario file — a scenario that sets a field but never reaches
the code path it was meant to exercise counts as **not covered**.*

35 scenarios. 405 tests green. Each scenario is one end-to-end run whose **entire** response is
compared byte-for-byte with its approved snapshot.

---

## How this matrix was built

The suite grew feature by feature, so it was deep on battery behaviour and thin everywhere else.
Three checks exposed that:

1. **Which snapshot shows a non-zero value for X?** — not "which scenario sets X".
   This is what revealed that **Level 2 produced zero savings in all 18** original scenarios, and
   that **PTI assist never actually engaged** despite two scenarios named after it.
2. **Which branch of each user-facing message is reached?** — only 2 of the 4 Level 1 rejection
   messages had a golden scenario.
3. **Which plant shapes exist?** — 15 of 18 scenarios used the *same* plant.

---

## 1 · Optimization levels

| Path | Scenarios | Notes |
|---|---|---|
| L1 savings only | 01–10, 12, 13, 15–18, 27–30, 32, 34 | the common case |
| **L2 savings > 0** | **19, 23, 24, 25, 26, 31, 33** | needs ≥2 aux running AND a curve region where an asymmetric split wins |
| L3 savings > 0 | 11, 19, 23–26, 31, 33 | |
| **All three tiers differ** | **19, 23, 24, 25, 26, 31, 33** | Advanced ≠ Pro ≠ Premium |
| Zero savings at every level | 14 | one valid combination — optimum *is* the baseline |
| L2 pass-through (0 or 1 aux running) | 01–18, 27–30, 32 | nothing to redistribute |

**Why L2 was invisible for so long:** the aux SFOC curve falls monotonically from 228 g/kWh at 10 %
to 193 at 74–80 %. More load is always better, and Level 1 already picks the fewest engines. Level 2
can only win by an **asymmetric** split — one generator at the 10 % floor, one at the 90 % ceiling —
which needs a demand that a single engine cannot legally carry. Scenario 19 is built for exactly that.

## 2 · Operational modes

| Combination | Scenarios |
|---|---|
| Transit only | 01–06, 08–10, 12, 13, 15, 16, 17, 18, 19, 21, 23–26, 29, 30, 31, 33, 34, 35 |
| Transit + DP | 04, 20, 22, 32 |
| Transit + Port | 07 |
| **Transit + Anchor** | **27** |
| **Transit + Maneuvering** | **28** |
| All five modes | 11 |

Maneuvering is the only non-DP mode besides Transit that carries propulsion — 28 is the only
scenario that exercises it alone.

## 3 · Plant topology

| Shape | Scenarios |
|---|---|
| Shaft generator installed | 01–16, 18, 27–30, 32, 34, 35 |
| **No shaft generator, successful result** | **19, 23, 24, 25, 26, 33** | 
| No shaft generator, rejected | 17, 20, 21, 22 |
| 1 main engine | 14 |
| 2 main engines | all others |
| Optimum runs ≥2 aux engines | 19, 23–26, 31, 33 |
| Optimum runs 0 aux engines | 06, 11, 14 |
| **PTI capacity configured AND engaged** | **32** |
| PTI configured, never engaged | 08, 09 |

**PTI could only be exercised in DP.** The Transit ME-utilisation validation does not account for
PTI, so any Transit plant needing shaft-motor assist is rejected before Level 1 sees it. Scenario 32
puts the deficit in DP mode, where the check does not apply. *(Logged as a question — see below.)*

## 4 · Battery

| Path | Scenarios |
|---|---|
| No battery | 03, 14, 17, 19–28, 31–33, 35 |
| Budget exhausts mid-cascade | 02 |
| **Budget saturated (surplus unused)** | **29** |
| Battery in Transit | 01, 02, 05, 06, 08–10, 12, 13, 15, 16, 29, 30 |
| Battery in DP | 04 |
| Battery in Transit + Port | 07 |
| Battery assigned to a zero-hour mode | 18 |
| Battery configured with **no** relevant modes | 34 |
| Mission heavy consumer | 05, 06 |
| DP class redundancy | 04 |
| L3 residual after battery shaving | 01, 05–08, 10–13, 15, 16, 29, 30 |
| Battery shaves the **entire** variation | 06 |

Scenario 29 proves invariant INV-2 end-to-end: at 10 000 kW the covered band (204.40) and the
spinning reserve (444.75) are **identical** to the 1 260 kW case — only the unused surplus grows.

## 5 · Fuels and CO₂

| Combination | Scenario | ME factor | AE factor |
|---|---|---|---|
| MDO / MGO | most | 3.93267 | 3.93267 |
| LNG / MGO | 13 | 2.753 | 3.93267 |
| **HFO / HFO** | **23** | 3.114 | 3.114 |
| **Ammonia / MGO** | **24** | **0.35154** | 3.93267 |

24 is the widest gap the model can produce — an 11× difference between the two engines' factors on
one vessel. It is the strongest guard against a regression that collapses back to a single constant.

## 6 · Level 3 variation source

| Source | Scenario | Result |
|---|---|---|
| Explicit `hotelLoadVariationKw` | most | as entered |
| Lookup → Bulk Carrier | 14 | 250 kW |
| **Lookup → Container** | **25** | 1 500 kW |
| **Lookup → LNG** | **33** | 1 000 kW |
| **Unknown vessel type → default** | **26** | 500 kW |

All three lookup entries plus the default are now covered. 14 also proves the *substring* match
("Bulk Carrier 10,000 dwt" → "Bulk Carrier").

## 7 · Baseline selection

| Path | Scenario |
|---|---|
| Default: highest-FOC combination | 03, 17, 19–28, 31–33 |
| Default with battery: third-highest (D1) | 01, 02, 04–16, 29, 34 |
| User-pinned index | 15 |
| **Pinned index out of range → default** | **30** |

30 produces a snapshot identical to 01's baseline block — which is the assertion: an index of 99
must be ignored, not clamped to the last row.

## 8 · Failure and advisory paths

| Response | Scenario | What it proves |
|---|---|---|
| 400 · ME utilisation > 100 % | 17 | validation, before Level 1 |
| 400 · battery PTI discharge gate | 09 | `RejectionTally` branch 1 |
| **400 · engines cannot carry the demand** | **20** | branch 2 |
| **400 · aux above 90 % load** | **21** | branch 3 |
| **400 · no configuration can cover** | **22** | branch 4 (structural fallback) |
| **400 · several errors at once (5)** | **35** | the error list and its order |
| 200 + battery capacity warning | 10 | |
| 200 + operating-hours warning | 14 | profile of 8 935 h |
| **200 + two advisory warnings** | **34** | battery without modes · DP redundancy without DP |

All four `Level1RejectionTally.ExplainFor` branches now have end-to-end coverage. The wording of each
is additionally pinned by `Level1RejectionDiagnosticsTests`.

---

## What is still NOT covered — deliberately

| Gap | Why it is left |
|---|---|
| Every individual validation message | 25+ of them; `ValidationServiceTests` pins each. The golden suite covers the *response shape*, not every string. |
| Sail changing the chosen combination | 12 covers the sail path; a plant where sail flips the optimum would be contrived. |
| A vessel with a mixed-unit category | Backend logs a warning; no client-visible effect. |
| Leap-year hours (8 784) | The model is annual; 8 760 is the stated constant. |

## Open questions this exercise raised

1. **The 10 % / 90 % split.** In scenarios 19/23–26/31/33 Level 2's answer is one generator at the
   minimum and one at the ceiling. A crew would more likely run a single generator at 100 % — which
   the 90 % ceiling forbids. **Does the reference workbook agree with 10/90?** If not, the floor or
   the ceiling may be mis-set.
2. **PTI can never engage in Transit**, because the ME-utilisation validation ignores PTI capacity.
   Intended, or an oversight? Today it means the shaft-motor feature is DP-only in practice.
3. **Scenario 14 remains 175 h over a year.** It now carries the advisory warning rather than being
   corrected, so the scenario also documents what an over-long profile looks like.

## Diesel-electric wave (36–39, Epic E1)

| Mechanism | Covered by |
|---|---|
| 0-ME distribution (AE = hotel + propulsion × (1 + loss factor)) | 36, 37, 38 |
| 90 % AE cap policing the whole electric load | 36 (ae=3 rejected at 91.7 %) |
| **Level 2 live with no SG** (unequal AE split) | **36** (+41.675 t/yr — only 19 also has non-zero L2) |
| DP thrust + uncovered DpReserve on the AE side, PTI gate inert | 37 |
| Cascade + both Benefit worlds at 0 ME; with-battery baseline clamp | 38 (world gap = PS 178 exactly) |
| Diesel-electric AE-capacity 400 (ME-shaped messages absent) | 39 |
| Not covered by a scenario: the L1 rejection sentence when the 90 % cap kills every combination after validation passes | unit test (`AllRejectedByTheCap_ExplainsWithTheDieselElectricSentence`) |
| Not covered: `ElectricPropulsionLossFactor ≠ 0` end-to-end | unit test only (config default is 0; a non-zero value is a product decision away) |

## Approval status

Scenarios 01–18 were verified against the reference workbook during the original QA passes.

**Scenarios 19–39 were generated from the current code.** They are *characterisation* snapshots:
they prove the behaviour does not change silently, and each one was checked to actually reach the
path it targets. Where an expected figure could be derived by hand it is shown in the matching card
under **Hand-check**. Figures that require the workbook are marked **pending reference verification**
and should be confirmed before these are treated as correctness proofs rather than change detectors.
