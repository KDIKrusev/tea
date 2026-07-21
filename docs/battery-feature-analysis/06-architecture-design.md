# 06 — Architecture Design: Battery / Spinning Reserve / Peak Shaving / PTI

**Status:** Draft v1 for review · **Date:** 2026-07-13 · **Author:** Winston (Architect)
**Inputs:** `01-task-brief.md`, `02-excel-model-analysis.md`, `03-gap-analysis.md`, decisions D1/D2
(`05-decisions-log.md`), code review of `CalculatorService.cs`, `Level1OptimizationService.cs`,
`CalculatorInput.cs`, Angular `cl/` structure.

---

## 1. Design goals & constraints

1. **Zero regression when no battery is configured** — every new code path is gated on
   `battery is active`; with battery off, results must be bit-identical to today.
2. **Excel is the calculation authority (D2)** — the allocation algorithm and the PTI/PTO
   feasibility gates are ported faithfully; app-specific deviations are isolated and documented.
3. **Additive API evolution** — camelCase JSON contract, existing fields untouched; new inputs are
   optional objects; new outputs are nullable additions. Old clients/profiles keep working.
4. **Testability first** — the battery allocation is a pure, synchronous, stateless component,
   unit-tested against the workbook's saved example before any pipeline wiring.
5. **Progressive complexity** — three decoupled increments (allocation → demand adjustment →
   PTI gates), each shippable and verifiable on its own.

## 2. Where battery logic lives in the pipeline (target state)

```
CalculatorInput (+ BatteryConfigurationInput)
        │
        ▼
ValidationService  ── battery field validation (new rules)
        │
        ▼
CalculatorService.CalculateAllVariantsAsync
        │
        ├── SailContributionService (unchanged; runs first, reduces Transit propulsion)
        │
        ├── NEW BatteryAllocationService.Allocate(mode)      ◄── per relevant mode
        │     input : battery PowerKw budget + mode load demands + variation factors
        │     output: BatteryModeAllocation
        │             { peakShavingBandKw (ΣJ), additionalSpinningReserveKw (ΣL),
        │               committedBatteryKw (ΣI), perLoad[] }
        │
        ├── NEW dual-scenario demand (review decision R3a):
        │     with-battery demand    = avg + ΣL   (battery covers most of the variation)
        │     without-battery demand = avg + ΣH   (all variation carried by gensets)
        │     → both L1 runs per relevant mode; Δ = "Battery benefit" line in results
        │
        ├── Level1OptimizationService (per mode)
        │     • mode loads adjusted: + uncovered spinning reserve (Excel: R8 = O7 + ΣL)
        │     • NEW feasibility gates: battery peak-shaving band must fit through
        │       PTI (discharge→shaft) / SG-PTO (charge) capacity          [increment C]
        │     • baseline default: battery ? sorted[max(0, n-3)] : sorted[n-1]   (D1)
        │
        ├── Level2OptimizationService (unchanged semantics — battery is NOT dispatchable, D2;
        │     it simply receives the L1 result computed on adjusted demand)
        │
        ├── Level3DrcService
        │     • effectiveVariation = max(0, variationKw − peakShavingBandKw)   [pending Q4]
        │
        └── Aggregation → AllVariantsCalculationResult (+ BatteryDetails)
```

Key placement decision: **allocation runs in `CalculatorService`, not inside Level 1.**
`Level1OptimizationService` stays a pure "combinations for given loads" engine; the orchestrator
computes battery-adjusted loads and passes them down. This mirrors how sail contribution already
works (`overridePropulsionKw`) and keeps Level 1 testable without battery context.

## 3. Backend design

### 3.1 New domain models (`Models/`)

```csharp
// Models/BatteryConfigurationInput.cs — API input (all optional; null/PowerKw=0 ⇒ feature off)
public class BatteryConfigurationInput
{
    public double CapacityKwh { get; set; }          // sketch: Capacity [kWh]
    public double PowerKw { get; set; }              // sketch: Power [kW] = Excel budget (I2)
    public List<OperationalMode> RelevantModes { get; set; } = new();  // Transit / DP / Port
    public bool IsActive => PowerKw > 0 && RelevantModes.Count > 0;
}

// Models/BatteryAllocation.cs — allocation engine output (per mode)
public class BatteryModeAllocation
{
    public OperationalMode Mode { get; set; }
    public List<BatteryLoadAllocation> Loads { get; set; } = new();
    public double PeakShavingBandKw { get; set; }             // ΣJ — the ± band battery covers
    public double AdditionalSpinningReserveKw { get; set; }   // ΣL — uncovered → gensets carry it
    public double CommittedBatteryKw { get; set; }            // ΣI
    public double RemainingBatteryKw { get; set; }            // final K
}

public class BatteryLoadAllocation                            // one row of the Load Demands sheet
{
    public string LoadName { get; set; }                      // "DP reserve", "Propeller", "Hotel"…
    public BatteryFunction Function { get; set; }             // Reserve | PeakShaving
    public double CoverageFactor { get; set; }                // 1.0 / 0.5 / 0.35 / 0.05
    public double AverageLoadKw { get; set; }                 // E
    public double VariationKw { get; set; }                   // H = G − E
    public double BatteryUsedKw { get; set; }                 // I = min(remaining, H)
    public double CoveredBandKw { get; set; }                 // J = I × D
    public double UncoveredReserveKw { get; set; }            // L = H − J
}

public enum BatteryFunction { Reserve, PeakShaving }
```

`CalculatorInput` gains one property: `public BatteryConfigurationInput? Battery { get; set; }`.
The legacy `BatteryCapacity` field stays for wire-compatibility; when `Battery == null` and
`BatteryCapacity > 0` we do **not** infer anything (it was always dead — keep it dead, deprecate in
XML doc). Profiles migrate explicitly on the client (see §4.3).

### 3.2 New service — `BatteryAllocationService` (pure logic, the Excel Load Demands port)

```csharp
public interface IBatteryAllocationService
{
    BatteryModeAllocation Allocate(OperationalMode mode, CalculatorInput input);
}
```

- **Stateless, synchronous, no I/O** → trivially unit-testable (mirror of `CalculationHelpers`
  philosophy). Registered scoped like the other calc services in `Program.cs`.
- Builds the per-mode load rows from `CalculatorInput` mode fields, then cascades the budget in
  priority order exactly as the sheet does (see `02-excel-model-analysis.md` §1.3).
- **Priority order, coverage factors and variation factors come from configuration**, not code:

```jsonc
// appsettings.json — new section, defaults = the Excel workbook values
"BatterySettings": {
  "PtiLossFactor": 0.05,
  "ChargeEfficiency": 0.97,          // MachCalcTool ηBatteryCharge
  "DischargeEfficiency": 0.97,       // MachCalcTool ηBatteryDischarge
  "ElectricMotorEfficiency": 0.965,  // MachCalcTool ηElectricMotor (PTI/PTO sizing)
  "LoadPriorities": [                // order = allocation priority (Excel rows 5→9)
    { "load": "DpReserve",     "function": "Reserve",     "coverageFactor": 1.00, "variationFactor": 0.00 },
    { "load": "DpDemand",      "function": "PeakShaving", "coverageFactor": 0.50, "variationFactor": 0.00 },
    { "load": "Mission",       "function": "PeakShaving", "coverageFactor": 0.50, "variationFactor": 0.00 },
    { "load": "Propulsion",    "function": "PeakShaving", "coverageFactor": 0.35, "variationFactor": 0.05 },
    { "load": "Hotel",         "function": "PeakShaving", "coverageFactor": 0.05, "variationFactor": 0.02 }
  ]
}
```

Rationale: the domain expert will want to tune factors without redeploying; and per-mode load
mapping differs (Transit has Propulsion+Hotel; DP has DpReserve+DpDemand+Hotel; Port has Hotel).
The mode→loads mapping is code (small, typed), the numbers are config.

> Design note (Q1, still open): `CapacityKwh` is carried through the model but in this design only
> validated for plausibility (e.g. warning if `CapacityKwh < PowerKw × 0.5h`). No SoC simulation.

### 3.3 Level 1 changes (`Level1OptimizationService`)

Three surgical changes, all gated:

1. **Adjusted mode loads.** `FindOptimalCombinationAsync` gets an optional parameter object instead
   of growing more positional args:

```csharp
public record Level1Adjustments(
    double? OverridePropulsionKw,      // existing sail override moves in here
    double AdditionalReserveKw,        // ΣL from allocation → added to demand (Excel R8)
    double PeakShavingBandKw,          // ΣJ → PTI/PTO feasibility gate (increment C)
    int? BaselineIndex,
    bool BatteryActive);
```

   The added reserve is applied to the demand the combination must *carry* (sufficiency check +
   load distribution input), split pro-rata between propulsion and hotel per the allocation's
   per-load `UncoveredReserveKw` (Excel adds R5/R6 per-load, we keep that granularity —
   propulsion-side reserve raises `propulsion`, hotel-side raises `hotel`).

   **Dual-scenario rule (review decision R3a, 2026-07-13).** The Excel evaluates one scenario
   (plant *with* battery); the app reports savings. To show the battery's own benefit honestly,
   when the battery is active the orchestrator runs Level 1 **twice per relevant mode**:
   - *with-battery*: demand = avg + ΣL (uncovered reserve only) — this run feeds the normal
     L1→L2→L3 pipeline and all tier results;
   - *without-battery reference*: demand = avg + **ΣH** (the full variation carried as genset
     spinning reserve; allocation evaluated with budget = 0).
   The FOC delta between the two optimal setups is reported as a separate **"Battery benefit"**
   line (`BatteryDetails.BenefitFocTonPerYear`) — it is *not* folded into the L1/L2/L3 tier
   savings, so tier semantics stay comparable with and without battery. Cost: one extra L1
   enumeration per relevant mode (~100 combos) — negligible.

2. **Baseline pre-selection (D1):**

```csharp
int defaultBaselineIndex = adjustments.BatteryActive
    ? Math.Max(0, sorted.Count - 3)     // "third highest" — clamped for small lists
    : sorted.Count - 1;                 // current behaviour, unchanged
```

   User override via `BaselineIndex` continues to win. `Level1Result.SelectedBaselineIndex` already
   flows to the client, so the Assumed Configuration table keeps working with no contract change.

3. **PTI/PTO feasibility gates (increment C).** New per-combination fields on `EngineCombination`:
   `PtiPowerKw`, `AvailablePtiForBatteryKw`, `AvailablePtoForBatteryKw`. In `IsValid`/
   `DistributeLoad`:
   - *PTI need*: if `propulsion > ME capacity` for the combo, the deficit may be delivered as PTI
     from the AE side, capped at `ActiveMeCount × MaxPtiPerEngineKw`; aux demand grows by
     `pti × (1 + PtiLossFactor)`. (Today such combos are simply invalid — PTI makes new combos
     valid, matching the Excel.)
   - *Battery gates*: combo is invalid if `AvailablePtiForBatteryKw < PeakShavingBandKw`
     ("Insufficient PTI" — battery cannot discharge through the shaft) or
     `AvailablePtoForBatteryKw < PeakShavingBandKw` (cannot recharge). PTO availability derives
     from unused SG capacity (SG **is** the PTO — see `03-gap-analysis.md` §4.2).
   - **`MaxPtiPerEngineKw` is a new optional input** on `CalculatorInput` (per ME).
     **Default when absent: 0 (no PTI).** A non-zero default (e.g. = SG capacity) would silently
     enlarge the valid-combination space for *existing* users and change their results — violating
     the zero-regression goal. Instead, the **client pre-fills a suggestion** = `SgCapacityPerEngine`
     when the battery section is enabled (Excel plants are symmetric: Max PTI = Max PTO = 3 250),
     and the user confirms/edits it. No DB migration needed in this phase; a `MaxPtiKW` column on
     `EngineType` is a later enhancement mirroring `ShaftGeneratorMaxCapacityKW`.

### 3.4 Level 3 interaction (pending Q4 — default rule proposed)

`Level3DrcService.CalculateDrcSavingsAsync` receives the mode's `PeakShavingBandKw` and computes
`effectiveVariation = max(0, variationKw − peakShavingBandKw)` before the DRC 20 % reduction.
This prevents double-counting by construction; if Q4 is answered differently the change is local to
one method.

### 3.5 Response contract additions

```csharp
// AllVariantsCalculationResult — new nullable member (omitted from JSON when null)
public BatteryDetails? BatteryDetails { get; set; }

public class BatteryDetails                       // built from per-mode allocations
{
    public double CapacityKwh { get; set; }
    public double PowerKw { get; set; }
    public double SpinningReserveKw { get; set; } // ΣL across relevant modes — the sketch's field
    public double PeakShavingKw { get; set; }     // ΣJ across relevant modes — the sketch's field
    public double BenefitFocTonPerYear { get; set; }   // R3a: with-battery vs without-battery Δ
    public double BenefitCostPerYear { get; set; }     // BenefitFocTonPerYear × fuelPrice
    public List<BatteryModeAllocationDto> ModeAllocations { get; set; } = new();
}
```

Note how the sketch's two "Functions" fields materialize: they are **outputs** (computed by the
allocation) surfaced with exactly the names the requester used. `ValidCombinationDto` gains
optional `ptiKw` / `availablePtiKw` for transparency in the baseline table (nullable → old clients
unaffected).

### 3.6 Validation rules (`ValidationService`)

- `Battery.PowerKw ≥ 0`, `Battery.CapacityKwh ≥ 0`; if `PowerKw > 0` then `CapacityKwh > 0`.
- `RelevantModes` ⊆ {Transit, DP, Port} (extend if Q5 says otherwise); DP in modes requires
  `DpEnabled`.
- Warning (not error) if `CapacityKwh` cannot sustain `PowerKw` for 30 min (placeholder threshold,
  Q1).
- `MaxPtiPerEngineKw ≥ 0`.

## 4. Frontend design (Angular 18, `cl/`)

### 4.1 Form — new standalone section component

`features/vessel-input/battery-config-section/` — same pattern as the existing five sections
(`[parentForm]` input, OnPush). Placed after Engine Configuration, per the sketch:

```
Battery configuration                     [enable toggle]
  Capacity [kWh]   Power [kW]
  Functions (computed after calculation — read-only):
    Spinning Reserve: {batteryDetails.spinningReserveKw} kW
    Peak Shaving:     {batteryDetails.peakShavingKw} kW
  Relevant Modes:  ☐ Transit  ☐ DP  ☐ Port     (DP checkbox disabled unless DP mode enabled)
```

Form model addition (nested group keeps `buildCalculatorInput` mapping mechanical):

```ts
battery: this.fb.group({
  enabled:     [false],
  capacityKwh: [0, [Validators.min(0)]],
  powerKw:     [0, [Validators.min(0)]],
  modes:       this.fb.group({ transit: [false], dp: [false], port: [false] })
})
```

`calculator.types.ts`: `CalculatorInput.battery?: BatteryConfigurationInput` +
`AllVariantsCalculationResult.batteryDetails?: BatteryDetails` — mirrors backend exactly.

### 4.2 Results

- **Battery Contribution panel** (new expansion panel, rendered only when `batteryDetails` present):
  the computed SR/PS values, per-mode allocation table (load, function, covered band, uncovered
  reserve), and the demand adjustment shown as `raw demand → adjusted demand`.
- **Baseline panel**: when battery active and no manual override, show hint
  "Default baseline: 3rd-highest consumption (battery installed)". The combination table itself is
  unchanged — `selectedBaselineIndex` already drives the radio selection.
- Report (`ReportService`): one "Battery" block in basis-of-estimate; out of the critical path.

### 4.3 Profile schema migration

`PROFILE_SCHEMA_VERSION: 2 → 3`. Migration on load: v2 profiles get
`battery = { enabled: false, capacityKwh: profile.batteryCapacity ?? 0, powerKw: 0, modes: {} }`.
Auto-draft uses the same migration. Export keeps writing v3.

## 5. Increments (build order) — each independently shippable

| # | Increment | Contents | Verification |
|---|---|---|---|
| A | **Allocation engine** | `BatteryAllocationService` + `BatterySettings` + models; not wired to pipeline | Unit tests reproduce the workbook example: budget 1 260 → ΣJ = 204.40, ΣL = 444.75, per-row I/J/K/L values |
| B | **Demand adjustment + baseline rule + contract** | `Battery` input, orchestrator wiring, adjusted L1 loads, `sorted[n−3]` default, `BatteryDetails` response, validation | Regression suite: battery off ⇒ byte-identical JSON; battery on ⇒ demand raised by ΣL, baseline index shifts |
| C | **PTI/PTO gates** | `MaxPtiPerEngineKw`, PTI in `IsValid`/`DistributeLoad`, battery feasibility gates, combo DTO fields | Excel row-level reconciliation on selected combinations (mind ranking-metric difference, `03` §4.4) |
| D | **Client** | Form section, types, panels, profile v3 | e2e smoke; old v2 profile loads cleanly |
| E | **L3 residual rule** | Effective-variation change (Q4) | Targeted L3 unit tests |

**Confirmed build order (review R7): A → B → D → C → E.** A→B→D ships a working, user-visible
battery feature (bus-level, dual-scenario benefit, no PTI gates); C then tightens it to Excel
fidelity; E closes the L3 double-counting rule. This de-risks the largest unknown (PTI) without
blocking user-visible progress.

## 6. Architectural risks & mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| **Homogeneous vs heterogeneous plant**: the app models N *identical* MEs + N *identical* AEs; the Excel has 6 individually-sized machines with per-machine SFOC curves | Exact Excel number reconciliation impossible for mixed plants | Accept: reconcile on symmetric scenarios; document as known deviation (same class as the ranking-metric difference) |
| PTI enlarges the valid-combination space → more L1 work + changed sorting | Perf negligible (≤ ~100 combos), but **baseline index meaning shifts** when new combos appear | **Decided (R5a):** the client pins a user-selected baseline by **combination signature** (`activeMeCount/sgEnabled/activeAeCount`), resolving it to an index against each fresh `validCombinations` list before re-POSTing; falls back to the default rule with a hint if the signature disappears |
| Double-counting battery PS with L3 DRC | Inflated Premium savings | Increment E rule (residual variation), default ON |
| Old profiles / old clients | Breakage on contract change | All additions nullable/optional; profile migration v2→v3; contract tests |
| Config drift (coverage factors) between environments | Silent number changes | Log effective `BatterySettings` at startup; include factors in `BatteryDetails` response for auditability |
| `AnnualHours` computed property ignores Port/Anchor/Maneuvering (`CalculatorInput.cs:115`) — pre-existing quirk that battery-mode weighting could expose | Mode-weighted battery savings mis-scaled | Do **not** reuse `AnnualHours` for battery aggregation; sum the relevant modes' hours explicitly |

## 7. Explicit non-goals of this design

- No SoC/time-domain battery simulation, no degradation model (out of scope per brief).
- No DB schema changes (PTI capacity is an input with SG-based default; `EngineType.MaxPtiKW`
  column is a future enhancement).
- No change to Level 2's algorithm (battery is not a dispatchable setpoint unit — D2).
- No new endpoints — the single `calculate-all-variants` contract grows additively.

## 8. Decision record (architect)

| ADR | Decision | Why |
|---|---|---|
| ADR-1 | Allocation in orchestrator, not in Level 1 | Level 1 stays a pure combinations engine; mirrors sail-override precedent |
| ADR-2 | Coverage factors/priorities in `appsettings` | Domain tuning without redeploy; Excel values as defaults |
| ADR-3 | `Battery` as nested optional object, legacy `BatteryCapacity` left dead | Additive contract; no silent reinterpretation of an always-zero field |
| ADR-4 | SR/PS are computed outputs, not inputs | Decision D2 (follow Excel); sketch fields preserved as displayed values |
| ADR-5 | PTI default = **0** (off); client suggests SG capacity when battery enabled | Non-zero default would change existing users' results (zero-regression); Excel symmetry kept as a UI suggestion |
| ADR-6 | Increment C (PTI) isolated last in backend order | Largest unknown; A+B+D already deliver user value |
