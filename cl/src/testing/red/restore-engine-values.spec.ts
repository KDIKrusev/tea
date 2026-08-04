import { fakeAsync } from '@angular/core/testing';

import { RestoreHarness } from '../restore-harness';
import { SCENARIO_03_NO_BATTERY } from '../scenarios';

/**
 * RED SUITE · Story C-B, spec 1 — a restored profile's engine values must survive the cascade.
 *
 * Expected to FAIL on today's code, in at least one arrival order. See
 * docs/stories/brownfield-client-b-red-suite.md.
 *
 * This is the spec the whole session exists for. Unlike the other two, its mechanism was NOT
 * established by reading: several writers can plausibly win and which one does depends on when the
 * two HTTP responses land. Naming it from the source is the mistake already made twice — so the
 * spec runs an arrival MATRIX and reports, per cell, which value ended up in the form.
 *
 * The fixture numbers are pairwise distinct by construction (ApiFixture, AC6), so the observed
 * value names the writer with no instrumentation:
 *
 *    9000 / 1100 / 2500  → loadEngineConfigurations   applied catalogue entry [0]
 *   15000 / 2000 / 1000  → setEngineConfiguration     applied the vessel-type defaults
 *   24000 / 3250 / 4000  → applyProfileInputValues    applied the restored profile  ← required
 */

const PROFILE = SCENARIO_03_NO_BATTERY.input;

const TRACKED_FIELDS = [
  'meCapacityPerEngine',
  'sgCapacityPerEngine',
  'aeCapacityPerEngine',
  'mainEngineTypeId',
  'auxEngineTypeId',
  'propulsionPower',
] as const;

type Tracked = (typeof TRACKED_FIELDS)[number];

const EXPECTED: Record<Tracked, number> = {
  meCapacityPerEngine: 24000,
  sgCapacityPerEngine: 3250,
  aeCapacityPerEngine: 4000,
  mainEngineTypeId: 1,
  auxEngineTypeId: 8,
  propulsionPower: 12036.15,
};

/** Which writer a value points at. Used only to make a failure self-explanatory. */
function attribute(field: Tracked, value: unknown): string {
  const catalogueFirst: Partial<Record<Tracked, number>> = {
    meCapacityPerEngine: 9000,
    sgCapacityPerEngine: 1100,
    aeCapacityPerEngine: 2500,
    mainEngineTypeId: 3,
    auxEngineTypeId: 9,
  };
  const vesselDefault: Partial<Record<Tracked, number>> = {
    meCapacityPerEngine: 15000,
    sgCapacityPerEngine: 2000,
    aeCapacityPerEngine: 1000,
    mainEngineTypeId: 2,
    auxEngineTypeId: 7,
    propulsionPower: 8888,
  };
  const n = Number(value);
  if (n === EXPECTED[field]) {
    return 'profile';
  }
  if (catalogueFirst[field] === n) {
    return 'catalogue[0] · loadEngineConfigurations';
  }
  if (vesselDefault[field] === n) {
    return 'vessel default · setEngineConfiguration/applyVesselData';
  }
  return 'unknown';
}

class Recorder {
  readonly steps: Array<{ label: string; values: Record<string, unknown> }> = [];

  constructor(private readonly harness: RestoreHarness) {}

  capture(label: string): void {
    const form = this.harness.formValue();
    const values: Record<string, unknown> = {};
    for (const field of TRACKED_FIELDS) {
      values[field] = form[field];
    }
    this.steps.push({ label, values });
  }

  /** A per-cell table for the story's Debug Log. */
  report(): string {
    const header = ['step', ...TRACKED_FIELDS].join(' | ');
    const rows = this.steps.map(s =>
      [s.label, ...TRACKED_FIELDS.map(f => String(s.values[f]))].join(' | '),
    );
    const final = this.steps[this.steps.length - 1]?.values ?? {};
    const blame = TRACKED_FIELDS.filter(f => Number(final[f]) !== EXPECTED[f]).map(
      f => `${f} = ${final[f]} (expected ${EXPECTED[f]}) ← written by ${attribute(f, final[f])}`,
    );
    return (
      `\n${header}\n${rows.join('\n')}\n` +
      (blame.length > 0 ? `\nLAST WRITER PER LOSING FIELD:\n  ${blame.join('\n  ')}` : '\nAll fields hold the profile value.')
    );
  }
}

/** The vessel-config payload for the auto-selected default category during a warm-up. */
const BULK_CARRIER_CONFIG = {
  vesselTypeName: 'Bulk Carrier 110,000 dwt',
  calmWaterPowerKW: 7777,
  seaMargin: 15,
};

function expectProfileValues(recorder: Recorder, harness: RestoreHarness): void {
  const form = harness.formValue();
  const report = recorder.report();
  for (const field of TRACKED_FIELDS) {
    expect(Number(form[field]))
      .withContext(`${field} — ${report}`)
      .toBe(EXPECTED[field]);
  }
}

describe('RED · a restored profile wins the engine cascade', () => {
  afterEach(() => RestoreHarness.clearStorage());

  it('fixture sanity — the profile, the vessel default and the catalogue never collide', () => {
    expect(PROFILE.meCapacityPerEngine).toBe(24000);
    expect(PROFILE.sgCapacityPerEngine).toBe(3250);
    expect(PROFILE.aeCapacityPerEngine).toBe(4000);
    expect(PROFILE.mainEngineTypeId).toBe(1);
    expect(PROFILE.auxEngineTypeId).toBe(8);
    expect(PROFILE.propulsionPower).toBe(12036.15);
  });

  // ─── E1 · the "Load" button, catalogue already warm ────────────────────────

  describe('E1 · Load Profile click (catalogue warm)', () => {
    /** Brings the app to the steady state a user is in before clicking Load. */
    function warmUp(harness: RestoreHarness): void {
      harness.api.answerCatalogue();
      harness.settle();
      harness.api.answerAllVesselConfig({ vesselConfig: BULK_CARRIER_CONFIG });
      harness.settle();
      harness.api.answerAllCalculations();
      harness.settle();
    }

    function runCell(vesselConfigDelayMs: number): void {
      const harness = RestoreHarness.mountPage();
      const recorder = new Recorder(harness);

      warmUp(harness);
      recorder.capture('after warm-up');

      harness.loadProfile(SCENARIO_03_NO_BATTERY);
      recorder.capture('loadProfile() called');

      harness.tick(vesselConfigDelayMs);
      recorder.capture(`+${vesselConfigDelayMs} ms, vessel-config still pending`);

      harness.api.answerAllVesselConfig();
      harness.settle();
      recorder.capture('vessel-config answered, settled');

      harness.api.answerAllCalculations();
      harness.settle();
      recorder.capture('calculations answered, settled');

      expectProfileValues(recorder, harness);
      harness.dispose();
    }

    it('E1-a · vessel-config after 500 ms', fakeAsync(() => runCell(500)));
    it('E1-b · vessel-config after 2000 ms', fakeAsync(() => runCell(2000)));
    it('E1-c · vessel-config after 3500 ms (crosses the 3000 ms restore watchdog)', fakeAsync(() =>
      runCell(3500)));
  });

  // ─── E2 · hard refresh with an auto-draft, catalogue cold ──────────────────

  describe('E2 · hard refresh restoring an auto-draft (catalogue cold)', () => {
    function runCell(catalogueDelayMs: number, vesselConfigDelayMs: number): void {
      const harness = RestoreHarness.mountPage({ draft: SCENARIO_03_NO_BATTERY });
      const recorder = new Recorder(harness);

      recorder.capture('mounted, nothing answered');

      harness.tick(catalogueDelayMs);
      recorder.capture(`+${catalogueDelayMs} ms, catalogue still pending`);

      harness.api.answerCatalogue();
      recorder.capture('catalogue answered');

      harness.tick(vesselConfigDelayMs);
      recorder.capture(`+${vesselConfigDelayMs} ms after the catalogue`);

      harness.api.answerAllVesselConfig();
      harness.settle();
      recorder.capture('vessel-config answered, settled');

      harness.api.answerAllCalculations();
      harness.settle();
      recorder.capture('calculations answered, settled');

      expectProfileValues(recorder, harness);
      harness.dispose();
    }

    it('E2-a · catalogue 50 ms, vessel-config +500 ms', fakeAsync(() => runCell(50, 500)));
    it('E2-b · catalogue 50 ms, vessel-config +2000 ms', fakeAsync(() => runCell(50, 2000)));
    it('E2-c · catalogue 2000 ms, vessel-config +1500 ms (crosses the watchdog)', fakeAsync(() =>
      runCell(2000, 1500)));
    it('E2-d · catalogue 3500 ms (itself past the watchdog), vessel-config +500 ms', fakeAsync(() =>
      runCell(3500, 500)));
  });

  // ─── E3 · Load clicked while the initial cascade is still in flight ────────
  //
  // The dimension E1 and E2 both miss. E1 warms up to a quiescent state first; E2's two fetch
  // triggers collapse inside the 400 ms debounce. Neither produces a vessel-config response that
  // is processed AFTER the profile has already been applied — and that is the shape of what Kamen
  // reports: the file's values appear, then vanish.
  //
  // A user does exactly this: the page starts loading, they click their saved scenario straight
  // away. The default-category fetch is already on the wire; `selectVessel` queues a second one.
  // The first answer applies the profile (200 ms path), `endRestore()` runs, and then the SECOND
  // answer arrives with `restoreInFlight` already false — so `onVesselEngineConfigSelected`
  // computes `applyEngineDefaults = true` and calls `setEngineConfiguration`, overwriting the
  // profile's ratings with the vessel type's.

  describe('E3 · Load clicked while the first vessel-config request is still in flight', () => {
    function runCell(clickAfterMs: number): void {
      const harness = RestoreHarness.mountPage();
      const recorder = new Recorder(harness);

      harness.api.answerCatalogue();
      harness.tick(clickAfterMs);
      recorder.capture(`+${clickAfterMs} ms — default-category fetch on the wire`);

      expect(harness.api.pendingVesselConfig())
        .withContext('cell precondition: a vessel-config request must already be in flight')
        .toBe(1);

      harness.loadProfile(SCENARIO_03_NO_BATTERY);
      recorder.capture('loadProfile() called with a request still pending');

      // The in-flight default-category answer lands first — before the restore's own fetch is
      // even issued (it is still inside its 400 ms debounce).
      harness.api.answerVesselConfig({
        vesselConfig: BULK_CARRIER_CONFIG,
        resolution: {
          lowerRefSize: null,
          upperRefSize: null,
          t: null,
          profileSource: 'Bulk Carrier 110,000 dwt',
          clamped: false,
        },
      });
      harness.settle();
      recorder.capture('first (stale) vessel-config answered, settled');

      harness.api.answerAllVesselConfig();
      harness.settle();
      recorder.capture('second (real) vessel-config answered, settled');

      harness.api.answerAllCalculations();
      harness.settle();
      recorder.capture('calculations answered, settled');

      expectProfileValues(recorder, harness);
      harness.dispose();
    }

    it('E3-a · Load clicked 450 ms after the catalogue', fakeAsync(() => runCell(450)));
    it('E3-b · Load clicked 1000 ms after the catalogue', fakeAsync(() => runCell(1000)));
  });

  // ─── E4 · the vessel response carries no operational profile ───────────────
  //
  // `applyVesselData` only re-emits `operationalProfileLoaded` when the response has a profile AND
  // the bucket changed. Without it `onOperationalProfileLoaded` never runs, so the profile is
  // applied by the 800 ms fallback in `onVesselEngineConfigSelected` instead of the 200 ms path —
  // a different writer, a different order.

  describe('E4 · vessel-config without an operational profile (the 800 ms fallback path)', () => {
    it('E4-a · applies the profile through the fallback', fakeAsync(() => {
      const harness = RestoreHarness.mountPage({ draft: SCENARIO_03_NO_BATTERY });
      const recorder = new Recorder(harness);

      harness.api.answerCatalogue();
      harness.settle();
      recorder.capture('catalogue answered');

      harness.api.answerAllVesselConfig({
        operationalProfile: null as never, // the shape the client already guards against
      });
      harness.settle();
      recorder.capture('vessel-config without operationalProfile, settled');

      harness.api.answerAllCalculations();
      harness.settle();
      recorder.capture('calculations answered, settled');

      expectProfileValues(recorder, harness);
      harness.dispose();
    }));
  });

  // ─── O4 / O5 · orderings that today's code cannot produce ──────────────────

  it('O4/O5 · records that the vessel-config response cannot precede the catalogue', fakeAsync(() => {
    // Kamen asked specifically for "the catalogue returns AFTER the profile has been applied".
    // On today's code that ordering — and its weaker form, vessel-config before catalogue — is
    // structurally unreachable: `loadCategories` and `selectVessel` both await
    // /api/app-data/initial, so the per-vessel fetch is downstream of it and nothing can invert
    // them. Recorded as a characterisation, NOT a passing guard. C-E removes the coupling, and at
    // that point this expectation flips and the ordering joins the live matrix above.
    const harness = RestoreHarness.mountPage({ draft: SCENARIO_03_NO_BATTERY });

    harness.tick(5000);

    expect(harness.api.pendingCatalogue()).toBe(true);
    expect(harness.api.vesselConfigRequestCount())
      .withContext('if this is ever non-zero, orderings O4/O5 have become reachable — add them above')
      .toBe(0);

    harness.api.answerCatalogue();
    harness.settle();
    harness.api.answerAllVesselConfig();
    harness.settle();
    harness.api.answerAllCalculations();
    harness.settle();
    harness.dispose();
  }));
});
