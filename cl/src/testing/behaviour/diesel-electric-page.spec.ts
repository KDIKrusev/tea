import { fakeAsync } from '@angular/core/testing';

import { RestoreHarness } from '../restore-harness';
import { SCENARIO_03_NO_BATTERY } from '../scenarios';

/**
 * The page-level half of story DE-C, added after manual testing found it missing.
 *
 * DE-C's specs asserted that the FORM emits a diesel-electric input. They mounted the form, so
 * they never reached `CalculatorPageComponent.onFormChange`, where a pre-existing pre-check
 * required `meCapacityPerEngine > 0` — true for every plant that existed until Epic E1. A
 * diesel-electric vessel sends 0 there, so every calculation was dropped silently: no request,
 * no results, no error. The form was working perfectly and the app looked dead.
 *
 * These specs watch the wire, which is the only place that distinguishes the two.
 */
describe('diesel-electric calculations reach the API', () => {
  afterEach(() => RestoreHarness.clearStorage());

  const DIESEL_ELECTRIC = {
    ...SCENARIO_03_NO_BATTERY,
    input: {
      ...SCENARIO_03_NO_BATTERY.input,
      propulsionPower: 8000,
      seaMargin: 0,
      transitHotelPowerKW: 3000,
      hotelLoad: 3000,
      meCount: 0,
      meCapacityPerEngine: 0,
      sgCapacityPerEngine: 0,
      mainEngineTypeId: 0,
      aeCapacityPerEngine: 4000,
      aeCount: 4,
    },
  };

  function settledPage(): RestoreHarness {
    const harness = RestoreHarness.mountPage();
    harness.api.answerCatalogue();
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();
    return harness;
  }

  it('posts the calculation after loading a diesel-electric profile', fakeAsync(() => {
    const harness = settledPage();
    const before = harness.api.postCount();

    harness.loadProfile(DIESEL_ELECTRIC as never);
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();

    expect(harness.api.postCount())
      .withContext('a diesel-electric plant must reach the backend, not be dropped by the pre-check')
      .toBeGreaterThan(before);

    const posted = harness.api.postedBodies().at(-1)!;
    expect(posted.meCount).toBe(0);
    expect(posted.meCapacityPerEngine).toBe(0);

    harness.api.answerAllCalculations();
    harness.settle();
    harness.dispose();
  }));

  it('keeps posting when a field is edited on a diesel-electric plant', fakeAsync(() => {
    const harness = settledPage();
    harness.loadProfile(DIESEL_ELECTRIC as never);
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    const before = harness.api.postCount();
    harness.formComponent.vesselForm.get('transitHotelPowerKW')!.setValue(3300);
    harness.settle();

    expect(harness.api.postCount())
      .withContext('editing a field must trigger a recalculation like on any other plant')
      .toBeGreaterThan(before);

    harness.api.answerAllCalculations();
    harness.settle();
    harness.dispose();
  }));

  it('still stays silent while a mechanical plant has no engine rating yet', fakeAsync(() => {
    // The guard's original purpose: a half-filled cascade (meCount >= 1, capacity not yet
    // prefilled) must not reach the wire. The diesel-electric exemption must not weaken it.
    const harness = settledPage();
    const before = harness.api.postCount();

    harness.formComponent.vesselForm.patchValue({ meCount: 2, meCapacityPerEngine: 0 });
    harness.settle();

    expect(harness.api.postCount())
      .withContext('an ME-equipped plant with no rating is still not calculable')
      .toBe(before);

    harness.dispose();
  }));
});
