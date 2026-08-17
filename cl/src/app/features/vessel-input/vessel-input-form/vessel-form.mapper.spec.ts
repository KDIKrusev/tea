import { buildVesselForm } from './vessel-form.schema';
import { buildCalculatorInput, buildBatteryInput, sameCalculatorInput } from './vessel-form.mapper';
import { profileToFormPatch } from './profile-patch';
import { FormBuilder } from '@angular/forms';
import { SCENARIO_01_EXCEL_BASELINE, SCENARIO_03_NO_BATTERY } from '../../../../testing/scenarios';

/**
 * Direct tests for the pure form↔request functions extracted in story C-G.
 *
 * The point of extracting them is that invariant I2 — what the client puts on the wire — can now
 * be exercised without mounting a component tree, answering two HTTP requests and advancing a
 * virtual clock. `request-body-golden.spec.ts` still guards the whole path end to end; these
 * pin the individual rules that path depends on, so a failure says *which* rule broke.
 *
 * The falsy-vs-nullish asymmetry gets the most attention, because it is the part most likely to be
 * "tidied" into a bug: `0` is falsy, and several of these fields legitimately hold `0`.
 */
describe('vessel-form mapper', () => {
  /** A fully populated form value, built through the real schema so defaults are the real ones. */
  function formValueFrom(patch: Record<string, unknown>): Record<string, unknown> {
    const form = buildVesselForm(new FormBuilder());
    form.patchValue(patch);
    return form.getRawValue() as Record<string, unknown>;
  }

  describe('round trip through a saved profile', () => {
    it('reproduces scenario 03 exactly', () => {
      const profile = SCENARIO_03_NO_BATTERY.input;
      const value = formValueFrom(profileToFormPatch(profile));

      const rebuilt = buildCalculatorInput(value, profile.vesselTypeName ?? '');

      expect(rebuilt.propulsionPower).toBe(12036.15);
      expect(rebuilt.meCapacityPerEngine).toBe(24000);
      expect(rebuilt.sgCapacityPerEngine).toBe(3250);
      expect(rebuilt.aeCapacityPerEngine).toBe(4000);
      expect(rebuilt.mainEngineTypeId).toBe(1);
      expect(rebuilt.auxEngineTypeId).toBe(8);
      expect(rebuilt.seaMargin).toBe(0);
      expect(rebuilt.fuelPrice).toBe(780);
      expect(rebuilt.mainFuelType).toBe('MDO');
      expect(rebuilt.battery).toBeNull();
      expect(rebuilt.sailInstalled).toBe(false);
    });

    it('reproduces scenario 01, battery and all', () => {
      const profile = SCENARIO_01_EXCEL_BASELINE.input;
      const value = formValueFrom(profileToFormPatch(profile));

      const rebuilt = buildCalculatorInput(value, profile.vesselTypeName ?? '');

      expect(rebuilt.battery).toEqual({
        powerKw: 1260,
        capacityKwh: 2000,
        relevantModes: ['Transit']
      });
      expect(rebuilt.transitHours).toBe(5000);
      expect(rebuilt.hotelLoadVariationKw).toBe(500);
    });
  });

  describe('falsy vs nullish — the asymmetry that must not be "tidied"', () => {
    it('drops a zero for fields guarded by truthiness', () => {
      // `x ? Number(x) : undefined` — a zero here means "not set", which is what the backend
      // expects for these optional inputs. Changing the guard to `x != null` would start sending 0
      // and change what is calculated.
      const value = formValueFrom({ transitHours: 0, dpHours: 0, batteryMaxPtiKw: 0 });
      const input = buildCalculatorInput(value, 'X');

      expect(input.transitHours).toBeUndefined();
      expect(input.dpHours).toBeUndefined();
      expect(input.maxPtiPerEngineKw).toBeUndefined();
    });

    it('keeps a zero for fields guarded by nullishness', () => {
      // `x != null ? Number(x) : undefined` — the weather inputs, where 0 m/s and 0° are real
      // values a user can choose.
      const value = formValueFrom({ trueWindSpeed: 0, windAngleRelVessel: 0 });
      const input = buildCalculatorInput(value, 'X');

      expect(input.trueWindSpeed).toBe(0);
      expect(input.windAngleRelVessel).toBe(0);
    });

    it('sends vesselSpeedKnots as 0 rather than undefined when unset', () => {
      const input = buildCalculatorInput(formValueFrom({}), 'X');
      expect(input.vesselSpeedKnots).toBe(0);
    });

    it('derives dpEnabled from positive DP hours only', () => {
      expect(buildCalculatorInput(formValueFrom({ dpHours: 0 }), 'X').dpEnabled).toBe(false);
      expect(buildCalculatorInput(formValueFrom({ dpHours: 2000 }), 'X').dpEnabled).toBe(true);
    });

    it('maps the Others battery demand and drops it when unset (Epic E2)', () => {
      // Same truthiness contract as its sibling batteryMissionMaxKw: absent/0 must NOT appear on
      // the wire, so the frozen request bodies of pre-E2 profiles stay byte-identical.
      expect(buildCalculatorInput(formValueFrom({ batteryOthersMaxKw: 300 }), 'X').othersConsumerMaxKw).toBe(300);
      expect(buildCalculatorInput(formValueFrom({}), 'X').othersConsumerMaxKw).toBeUndefined();
      expect(buildCalculatorInput(formValueFrom({ batteryOthersMaxKw: 0 }), 'X').othersConsumerMaxKw).toBeUndefined();
    });
  });

  describe('battery payload', () => {
    it('is null when the battery is disabled', () => {
      const value = formValueFrom({ batteryEnabled: false, batteryPowerKw: 1260 });
      expect(buildBatteryInput(value)).toBeNull();
    });

    it('is null when enabled with no power — the pre-battery wire contract', () => {
      const value = formValueFrom({ batteryEnabled: true, batteryPowerKw: 0 });
      expect(buildBatteryInput(value)).toBeNull();
    });

    it('collects the relevant modes in a fixed order', () => {
      const value = formValueFrom({
        batteryEnabled: true,
        batteryPowerKw: 500,
        batteryCapacityKwh: 1000,
        batteryModePort: true,
        batteryModeTransit: true,
        batteryModeDp: true
      });
      expect(buildBatteryInput(value)?.relevantModes).toEqual(['Transit', 'DP', 'Port']);
    });
  });

  describe('vesselTypeName', () => {
    it('comes from the caller, not from a control, and an empty label is omitted', () => {
      expect(buildCalculatorInput(formValueFrom({}), 'Offshore Support 11,000 dwt').vesselTypeName)
        .toBe('Offshore Support 11,000 dwt');
      expect(buildCalculatorInput(formValueFrom({}), '').vesselTypeName).toBeUndefined();
    });
  });

  describe('sameCalculatorInput', () => {
    it('ignores properties that never reach the wire', () => {
      const a = buildCalculatorInput(formValueFrom({ seaMargin: 15 }), 'X');
      const b = buildCalculatorInput(formValueFrom({ seaMargin: 15 }), 'X');
      expect(sameCalculatorInput(a, b)).toBe(true);
    });

    it('sees a real difference', () => {
      const a = buildCalculatorInput(formValueFrom({ seaMargin: 15 }), 'X');
      const b = buildCalculatorInput(formValueFrom({ seaMargin: 0 }), 'X');
      expect(sameCalculatorInput(a, b)).toBe(false);
    });
  });

  describe('profileToFormPatch', () => {
    it('clears an optional field the profile does not carry', () => {
      // Patching `undefined` would leave whatever the vessel-type cascade wrote a moment earlier.
      const patch = profileToFormPatch(SCENARIO_03_NO_BATTERY.input);
      expect(patch['dpHours']).toBeNull();
      expect(patch['portHours']).toBeNull();
      expect(patch['batteryPowerKw']).toBeNull();
    });

    it('maps the boolean sail flag onto the Yes/No control', () => {
      expect(profileToFormPatch({ ...SCENARIO_03_NO_BATTERY.input, sailInstalled: true })['sailInstalled'])
        .toBe('Yes');
      expect(profileToFormPatch({ ...SCENARIO_03_NO_BATTERY.input, sailInstalled: false })['sailInstalled'])
        .toBe('No');
    });
  });
});
