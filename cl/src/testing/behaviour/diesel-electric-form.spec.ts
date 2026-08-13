import { fakeAsync } from '@angular/core/testing';

import { RestoreHarness } from '../restore-harness';
import { SCENARIO_03_NO_BATTERY } from '../scenarios';

/**
 * Story DE-C: at meCount == 0 the shaft-bound controls (ME capacity, ME type, SG, PTI) park —
 * disabled and cleared — so the `required` validators stop applying (disabled controls drop out
 * of form validity, and validity gates every emission), while `getRawValue()` still carries
 * explicit zeros to the wire, which backend story DE-A accepts.
 *
 * The fixture catalogue applies `mainEngines[0]` ("Catalogue First ME", 9 000 kW, id 3) on plain
 * startup — the same engine the un-park transition re-prefills.
 */
describe('diesel-electric form gating', () => {
  afterEach(() => RestoreHarness.clearStorage());

  const SHAFT_BOUND = ['meCapacityPerEngine', 'mainEngineTypeId', 'sgCapacityPerEngine', 'batteryMaxPtiKw'];

  function settledForm(): RestoreHarness {
    const harness = RestoreHarness.mountForm();
    harness.api.answerCatalogue();
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();
    return harness;
  }

  it('parks the shaft-bound controls at meCount 0 and sends zeros on the wire', fakeAsync(() => {
    const harness = settledForm();
    const form = harness.formComponent.vesselForm;

    form.get('meCount')!.setValue(0);
    harness.settle();

    for (const name of SHAFT_BOUND) {
      expect(form.get(name)!.disabled)
        .withContext(`${name} must be disabled on a diesel-electric plant`)
        .toBeTrue();
      expect(form.get(name)!.value)
        .withContext(`${name} must be cleared, not keep a stale shaft value`)
        .toBeNull();
    }

    // Disabled controls drop out of validity — the required ME capacity/type must not block.
    expect(form.valid)
      .withContext('the form must stay valid so the calculation emission still fires')
      .toBeTrue();

    const last = harness.emissions().at(-1)!;
    expect(last.input.meCount).toBe(0);
    expect(last.input.meCapacityPerEngine).withContext('null → explicit 0 on the wire').toBe(0);
    expect(last.input.mainEngineTypeId).toBe(0);
    expect(last.input.sgCapacityPerEngine).toBe(0);
    expect(last.input.maxPtiPerEngineKw)
      .withContext('cleared PTI is dropped from the payload (falsy-as-absent contract)')
      .toBeUndefined();

    harness.dispose();
  }));

  it('un-parking restores the catalogue prefill instead of stale nulls', fakeAsync(() => {
    const harness = settledForm();
    const form = harness.formComponent.vesselForm;

    form.get('meCount')!.setValue(0);
    harness.settle();
    form.get('meCount')!.setValue(2);
    harness.settle();

    for (const name of SHAFT_BOUND) {
      expect(form.get(name)!.enabled)
        .withContext(`${name} must be usable again on a conventional plant`)
        .toBeTrue();
    }
    expect(form.get('meCapacityPerEngine')!.value)
      .withContext('capacity re-prefills from the catalogue engine')
      .toBe(9000);
    expect(form.get('mainEngineTypeId')!.value).toBe(3);

    const last = harness.emissions().at(-1)!;
    expect(last.input.meCount).toBe(2);
    expect(last.input.meCapacityPerEngine).toBe(9000);

    harness.dispose();
  }));

  it('a restored diesel-electric draft arrives parked with meCount 0 on the wire', fakeAsync(() => {
    // Restore is the path findings 4/5/6 came from — the gating must survive it, not just
    // a live user edit.
    const deDraft = {
      ...SCENARIO_03_NO_BATTERY,
      input: {
        ...SCENARIO_03_NO_BATTERY.input,
        meCount: 0,
        meCapacityPerEngine: 0,
        sgCapacityPerEngine: 0,
        mainEngineTypeId: 0
      }
    };
    const harness = RestoreHarness.mountForm({ draft: deDraft });
    harness.api.answerCatalogue();
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    const form = harness.formComponent.vesselForm;
    expect(form.get('meCount')!.value).toBe(0);
    for (const name of SHAFT_BOUND) {
      expect(form.get(name)!.disabled)
        .withContext(`${name} must arrive parked after a diesel-electric restore`)
        .toBeTrue();
    }

    const last = harness.emissions().at(-1)!;
    expect(last.input.meCount).toBe(0);
    expect(Number.isFinite(last.input.meCapacityPerEngine))
      .withContext('the wire must carry a number, never NaN')
      .toBeTrue();

    harness.dispose();
  }));

  it('an ordinary meCount edit (2 → 3) does not touch the shaft controls', fakeAsync(() => {
    // The re-prefill runs ONLY on the parked → unparked transition; otherwise a plain count edit
    // would stomp a user-edited capacity — the exact bug family findings 4/5/6 came from.
    const harness = settledForm();
    const form = harness.formComponent.vesselForm;

    form.get('meCapacityPerEngine')!.setValue(12345); // a user edit
    harness.settle();
    form.get('meCount')!.setValue(3);
    harness.settle();

    expect(form.get('meCapacityPerEngine')!.value)
      .withContext('a non-zero count change must never re-prefill')
      .toBe(12345);

    harness.dispose();
  }));
});
