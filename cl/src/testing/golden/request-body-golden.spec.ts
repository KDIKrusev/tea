import { fakeAsync } from '@angular/core/testing';

import { RestoreHarness } from '../restore-harness';
import { GOLDEN_SCENARIOS } from '../scenarios';

import golden01 from './01-excel-baseline.request.json';
import golden03 from './03-no-battery-reference.request.json';
import golden15 from './15-baseline-user-pick.request.json';

/**
 * INVARIANT I2 — the request body is the client's golden snapshot.
 *
 * The backend's golden tests post scenario JSON straight at the API and never pass through the
 * form, which is exactly why they stayed green while the UI was wrong. This spec closes that gap:
 * for a settled form state, what the client puts on the wire is frozen.
 *
 * From C-C onwards every story must leave these three bodies unchanged. A story that changes one
 * is not a refactoring story — stop and escalate.
 *
 * WHAT IS FROZEN HERE IS TODAY'S BEHAVIOUR, BUGS INCLUDED. In particular
 * `15-baseline-user-pick.request.json` carries NO `baselineIndex`, even though scenario 15 pins
 * index 4 — that is the open finding the red suite fails on. I2 guards against *unintended*
 * change; when C-E fixes the race, these files are re-frozen deliberately and the diff is named in
 * that story.
 *
 * Comparison is on the PARSED body, not the serialised string: key order is not part of the wire
 * contract, and freezing it would turn C-G's mapper extraction into a false alarm.
 */
describe('I2 · frozen calculation request bodies', () => {
  const expected: Record<string, unknown> = {
    '01-excel-baseline': golden01,
    '03-no-battery-reference': golden03,
    '15-baseline-user-pick': golden15,
  };

  afterEach(() => RestoreHarness.clearStorage());

  for (const { key, file } of GOLDEN_SCENARIOS) {
    it(`${key} — settled body matches the frozen snapshot`, fakeAsync(() => {
      const harness = RestoreHarness.mountPage({ draft: file });

      harness.api.answerCatalogue();
      harness.settle();
      harness.api.answerAllVesselConfig();
      harness.settle();
      harness.api.answerAllCalculations();
      harness.settle();

      const bodies = harness.api.postedBodies();
      expect(bodies.length).withContext('the restore must produce a calculation').toBeGreaterThan(0);

      // JSON round-trip: `undefined` properties never reach the wire, so they must not reach the
      // comparison either.
      const onTheWire = JSON.parse(JSON.stringify(bodies[bodies.length - 1]));
      expect(onTheWire).toEqual(expected[key]);

      harness.dispose();
    }));
  }
});
