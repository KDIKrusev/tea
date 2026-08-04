import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import {
  ApiFixture,
  FIXTURE_CATEGORIES,
  TEST_API_URL,
  fixtureCatalogue,
  fixtureVesselConfig,
} from './api-fixture';
import { GOLDEN_SCENARIOS, SCENARIO_03_NO_BATTERY } from './scenarios';
import { AppDataService } from '../app/core/app-data.service';
import { CalculatorService } from '../app/calculations/calculator.service';
import { ConfigService } from '../app/core/config.service';

/**
 * Self-test for the harness's defining property (Story C-A, AC5 + AC6).
 *
 * This spec deliberately drives `AppDataService` directly rather than through the component tree:
 * the question here is whether the FIXTURE can hold one response back while the other lands, not
 * what the components do with them. C-B answers the second question.
 */
describe('ApiFixture', () => {
  let api: ApiFixture;
  let appData: AppDataService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    TestBed.inject(ConfigService).setConfig({ apiUrl: TEST_API_URL });
    appData = TestBed.inject(AppDataService);
    api = new ApiFixture(TestBed.inject(HttpTestingController));
  });

  describe('independent responses (AC5)', () => {
    it('answers the catalogue while the vessel-config request stays pending', fakeAsync(() => {
      let catalogueArrived = false;
      let vesselConfigArrived = false;

      appData.loadInitialData().subscribe(() => (catalogueArrived = true));
      appData
        .getFullVesselDataByCategory('Offshore Support', 11000, 12.5)
        .subscribe(() => (vesselConfigArrived = true));

      expect(api.pendingCatalogue()).toBe(true);
      expect(api.pendingVesselConfig()).toBe(1);

      api.answerCatalogue();
      tick(1000);

      expect(catalogueArrived).toBe(true);
      expect(vesselConfigArrived)
        .withContext('vessel-config must NOT resolve just because the catalogue did')
        .toBe(false);
      expect(api.pendingVesselConfig()).toBe(1);

      api.answerVesselConfig();
      tick(0);

      expect(vesselConfigArrived).toBe(true);
      api.verifyNoOutstanding();
    }));

    it('answers the vessel config while the catalogue stays pending', fakeAsync(() => {
      let catalogueArrived = false;
      let vesselConfigArrived = false;

      appData.loadInitialData().subscribe(() => (catalogueArrived = true));
      appData
        .getFullVesselDataByCategory('Offshore Support', 11000, 12.5)
        .subscribe(() => (vesselConfigArrived = true));

      api.answerVesselConfig();
      tick(1000);

      expect(vesselConfigArrived).toBe(true);
      expect(catalogueArrived)
        .withContext('the catalogue must be holdable past the vessel-config response')
        .toBe(false);
      expect(api.pendingCatalogue()).toBe(true);

      api.answerCatalogue();
      tick(0);

      expect(catalogueArrived).toBe(true);
      api.verifyNoOutstanding();
    }));

    it('reports an unanswered request instead of letting the spec pass silently', fakeAsync(() => {
      appData.loadInitialData().subscribe();
      expect(() => api.verifyNoOutstanding()).toThrowError(/catalogue × 1/);
      api.answerCatalogue();
      tick(0);
      api.verifyNoOutstanding();
    }));
  });

  describe('conflicting defaults (AC6)', () => {
    it('keeps the three possible sources of an engine rating pairwise distinct', () => {
      const catalogue = fixtureCatalogue();
      const vesselConfig = fixtureVesselConfig();
      const profile = SCENARIO_03_NO_BATTERY.input;

      // The writer that fires when the catalogue lands and nothing has claimed the field yet.
      expect(catalogue.engineTypes.mainEngines[0].id).toBe(3);
      expect(catalogue.engineTypes.mainEngines[0].maxCapacityKW).toBe(9000);
      expect(catalogue.engineTypes.auxiliaryEngines[0].id).toBe(9);
      expect(catalogue.engineTypes.auxiliaryEngines[0].maxCapacityKW).toBe(2500);

      // The writer that fires when the vessel type's defaults are applied.
      expect(vesselConfig.mainEngineData?.id).toBe(2);
      expect(vesselConfig.mainEngineData?.maxCapacityKW).toBe(15000);
      expect(vesselConfig.auxEngineData?.id).toBe(7);
      expect(vesselConfig.auxEngineData?.maxCapacityKW).toBe(1000);

      // The writer that must win: the restored profile.
      expect(profile.mainEngineTypeId).toBe(1);
      expect(profile.meCapacityPerEngine).toBe(24000);
      expect(profile.auxEngineTypeId).toBe(8);
      expect(profile.aeCapacityPerEngine).toBe(4000);

      expect(new Set([9000, 15000, 24000]).size).withContext('ME ratings collide').toBe(3);
      expect(new Set([2500, 1000, 4000]).size).withContext('AE ratings collide').toBe(3);
      expect(new Set([3, 2, 1]).size).withContext('ME ids collide').toBe(3);
      expect(new Set([9, 7, 8]).size).withContext('AE ids collide').toBe(3);
    });

    it('keeps the auto-selected first category different from every golden scenario', () => {
      // `loadCategories` picks categories[0] on its own. If that were the scenario's category the
      // default-selection writer would be invisible.
      const autoSelected = FIXTURE_CATEGORIES[0].name;
      expect(autoSelected).toBe('Bulk Carrier');

      for (const { key, file } of GOLDEN_SCENARIOS) {
        expect(file.vesselCategory).withContext(`${key} must not use the auto-selected category`).not.toBe(autoSelected);
      }
    });

    it('keeps the vessel-type curve values away from the scenario values', () => {
      const vesselConfig = fixtureVesselConfig();
      expect(vesselConfig.vesselConfig['calmWaterPowerKW']).toBe(8888);
      expect(vesselConfig.vesselConfig['seaMargin']).toBe(20);
      expect(SCENARIO_03_NO_BATTERY.input.propulsionPower).toBe(12036.15);
      expect(SCENARIO_03_NO_BATTERY.input.seaMargin).toBe(0);
    });

    it('serves the real backend fuel default prices', () => {
      // appsettings.json → CalculatorSettings.FuelDefaultPrices. Every one of the 35 scenario files
      // stores exactly its main fuel's default, so a spec probing a USER-CHOSEN price has to
      // synthesise one (see withInput() and design §7.1).
      expect(fixtureCatalogue().fuelDefaultPrices).toEqual({
        MGO: 950,
        MDO: 780,
        HFO: 420,
        LNG: 620,
        Ammonia: 1350,
      });
      expect(SCENARIO_03_NO_BATTERY.input.mainFuelType).toBe('MDO');
      expect(SCENARIO_03_NO_BATTERY.input.fuelPrice).toBe(780);
    });
  });

  describe('calculation capture', () => {
    it('records every posted body in order and skips cancelled requests', fakeAsync(() => {
      const calculator = TestBed.inject(CalculatorService);
      const first = { ...SCENARIO_03_NO_BATTERY.input, baselineIndex: 4 };
      const second = { ...SCENARIO_03_NO_BATTERY.input, baselineIndex: 2 };

      calculator.calculateAllVariants(first).subscribe();
      calculator.calculateAllVariants(second).subscribe();

      expect(api.postCount()).toBe(2);
      expect(api.postedBodies().map(b => b.baselineIndex)).toEqual([4, 2]);
      expect(api.lastPostedBody()?.baselineIndex).toBe(2);

      expect(api.answerAllCalculations()).toBe(2);
      tick(0);
      api.verifyNoOutstanding();
    }));
  });

  describe('fixture source (AC7)', () => {
    it('loads the scenario files from docs/qa/manual-test-scenarios', () => {
      expect(SCENARIO_03_NO_BATTERY.name).toBe('03 No-battery reference world (avg + full swing)');
      expect(SCENARIO_03_NO_BATTERY.vesselCategory).toBe('Offshore Support');
      expect(SCENARIO_03_NO_BATTERY.vesselSize).toBe(11000);
      expect(SCENARIO_03_NO_BATTERY.vesselSpeed).toBe(12.5);
      expect(GOLDEN_SCENARIOS.length).toBe(3);
    });
  });
});
