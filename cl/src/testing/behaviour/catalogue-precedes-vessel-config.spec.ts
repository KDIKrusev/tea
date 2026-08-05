import { fakeAsync } from '@angular/core/testing';

import { RestoreHarness } from '../restore-harness';
import { SCENARIO_03_NO_BATTERY } from '../scenarios';
import { FIXTURE_MAIN_ENGINES, FIXTURE_AUX_ENGINES } from '../api-fixture';

/**
 * The invariant that made `pendingEngineConfig` dead code (story C-D §4).
 *
 * `EngineConfigSectionComponent` used to carry a "replay the selection that arrived before the
 * catalogue" branch across four methods. It could never fire: both callers of
 * `setEngineConfiguration` / `setEngineTypeReferences` run from a vessel-config response, and that
 * request is only issued once the vessel categories — which come from the *same*
 * `/api/app-data/initial` payload as the engine catalogue — have arrived, then 400 ms behind a
 * debounce.
 *
 * The branch was deleted. This spec is what replaces the argument for why that was safe: if the
 * ordering ever changes, this fails instead of the engine ratings silently reverting.
 */
describe('the engine catalogue always precedes the first vessel-config response', () => {
  afterEach(() => RestoreHarness.clearStorage());

  it('issues no vessel-config request until the catalogue has been answered', fakeAsync(() => {
    const harness = RestoreHarness.mountForm({ draft: SCENARIO_03_NO_BATTERY });

    harness.tick(5000); // far past the 400 ms fetch debounce

    expect(harness.api.pendingCatalogue()).toBe(true);
    expect(harness.api.vesselConfigRequestCount())
      .withContext('a vessel-config request before the catalogue would resurrect pendingEngineConfig')
      .toBe(0);

    harness.api.answerCatalogue();
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();
    harness.dispose();
  }));

  it('has both engine lists populated before any vessel-config response can arrive', fakeAsync(() => {
    const harness = RestoreHarness.mountForm({ draft: SCENARIO_03_NO_BATTERY });

    harness.api.answerCatalogue();

    // The moment the catalogue lands — before the debounce has even elapsed — the section that
    // consumes it is already fully populated. That is what makes a deferred replay impossible.
    const engineSection = harness.formComponent.engineConfigSection;
    expect(engineSection.mainEngineTypes.length).toBe(FIXTURE_MAIN_ENGINES.length);
    expect(engineSection.auxiliaryEngineTypes.length).toBe(FIXTURE_AUX_ENGINES.length);
    expect(harness.api.vesselConfigRequestCount()).toBe(0);

    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    // And the restore still lands on the profile's engines, not the catalogue's first entry.
    expect(harness.field('meCapacityPerEngine')).toBe(24000);
    expect(harness.field('mainEngineTypeId')).toBe(1);

    harness.dispose();
  }));
});
