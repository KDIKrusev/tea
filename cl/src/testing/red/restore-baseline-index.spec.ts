import { fakeAsync } from '@angular/core/testing';

import { RestoreHarness } from '../restore-harness';
import { SCENARIO_15_BASELINE_PIN } from '../scenarios';

/**
 * RED SUITE · Story C-B, spec 2 — `baselineIndex` is lost on profile restore.
 *
 * Expected to FAIL on today's code. See docs/stories/brownfield-client-b-red-suite.md.
 *
 * Mechanism (established by reading, confirmed here):
 *   `applyProfileInputValues` calls `endRestore()` at vessel-input-form.component.ts:689 and
 *   `emitFormValues()` at :691 — in that order. `emitFormValues` reads `restoreInFlight` at emit
 *   time, so the restore's own terminal emission is tagged `'user'`. `CalculatorPageComponent`
 *   clears the pinned baseline on any `'user'` emission (calculator-page.component.ts:260-262).
 *   The restore therefore destroys the very value it was carrying.
 *
 * Repro: scenario 15, which pins the worst combination row (index 4).
 */
describe('RED · profile restore preserves the pinned baseline', () => {
  afterEach(() => RestoreHarness.clearStorage());

  it('sends the profile\'s baselineIndex on the calculation that follows the restore', fakeAsync(() => {
    const harness = RestoreHarness.mountPage();

    harness.api.answerCatalogue();
    harness.loadProfile(SCENARIO_15_BASELINE_PIN);
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    expect(SCENARIO_15_BASELINE_PIN.input.baselineIndex)
      .withContext('fixture sanity — scenario 15 must pin a baseline')
      .toBe(4);

    const bodies = harness.api.postedBodies();
    expect(bodies.length).withContext('the restore must produce a calculation').toBeGreaterThan(0);

    expect(bodies[bodies.length - 1].baselineIndex)
      .withContext(
        `the last request must carry the pinned baseline; observed sequence: ` +
          `${JSON.stringify(bodies.map(b => b.baselineIndex))}`,
      )
      .toBe(4);

    harness.dispose();
  }));

  it('does not tag any emission as a user edit before the restore has settled', fakeAsync(() => {
    // This is the CAUSE assertion. The test above observes the symptom; this one pins the reason,
    // so a fix that merely re-reads the pin somewhere else cannot make both pass.
    const harness = RestoreHarness.mountPage();

    harness.api.answerCatalogue();
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    const beforeRestore = harness.emissions().length;
    harness.loadProfile(SCENARIO_15_BASELINE_PIN);
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    const duringRestore = harness.emissionSources().slice(beforeRestore);
    expect(duringRestore.length).withContext('the restore must emit at least once').toBeGreaterThan(0);
    expect(duringRestore.filter(s => s === 'user'))
      .withContext(`every emission of a restore must be tagged 'restore'; observed: ${JSON.stringify(duringRestore)}`)
      .toEqual([]);

    harness.dispose();
  }));
});
