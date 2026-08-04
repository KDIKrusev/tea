import { fakeAsync } from '@angular/core/testing';

import { RestoreHarness } from '../restore-harness';
import { SCENARIO_03_NO_BATTERY, withInput } from '../scenarios';

/**
 * RED SUITE · Story C-B, spec 4 — a restored profile keeps its saved fuel price.
 *
 * DELIBERATELY DISABLED (`xit`). Kamen's decision, 2026-08-04: fixing this moves displayed $
 * figures, so it is deferred to design §7.1 and is NOT part of the refactoring epic. The spec is
 * written and left here so the question stays tracked instead of forgotten — a missing spec is a
 * forgotten question.
 *
 * Mechanism:
 *   `applyProfileInputValues` patches the saved price (vessel-input-form.component.ts:646), then
 *   calls `setEngineTypeReferences` a SECOND time at :671. That runs `reconcileMainFuel` →
 *   `prefillPriceFromMainFuel` (engine-config-section.component.ts:474), which overwrites the
 *   price with the catalogue default AND re-baselines the edit tracker — so the 500 ms
 *   `updateFuelPriceFromFuelType` pass keeps the default too, believing it was never edited.
 *
 * Why nobody noticed: all 35 scenario files store a `fuelPrice` exactly equal to their main fuel's
 * backend default (MDO 780, LNG 620, HFO 420, Ammonia 1350), so the overwrite replaces the value
 * with itself. Only a price the user typed and saved is actually lost — hence `withInput` below
 * rather than a scenario file as-is.
 *
 * To run it: change `xdescribe` to `describe`.
 */
xdescribe('RED (deferred §7.1) · a restored profile keeps its own fuel price', () => {
  afterEach(() => RestoreHarness.clearStorage());

  it('restores a user-chosen price that differs from the fuel default', fakeAsync(() => {
    // MDO's backend default is 780. A user who negotiated 640 and saved the scenario must get 640.
    const negotiatedPrice = withInput(SCENARIO_03_NO_BATTERY, { fuelPrice: 640 });
    const harness = RestoreHarness.mountPage({ draft: negotiatedPrice });

    harness.api.answerCatalogue();
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    expect(harness.field('fuelPrice'))
      .withContext('the catalogue default (780) must not replace the saved price')
      .toBe(640);
    expect(harness.api.lastPostedBody()?.fuelPrice).toBe(640);

    harness.dispose();
  }));
});
