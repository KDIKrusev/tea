import { fakeAsync } from '@angular/core/testing';

import { RestoreHarness } from './restore-harness';
import { SCENARIO_03_NO_BATTERY } from './scenarios';

/**
 * Smoke spec for the harness (Story C-A, Task 4).
 *
 * Deliberately asserts almost nothing about CORRECTNESS — only that the real component tree mounts,
 * that both HTTP responses can be released on the spec's clock, and that the cascade quiesces
 * without leaking a timer or an unanswered request. Whether the values that end up in the form are
 * the right ones is Story C-B's question, and it is not asked here.
 */
describe('RestoreHarness', () => {
  afterEach(() => RestoreHarness.clearStorage());

  it('mounts the form, releases both responses in order and quiesces', fakeAsync(() => {
    const harness = RestoreHarness.mountForm();

    // The catalogue request is issued during ngOnInit, before anything is answered.
    expect(harness.api.pendingCatalogue()).toBe(true);
    expect(harness.api.pendingVesselConfig()).toBe(0);

    harness.api.answerCatalogue();
    harness.tick(500); // past the 400 ms vessel-config fetch debounce

    expect(harness.api.pendingVesselConfig())
      .withContext('the auto-selected default category must trigger exactly one vessel fetch')
      .toBe(1);

    harness.api.answerVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    expect(harness.api.outstanding()).toEqual([]);
    harness.dispose();
  }));

  it('cannot issue the vessel-config request before the catalogue resolves', fakeAsync(() => {
    // Recorded as a property of TODAY's code, not as a desired one: `selectVessel` and
    // `loadCategories` both await /api/app-data/initial, so the per-vessel fetch is structurally
    // downstream of the catalogue. This is matrix ordering O5 in Story C-B — the case Kamen asked
    // to cover — and it is why C-E must remove the coupling rather than re-time it.
    const harness = RestoreHarness.mountForm();

    harness.tick(5000);

    expect(harness.api.pendingCatalogue()).toBe(true);
    expect(harness.api.vesselConfigRequestCount())
      .withContext('no vessel-config request can exist while the catalogue is unanswered')
      .toBe(0);

    harness.api.answerCatalogue();
    harness.settle();

    expect(harness.api.vesselConfigRequestCount()).toBeGreaterThan(0);
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();
    harness.dispose();
  }));

  it('mounts the page, restores a profile and reaches a settled state', fakeAsync(() => {
    const harness = RestoreHarness.mountPage();

    harness.api.answerCatalogue();
    harness.loadProfile(SCENARIO_03_NO_BATTERY);
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    // The restore reached the vessel the scenario names — the cascade ran end to end.
    expect(harness.api.vesselConfigParams().pop()).toEqual({
      category: 'Offshore Support',
      size: '11000',
      speed: '12.5',
    });
    expect(harness.api.postCount())
      .withContext('a restore must produce at least one calculation')
      .toBeGreaterThan(0);
    expect(harness.emissions().length).toBeGreaterThan(0);

    harness.dispose();
  }));

  it('seeds an auto-draft and restores it on mount (the hard-refresh path)', fakeAsync(() => {
    const harness = RestoreHarness.mountForm({ draft: SCENARIO_03_NO_BATTERY });

    harness.api.answerCatalogue();
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    expect(harness.api.vesselConfigParams().pop()?.category).toBe('Offshore Support');
    harness.dispose();
  }));
});
