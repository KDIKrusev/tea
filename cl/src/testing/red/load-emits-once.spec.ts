import { fakeAsync } from '@angular/core/testing';

import { RestoreHarness } from '../restore-harness';
import { SCENARIO_03_NO_BATTERY } from '../scenarios';
import { CalculatorInput } from '../../app/calculations/calculator.types';

/**
 * RED SUITE · Story C-B, spec 3 — one load, one calculation.
 *
 * Expected to FAIL on today's code. See docs/stories/brownfield-client-b-red-suite.md.
 *
 * Mechanism (established by reading):
 *   #1 `onOperationalProfileLoaded`  vessel-input-form.component.ts:603
 *   #2 `applyProfileInputValues`     vessel-input-form.component.ts:691
 *   #3 the 500 ms `valueChanges` debounce, fed by the `emitEvent:true` patches in
 *      `applyVesselData`, `onVesselEngineConfigSelected` and `populateFormWithProfile`
 *
 * The failure message prints which fields differ between the bodies — that is what tells C-F
 * which emissions to remove rather than merely how many.
 */
describe('RED · a single load produces a single calculation', () => {
  afterEach(() => RestoreHarness.clearStorage());

  it('posts exactly once for a cold load with no saved profile', fakeAsync(() => {
    const harness = RestoreHarness.mountPage();

    harness.api.answerCatalogue();
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    expect(harness.api.postCount())
      .withContext(describeBodies(harness.api.postedBodies()))
      .toBe(1);

    harness.dispose();
  }));

  it('posts exactly once for a hard refresh that restores an auto-draft', fakeAsync(() => {
    const harness = RestoreHarness.mountPage({ draft: SCENARIO_03_NO_BATTERY });

    harness.api.answerCatalogue();
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    expect(harness.api.postCount())
      .withContext(describeBodies(harness.api.postedBodies()))
      .toBe(1);

    harness.dispose();
  }));

  it('posts exactly once for a profile loaded from the Saved Scenarios list', fakeAsync(() => {
    const harness = RestoreHarness.mountPage();

    // Bring the app to the steady state a user is in before clicking Load.
    harness.api.answerCatalogue();
    harness.settle();
    harness.api.answerAllVesselConfig({ vesselConfig: BULK_CARRIER_CONFIG });
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    const before = harness.api.postCount();

    harness.loadProfile(SCENARIO_03_NO_BATTERY);
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    const duringRestore = harness.api.postedBodies().slice(before);
    expect(duringRestore.length).withContext(describeBodies(duringRestore)).toBe(1);

    harness.dispose();
  }));
});

/** The warm-up fetch answers for the auto-selected default category, not for the scenario's. */
const BULK_CARRIER_CONFIG = {
  vesselTypeName: 'Bulk Carrier 110,000 dwt',
  calmWaterPowerKW: 7777,
  seaMargin: 15,
};

/** Names the fields that differ between the posted bodies — the diagnostic C-F needs. */
function describeBodies(bodies: CalculatorInput[]): string {
  if (bodies.length <= 1) {
    return `${bodies.length} calculation request(s)`;
  }
  const keys = new Set<string>();
  for (const body of bodies) {
    Object.keys(body).forEach(k => keys.add(k));
  }
  const differing: string[] = [];
  for (const key of keys) {
    const values = bodies.map(b => JSON.stringify((b as unknown as Record<string, unknown>)[key]));
    if (new Set(values).size > 1) {
      differing.push(`${key}: ${values.join(' → ')}`);
    }
  }
  return (
    `${bodies.length} calculation requests. ` +
    (differing.length > 0
      ? `Fields that changed between them:\n  ${differing.join('\n  ')}`
      : 'All bodies are IDENTICAL — every extra request is pure waste.')
  );
}
