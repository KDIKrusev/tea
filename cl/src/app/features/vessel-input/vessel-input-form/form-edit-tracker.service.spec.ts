import { TestBed } from '@angular/core/testing';
import { FormEditTrackerService } from './form-edit-tracker.service';

/**
 * Proof-of-life spec for the test infrastructure (Story C-A, AC2).
 *
 * `FormEditTrackerService` is the smallest real unit in the client: no HTTP, no zone, no template.
 * If these five pass, the runner, the TypeScript spec build and TestBed all work.
 *
 * These are CHARACTERISATION tests. They record what the service does today — including the
 * `Number()` coercion and the null-baseline shortcut — and take no position on whether that is
 * the right behaviour. Two of the epic's open questions live in here (design §7.7).
 */
describe('FormEditTrackerService', () => {
  let tracker: FormEditTrackerService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    tracker = TestBed.inject(FormEditTrackerService);
  });

  it('reports a field with no recorded baseline as not edited', () => {
    expect(tracker.isFieldEdited('meCapacityPerEngine', 24000)).toBe(false);
  });

  it('keeps the first baseline on setOriginalValue but replaces it on updateOriginalValue', () => {
    tracker.setOriginalValue('meCapacityPerEngine', 15000);
    tracker.setOriginalValue('meCapacityPerEngine', 99999); // ignored — a baseline is set once

    expect(tracker.isFieldEdited('meCapacityPerEngine', 15000)).toBe(false);
    expect(tracker.isFieldEdited('meCapacityPerEngine', 24000)).toBe(true);

    tracker.updateOriginalValue('meCapacityPerEngine', 24000); // re-baselines

    expect(tracker.isFieldEdited('meCapacityPerEngine', 24000)).toBe(false);
  });

  it('compares numerically, so a string from an input element equals its numeric baseline', () => {
    // Material number inputs hand back strings; the "(edited)" badge must not fire on that alone.
    tracker.updateOriginalValue('sgCapacityPerEngine', 3250);

    expect(tracker.isFieldEdited('sgCapacityPerEngine', '3250')).toBe(false);
    expect(tracker.isFieldEdited('sgCapacityPerEngine', '3251')).toBe(true);
  });

  it('treats a null or undefined baseline as "never edited", whatever the current value', () => {
    tracker.updateOriginalValue('hotelLoadVariationKw', null);
    expect(tracker.isFieldEdited('hotelLoadVariationKw', 500)).toBe(false);

    tracker.updateOriginalValue('hotelLoadVariationKw', undefined);
    expect(tracker.isFieldEdited('hotelLoadVariationKw', 500)).toBe(false);
  });

  it('falls back to strict comparison when neither value is numeric', () => {
    tracker.updateOriginalValue('mainFuelType', 'MGO');

    expect(tracker.isFieldEdited('mainFuelType', 'MGO')).toBe(false);
    expect(tracker.isFieldEdited('mainFuelType', 'LNG')).toBe(true);
  });
});
