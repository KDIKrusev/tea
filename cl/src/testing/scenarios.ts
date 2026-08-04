/**
 * Scenario fixtures — read straight from `docs/qa/manual-test-scenarios/`.
 *
 * Design invariant I3: ONE fixture source. There is deliberately no copy of these files under
 * `cl/`. A client spec and a manual QA scenario must never be able to drift — that drift is what
 * let scenarios 19–35 ship un-importable while every backend golden stayed green.
 *
 * Story C-A · docs/stories/brownfield-client-a-test-harness.md
 */
import { SavedProfile } from '../app/core/profile.types';
import { CalculatorInput } from '../app/calculations/calculator.types';

import scenario01 from '../../../docs/qa/manual-test-scenarios/01-excel-baseline.json';
import scenario03 from '../../../docs/qa/manual-test-scenarios/03-no-battery-reference.json';
import scenario15 from '../../../docs/qa/manual-test-scenarios/15-baseline-user-pick.json';

/** A scenario file is a saved profile WITHOUT the identity fields the importer assigns. */
export type ScenarioFile = Omit<SavedProfile, 'id' | 'createdAt' | 'updatedAt'>;

export const SCENARIO_01_EXCEL_BASELINE = scenario01 as unknown as ScenarioFile;
export const SCENARIO_03_NO_BATTERY = scenario03 as unknown as ScenarioFile;
export const SCENARIO_15_BASELINE_PIN = scenario15 as unknown as ScenarioFile;

/** The three scenarios frozen as client golden request bodies (design I2). */
export const GOLDEN_SCENARIOS: ReadonlyArray<{ key: string; file: ScenarioFile }> = [
  { key: '01-excel-baseline', file: SCENARIO_01_EXCEL_BASELINE },
  { key: '03-no-battery-reference', file: SCENARIO_03_NO_BATTERY },
  { key: '15-baseline-user-pick', file: SCENARIO_15_BASELINE_PIN },
];

/**
 * Turns a scenario file into what `ProfileService` would have stored, so specs exercise the same
 * `loadProfile(SavedProfile)` entry point the UI uses. Timestamps are fixed, not `Date.now()` —
 * a spec must not depend on the clock.
 */
export function asSavedProfile(file: ScenarioFile, id = 'spec-profile'): SavedProfile {
  return {
    ...file,
    id,
    createdAt: '2026-01-01T00:00:00.000Z',
    updatedAt: '2026-01-01T00:00:00.000Z',
  };
}

/**
 * A scenario with individual input fields overridden.
 *
 * Needed because every one of the 35 scenario files stores a `fuelPrice` exactly equal to its main
 * fuel's backend default (MDO 780, LNG 620, HFO 420, Ammonia 1350). A spec that wants to observe
 * whether a *user-chosen* price survives a restore has to synthesise one — see design §7.1.
 */
export function withInput(file: ScenarioFile, overrides: Partial<CalculatorInput>): ScenarioFile {
  return { ...file, input: { ...file.input, ...overrides } };
}
