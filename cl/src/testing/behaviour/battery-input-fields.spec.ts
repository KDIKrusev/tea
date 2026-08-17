import { fakeAsync } from '@angular/core/testing';

import { RestoreHarness } from '../restore-harness';

/**
 * Epic E2's UI rule (D-BI1/D-BI5), added after a coverage audit: the client's model must be
 * visible in the form, not just enforced in the cascade.
 *
 *   Others                 — always available (Transit/Port battery demand)
 *   Mission Heavy-Consumer — DP only
 *   DP Redundancy          — DP only
 *
 * Manual testing confirmed this on scenarios 37 and 38; these specs pin it.
 */
describe('battery input fields follow the mode rules', () => {
  afterEach(() => RestoreHarness.clearStorage());

  function batteryFieldLabels(harness: RestoreHarness): string[] {
    return Array.from(
      harness.section('app-battery-config-section').querySelectorAll('mat-label')
    ).map(el => el.textContent?.replace(/\s+/g, ' ').trim() ?? '');
  }

  /** A settled form with the battery switched on — the fields live behind that toggle. */
  function formWithBattery(dpHours: number): RestoreHarness {
    const harness = RestoreHarness.mountForm();
    harness.api.answerCatalogue();
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();

    harness.formComponent.vesselForm.patchValue({
      dpHours,
      dpHotelPowerKW: dpHours > 0 ? 1500 : null,
      requiredDPPowerKW: dpHours > 0 ? 2500 : null,
      batteryEnabled: true,
      batteryPowerKw: 500,
      batteryCapacityKwh: 2000,
    });
    harness.settle();
    return harness;
  }

  it('offers Others but hides the DP-only fields when DP is not in use', fakeAsync(() => {
    const harness = formWithBattery(0);
    const labels = batteryFieldLabels(harness);

    expect(labels.some(l => l.startsWith('Others')))
      .withContext('Others covers Transit/Port and is always relevant')
      .toBeTrue();
    expect(labels.some(l => l.startsWith('Mission Heavy-Consumer')))
      .withContext('mission operations are a DP affair (D-BI1) — no DP, no field')
      .toBeFalse();
    expect(labels.some(l => l.startsWith('DP Redundancy')))
      .withContext('a DP class reserve without DP mode would mislead')
      .toBeFalse();

    harness.dispose();
  }));

  it('reveals Mission and DP Redundancy once DP mode is in use', fakeAsync(() => {
    const harness = formWithBattery(2000);
    const labels = batteryFieldLabels(harness);

    expect(labels.some(l => l.startsWith('Others'))).toBeTrue();
    expect(labels.some(l => l.startsWith('Mission Heavy-Consumer'))).toBeTrue();
    expect(labels.some(l => l.startsWith('DP Redundancy'))).toBeTrue();

    harness.dispose();
  }));

  it('carries the Others value to the wire and drops the others when DP is off', fakeAsync(() => {
    const harness = formWithBattery(0);
    harness.formComponent.vesselForm.patchValue({ batteryOthersMaxKw: 300 });
    harness.settle();

    const posted = harness.emissions().at(-1)!.input;
    expect(posted.othersConsumerMaxKw).toBe(300);
    expect(posted.missionHeavyConsumerMaxKw)
      .withContext('an untouched DP-only field must not appear on the wire')
      .toBeUndefined();

    harness.dispose();
  }));
});
