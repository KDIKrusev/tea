import { fakeAsync } from '@angular/core/testing';

import { RestoreHarness } from '../restore-harness';
import { SCENARIO_15_BASELINE_PIN } from '../scenarios';

/**
 * Story C-F — the one calculation nothing else covers.
 *
 * Re-picking the assumed baseline is deliberately *silent*: no spinner, panels left alone. That is
 * exactly why a duplicate request here would be invisible in the UI — it would only ever show up
 * as a second line in the network tab. The other three intents (cold start, restore, field edit)
 * are covered by `load-emits-once.spec.ts` and `user-edits.spec.ts`.
 */
describe('re-picking the assumed baseline', () => {
  afterEach(() => RestoreHarness.clearStorage());

  /** Scenario 15 restored and quiesced, with its pinned baseline (index 4) in place. */
  function settledWithPin(): RestoreHarness {
    const harness = RestoreHarness.mountPage();
    harness.api.answerCatalogue();
    harness.loadProfile(SCENARIO_15_BASELINE_PIN);
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();
    return harness;
  }

  it('posts exactly once, carrying the index just picked', fakeAsync(() => {
    const harness = settledWithPin();
    const before = harness.api.postCount();

    harness.pageComponent.onBaselineIndexChanged(2);
    harness.settle();

    const posted = harness.api.postedBodies().slice(before);
    expect(posted.length).withContext('one re-pick, one request').toBe(1);
    expect(posted[0].baselineIndex).toBe(2);

    harness.api.answerAllCalculations();
    harness.settle();
    harness.dispose();
  }));

  it('is silent — no spinner, and the rendered results stay put', fakeAsync(() => {
    const harness = settledWithPin();
    const resultsBefore = harness.pageComponent.allVariantsResult;
    expect(resultsBefore).withContext('a result must already be on screen').not.toBeNull();

    harness.pageComponent.onBaselineIndexChanged(1);
    harness.settle();

    expect(harness.pageComponent.isCalculating)
      .withContext('a silent request must not raise the spinner')
      .toBe(false);
    expect(harness.pageComponent.allVariantsResult)
      .withContext('the previous results stay until the new answer lands')
      .toBe(resultsBefore);

    harness.api.answerAllCalculations();
    harness.settle();
    harness.dispose();
  }));

  it('does not disturb the form, so no restore is invalidated', fakeAsync(() => {
    const harness = settledWithPin();
    const emissionsBefore = harness.emissions().length;
    const formBefore = JSON.stringify(harness.formValue());

    harness.pageComponent.onBaselineIndexChanged(3);
    harness.settle();

    expect(harness.emissions().length)
      .withContext('the baseline path must not go through the form at all')
      .toBe(emissionsBefore);
    expect(JSON.stringify(harness.formValue())).toBe(formBefore);

    harness.api.answerAllCalculations();
    harness.settle();
    harness.dispose();
  }));

  it('keeps the newly picked baseline across a following field edit', fakeAsync(() => {
    const harness = settledWithPin();

    harness.pageComponent.onBaselineIndexChanged(2);
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    // A real edit is allowed to drop the pin — that is the documented rule
    // (calculator-page.component.ts: a 'user' emission clears it). Assert the rule rather than a
    // hoped-for behaviour, so a future change to it is a deliberate decision and not a surprise.
    harness.formComponent.vesselForm.get('seaMargin')?.setValue(11);
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    const last = harness.api.lastPostedBody();
    expect(last?.seaMargin).toBe(11);
    expect(last?.baselineIndex)
      .withContext('a genuine edit invalidates a pinned baseline, by design')
      .toBeUndefined();

    harness.dispose();
  }));

  it('recalculates even when the same index is picked twice', fakeAsync(() => {
    // The value-dedup added in C-E guards the FORM's emissions. The baseline path does not run
    // through it, and must not start: picking the same row again is an explicit user action.
    const harness = settledWithPin();
    const before = harness.api.postCount();

    harness.pageComponent.onBaselineIndexChanged(2);
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    harness.pageComponent.onBaselineIndexChanged(2);
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    const posted = harness.api.postedBodies().slice(before);
    expect(posted.length).toBe(2);
    expect(posted.map(p => p.baselineIndex)).toEqual([2, 2]);

    harness.dispose();
  }));
});
