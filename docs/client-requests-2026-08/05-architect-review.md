# Epic E1 — Architect Review of the Implemented Changes

**Architect:** Winston (BMAD) · **Date:** 2026-08-13 · **Scope:** the full wave diff
(`5b82a4a..HEAD` + this review's own fixes) — COS-A, DE-A..DE-D.
**Verdict: READY.** The epic is architecturally sound and complete; three findings were fixed in
this review pass (472/472 green after), two are recorded as accepted debts.

## 1. Design-conformance check

| Design rule | Status |
|---|---|
| Single gate `MeCount == 0`; MeCount ≥ 1 instruction stream identical | ✅ the only production branch points are `TryDistribute` (early branch), the validation slices, and the tally sentence — all gated |
| §5 "do not touch" list (cascade, adapter, pipeline runner, baselines, SFOC, PTI assist, builders, L2/L3) | ✅ none of those files appear in the diff |
| No serializable member on `CalculatorInput` | ✅ `PlantShape` helper |
| Loss factor applied at distribution only; demands stay user-entered | ✅ threaded as a parameter, builder stays pure |
| 400-order append-only | ✅ pinned suites green; golden 35 diff-clean |
| Frozen goldens | ✅ `git diff --stat` on `Expected/` empty at every story boundary; GOLDEN_UPDATE used once, filtered, with 4 additions only |
| No new client emission sources | ✅ gating writes are `emitEvent:false`; restore handled by an explicit refresh hook (existing idiom) |

## 2. Findings fixed in this pass

1. **Predicate consistency (self-inflicted).** The design's own §1 principle — one predicate, one
   place — was violated by four inline `MeCount == 0` checks in `ValidationService` and
   `Level1RejectionTally`. All now call `PlantShape.IsDieselElectric`. Deliberately NOT converted:
   the `MeCount >= 1` conditionals (they are not the negation of diesel-electric — a negative
   count is neither — and rewriting them would change behaviour for negative inputs) and the
   golden host's guard (same reason).
2. **`ValidateSystemCapacity` structure.** The DE branch had grown the method to ~90 lines of
   if/else — against the file's own named-slice idiom. Extracted verbatim into
   `ValidateDieselElectricCapacity` / `ValidateMechanicalPlantCapacity`; the shared advisory tail
   stays in the caller.
3. **Architecture doc drift.** §3 said config key `Calculator:…`; the real section is
   `CalculatorSettings:…`. The doc now matches the code (the story record had already flagged it;
   the doc itself is what future readers open).

## 3. Accepted debts (recorded, not fixed)

1. **Optional `IOptions<CalculatorSettings>?` constructor parameter** on
   `Level1OptimizationService`. A required parameter is the cleaner shape, but it would churn
   ~15 direct test constructions for zero behavioural gain; DI always supplies it in production
   and the default is the config default (0). Revisit only if the service grows a third options
   dependency.
2. **`EngineConfigSectionComponent` size.** The gating block (+76 lines) is cohesive and
   documented, but the component was already the form's heaviest. When the next client-refactor
   epic opens, the park/wake pair plus the shaft-bound list is a ready-made extraction seam
   (a `DieselElectricFormGate` service). Not worth a standalone change now — the client has
   exactly one consumer of that logic.

## 4. Looked for and NOT found

- Hidden coupling of the DE branch to mode identity (it ignores `mode` — correct: Port/Anchor
  hotel-only demands flow through the same arithmetic with propulsion 0).
- Battery-side asymmetries at 0 ME (both L piles land on the one AE side; scenario 38's world gap
  equals the PS tile exactly — the invariant held in the golden, not just the unit test).
- Golden-host divergence from `Program.cs` (the calcOptions wiring closed the one gap; behaviour
  identical since the key is absent).
- NaN paths on the wire from parked controls (`Number(null) = 0` mapped and spec-pinned).

## 5. Readiness statement

Epic E1 is releasable as built: deploy needs binaries + appsettings + `ng build` output only
(no DB scripts — the DE family adds no schema or data). The two follow-ups that remain are
process, not code: client verification of one DE scenario against his reference (promotes 36–39
to proof), and the one-eyeball UI pass of scenario 36. Epic E2 stays parked on O-1..O-5.
