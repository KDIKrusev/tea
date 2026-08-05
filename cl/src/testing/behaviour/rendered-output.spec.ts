import { fakeAsync } from '@angular/core/testing';

import { RestoreHarness } from '../restore-harness';
import { SCENARIO_03_NO_BATTERY } from '../scenarios';

/**
 * What the user actually SEES.
 *
 * Every other spec in this suite asserts form state or request bodies. That was deliberate —
 * invariant I2 is about the wire — but it leaves a hole: a value can be correct in the `FormGroup`
 * and stale, or absent, on screen. No amount of wire-level testing can see that.
 *
 * Story C-G converted the last two components to OnPush, which is exactly the change that can open
 * that hole: a parent calling a child's method directly does not mark an OnPush child dirty, so a
 * field the child renders from can change without the child ever being re-checked.
 *
 * These specs read the DOM.
 */
describe('rendered output', () => {
  afterEach(() => RestoreHarness.clearStorage());

  function settledForm(): RestoreHarness {
    const harness = RestoreHarness.mountForm({ draft: SCENARIO_03_NO_BATTERY });
    harness.api.answerCatalogue();
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();
    return harness;
  }

  it('renders the operational modes section once a profile has been applied', fakeAsync(() => {
    const harness = settledForm();
    const modes = harness.section('app-operational-modes-section');

    // The whole section is behind *ngIf="!isLoading && operationalProfile", and
    // `operationalProfile` is set by a direct method call from the parent.
    expect(modes.querySelector('.modes-container'))
      .withContext('the Operational Modes section must be on screen after a vessel loads')
      .not.toBeNull();

    harness.dispose();
  }));

  it('shows the vessel type badge the operational profile carries', fakeAsync(() => {
    const harness = settledForm();
    // Scoped: six sections share the .info-badge class, and an unscoped query matches the
    // vessel-config header instead — a green that means nothing.
    const badge = harness.section('app-operational-modes-section').querySelector('.info-badge');

    expect(badge).withContext('the profile source badge must render').not.toBeNull();
    expect(badge?.textContent?.trim()).toContain('Offshore Support');

    harness.dispose();
  }));

  it('recomputes the mode-share percentages after a new profile is applied', fakeAsync(() => {
    const harness = settledForm();

    const percentagesAfterFirstLoad = harness.modeSharePercentages();
    expect(percentagesAfterFirstLoad.length)
      .withContext('one percentage badge per operational mode')
      .toBeGreaterThan(0);

    // A different vessel type brings different mode hours. The badges are method calls in the
    // template, so they only change if the component is re-checked.
    harness.formComponent.vesselConfigSection.onCategoryChange('Container');
    harness.tick(500);
    harness.api.answerAllVesselConfig({
      vesselConfig: {
        vesselTypeName: 'Container 10,000 TEU',
        calmWaterPowerKW: 31000,
        seaMargin: 18
      },
      operationalProfile: {
        vesselTypeName: 'Container 10,000 TEU',
        sizeCategory: 'Large',
        transit: { hotelLoadPowerKW: 1200, annualHours: 2000, percentageOfYear: 22.8 },
        port: { hotelLoadPowerKW: 800, annualHours: 6000, percentageOfYear: 68.5 },
        anchor: { hotelLoadPowerKW: 400, annualHours: 500, percentageOfYear: 5.7 },
        maneuvering: { hotelLoadPowerKW: 600, propulsionPowerKW: 900, annualHours: 260, percentageOfYear: 3.0 },
        dP: null
      },
      resolution: {
        lowerRefSize: null, upperRefSize: null, t: null,
        profileSource: 'Container 10,000 TEU', clamped: false
      }
    });
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    expect(harness.modeSharePercentages())
      .withContext('the mode-share badges must follow the new vessel, not keep the old numbers')
      .not.toEqual(percentagesAfterFirstLoad);

    harness.dispose();
  }));

  it('renders the selected engines in the engine section header', fakeAsync(() => {
    // Same hazard as the operational modes section: `setEngineConfiguration` is a direct method
    // call from the parent, and the header is built from `getSelectedMainEngineName()` — a method
    // call in an OnPush template. It survived only because patching the capacities happens to fire
    // `valueChanges`, which a subscription turns into a markForCheck. This spec makes that a
    // guarantee instead of a side effect.
    const harness = RestoreHarness.mountForm();
    harness.api.answerCatalogue();
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    const header = harness.section('app-engine-config-section').querySelector('.info-badge');
    expect(header).withContext('the engine summary badge must render').not.toBeNull();

    const text = header?.textContent ?? '';
    expect(text)
      .withContext('a plain startup shows the vessel type\'s default engines')
      .toContain('Vessel Default ME');
    expect(text).toContain('Vessel Default AE');

    harness.dispose();
  }));

  it('renders the profile hours in the inputs and the shares in the badges', fakeAsync(() => {
    const harness = settledForm();

    expect(harness.formComponent.getTotalHours())
      .withContext('scenario 03 runs 5000 transit hours and nothing else')
      .toBe(5000);

    // Hours live in <input value>, which `textContent` does not include — read the control.
    const transitHours = harness.inputFor('transitHours');
    expect(transitHours).withContext('the transit hours field must render').not.toBeNull();
    expect(transitHours?.value).toBe('5000');

    // The share badge is a method call in the template, so it only shows a number if the
    // component was re-checked after the profile landed.
    expect(harness.modeSharePercentages()[0])
      .withContext('transit is the only active mode, so it carries the whole year')
      .toBe('100.0%');

    harness.dispose();
  }));
});
