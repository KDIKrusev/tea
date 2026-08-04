import { fakeAsync } from '@angular/core/testing';

import { RestoreHarness } from '../restore-harness';
import { SCENARIO_03_NO_BATTERY } from '../scenarios';

/**
 * The paths C-E could most easily have broken, and that nothing else covers.
 *
 * The red suite only exercises startup and restore. Everything a user does afterwards — typing in
 * a field, changing the size, switching category — goes through the debounced `valueChanges` path,
 * which C-E now guards with a load sequence and a value-equality check. If either guard is too
 * eager, the app silently stops recalculating and no red spec notices.
 *
 * These are characterisation tests: they lock in behaviour, they do not judge it.
 */
describe('user edits after a load has settled', () => {
  afterEach(() => RestoreHarness.clearStorage());

  /** Startup, quiesced, with the scenario's vessel selected. */
  function settledApp(): RestoreHarness {
    const harness = RestoreHarness.mountPage({ draft: SCENARIO_03_NO_BATTERY });
    harness.api.answerCatalogue();
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();
    return harness;
  }

  it('recalculates once when a plain field is edited', fakeAsync(() => {
    const harness = settledApp();
    const before = harness.api.postCount();

    harness.formComponent.vesselForm.get('seaMargin')?.setValue(15);
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    const posted = harness.api.postedBodies().slice(before);
    expect(posted.length).withContext('one edit, one calculation').toBe(1);
    expect(posted[0].seaMargin).toBe(15);
    expect(harness.emissionSources().slice(-1)).toEqual(['user']);

    harness.dispose();
  }));

  it('recalculates when an edit is undone, because the panels show the intermediate result', fakeAsync(() => {
    const harness = settledApp();
    const before = harness.api.postCount();

    const control = harness.formComponent.vesselForm.get('seaMargin');
    const original = control?.value;
    control?.setValue(15);
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();
    control?.setValue(original);
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    // The dedup guard compares against the LAST emission, not the whole history — deliberately.
    // After the first edit the panels show the sea-margin-15 result, so returning to 0 is new
    // information and must recalculate. A history-wide cache would leave the wrong numbers on
    // screen.
    const posted = harness.api.postedBodies().slice(before);
    expect(posted.length).toBe(2);
    expect(posted.map(p => p.seaMargin)).toEqual([15, original]);

    harness.dispose();
  }));

  it('fetches once and recalculates when the vessel size changes', fakeAsync(() => {
    const harness = settledApp();
    const before = harness.api.postCount();

    harness.formComponent.vesselForm.get('vesselSize')?.setValue(9000);
    harness.tick(500); // past the 400 ms fetch debounce

    expect(harness.api.pendingVesselConfig())
      .withContext('one size change, one vessel fetch')
      .toBe(1);
    expect(harness.api.vesselConfigParams().pop()?.size).toBe('9000');

    harness.api.answerAllVesselConfig({
      vesselConfig: {
        vesselTypeName: 'Offshore Support 9,000 dwt',
        calmWaterPowerKW: 6543,
        seaMargin: 12,
      },
    });
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    const posted = harness.api.postedBodies().slice(before);
    expect(posted.length).withContext('a size change must still produce a calculation').toBeGreaterThan(0);
    expect(posted[posted.length - 1].propulsionPower)
      .withContext('the new curve value must reach the wire')
      .toBe(6543);

    harness.dispose();
  }));

  it('applies the new category defaults when the category changes', fakeAsync(() => {
    const harness = settledApp();
    const before = harness.api.postCount();

    harness.formComponent.vesselConfigSection.onCategoryChange('Container');
    harness.tick(500);

    expect(harness.api.vesselConfigParams().pop()?.category).toBe('Container');

    harness.api.answerAllVesselConfig({
      vesselConfig: {
        vesselTypeName: 'Container 10,000 TEU',
        calmWaterPowerKW: 31000,
        seaMargin: 18,
      },
      resolution: {
        lowerRefSize: null,
        upperRefSize: null,
        t: null,
        profileSource: 'Container 10,000 TEU',
        clamped: false,
      },
    });
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    const posted = harness.api.postedBodies().slice(before);
    expect(posted.length).withContext('a category change must produce a calculation').toBeGreaterThan(0);

    const last = posted[posted.length - 1];
    expect(last.propulsionPower).toBe(31000);
    expect(last.seaMargin).toBe(18);
    // The category's own engine defaults win here — no profile is being restored.
    expect(last.mainEngineTypeId).toBe(2);
    expect(last.meCapacityPerEngine).toBe(15000);

    harness.dispose();
  }));

  it('unblocks the form after a failed vessel fetch instead of waiting forever', fakeAsync(() => {
    // The 1500 ms fallback timer used to cover this; `vesselDataFailed` replaces it, so the load
    // sequence ends explicitly and the form is free to emit again.
    //
    // No calculation is POSTed either way: without a vessel response `propulsionPower` is never
    // filled, and `CalculatorPageComponent.onFormChange` correctly declines to calculate a plant
    // with no propulsion demand. The assertion is therefore about the form emitting at all —
    // which is exactly the property that would be lost if a sequence could never end.
    const harness = RestoreHarness.mountPage();
    harness.api.answerCatalogue();
    harness.tick(500);

    harness.api.failVesselConfig();
    harness.settle();

    harness.formComponent.vesselForm.get('meCapacityPerEngine')?.setValue(12345);
    harness.settle();

    const emissions = harness.emissions();
    expect(emissions.length).withContext('a failed fetch must not freeze the form').toBeGreaterThan(0);
    expect(emissions[emissions.length - 1].input.meCapacityPerEngine).toBe(12345);
    expect(harness.api.postCount())
      .withContext('but nothing is calculated without a propulsion demand')
      .toBe(0);

    harness.dispose();
  }));
});
