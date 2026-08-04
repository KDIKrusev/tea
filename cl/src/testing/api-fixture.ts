/**
 * `ApiFixture` — the network, on the spec's clock.
 *
 * The single most important property of this class: the two GETs the restore cascade depends on
 * can be answered **independently, at a moment the spec chooses**.
 *
 *   GET /api/app-data/initial       engine catalogue + vessel categories (cached client-side)
 *   GET /api/app-data/vessel-config per size/speed, NOT cached
 *
 * Which writer wins the restore race depends on the order in which those two land. A fixture that
 * cannot hold one back while the other arrives cannot reproduce the bug at all.
 *
 * Story C-A · docs/stories/brownfield-client-a-test-harness.md
 */
import { HttpTestingController, TestRequest } from '@angular/common/http/testing';
import { AppInitialData, FullVesselData, VesselCategoryData } from '../app/core/app-data.types';
import { AuxiliaryEngineType, EngineType } from '../app/core/engine-configuration.types';
import { VesselOperationalProfile } from '../app/core/operational-profile.types';
import {
  AllVariantsCalculationResult,
  CalculatorInput,
  VariantResult,
} from '../app/calculations/calculator.types';

/** Base URL the harness configures on `ConfigService`. Empty ⇒ requests are root-relative. */
export const TEST_API_URL = '';

const CATALOGUE_URL = `${TEST_API_URL}/api/app-data/initial`;
const VESSEL_CONFIG_URL = `${TEST_API_URL}/api/app-data/vessel-config`;
const CALCULATE_URL = `${TEST_API_URL}/api/calculator/calculate-all-variants`;

// ─── Fixture data ─────────────────────────────────────────────────────────────
//
// Every number below is chosen so that a failing assertion NAMES THE WRITER on sight. The three
// possible sources of an engine rating must never share a value:
//
//   catalogue first entry   ME id 3 @  9 000 / SG 1 100   AE id 9 @ 2 500   ← loadEngineConfigurations
//   vessel-type default     ME id 2 @ 15 000 / SG 2 000   AE id 7 @ 1 000   ← setEngineConfiguration
//   scenario 03 profile     ME id 1 @ 24 000 / SG 3 250   AE id 8 @ 4 000   ← applyProfileInputValues
//
// The same applies to the first category: `loadCategories` auto-selects `categories[0]`, so that
// entry is deliberately NOT the category any golden scenario uses.

/** `categories[0]` is Bulk Carrier on purpose — the scenarios are all Offshore Support. */
export const FIXTURE_CATEGORIES: VesselCategoryData[] = [
  { name: 'Bulk Carrier', unit: 'dwt', sizeMin: 20000, sizeMax: 200000, speedMin: 10, speedMax: 16 },
  { name: 'Offshore Support', unit: 'dwt', sizeMin: 5000, sizeMax: 20000, speedMin: 8, speedMax: 16 },
  { name: 'Container', unit: 'TEU', sizeMin: 1000, sizeMax: 20000, speedMin: 12, speedMax: 24 },
];

const mainEngine = (
  id: number,
  name: string,
  maxCapacityKW: number,
  shaftGeneratorMaxCapacityKW: number,
  maker: string,
): EngineType => ({
  id,
  name,
  maxCapacityKW,
  shaftGeneratorMaxCapacityKW,
  description: `${name} (spec fixture)`,
  isActive: true,
  sfocData: [], // [JsonIgnore] server-side — the real payload carries none either
  fuelFamily: 'Liquid',
  maker,
  series: null,
  ratedPowerKW: maxCapacityKW,
});

const auxEngine = (id: number, name: string, maxCapacityKW: number, maker: string): AuxiliaryEngineType => ({
  id,
  name,
  maxCapacityKW,
  description: `${name} (spec fixture)`,
  isActive: true,
  sfocData: [],
  fuelFamily: 'Liquid',
  maker,
  series: null,
  ratedPowerKW: maxCapacityKW,
});

/** Array order matters: `loadEngineConfigurations` applies `mainEngines[0]` when unclaimed. */
export const FIXTURE_MAIN_ENGINES: EngineType[] = [
  mainEngine(3, 'Catalogue First ME', 9000, 1100, 'Zulu'),
  mainEngine(1, 'Profile ME', 24000, 3250, 'Alpha'),
  mainEngine(2, 'Vessel Default ME', 15000, 2000, 'Bravo'),
];

export const FIXTURE_AUX_ENGINES: AuxiliaryEngineType[] = [
  auxEngine(9, 'Catalogue First AE', 2500, 'Zulu'),
  auxEngine(8, 'Profile AE', 4000, 'Alpha'),
  auxEngine(7, 'Vessel Default AE', 1000, 'Bravo'),
];

/** The real backend values (appsettings.json → CalculatorSettings.FuelDefaultPrices). */
export const FIXTURE_FUEL_PRICES: Record<string, number> = {
  MGO: 950,
  MDO: 780,
  HFO: 420,
  LNG: 620,
  Ammonia: 1350,
};

export function fixtureCatalogue(overrides: Partial<AppInitialData> = {}): AppInitialData {
  return {
    categories: FIXTURE_CATEGORIES,
    engineTypes: {
      mainEngines: FIXTURE_MAIN_ENGINES,
      auxiliaryEngines: FIXTURE_AUX_ENGINES,
    },
    operationalProfiles: [],
    metadata: {
      version: 'spec',
      loadedAt: '2026-01-01T00:00:00.000Z',
      vesselTypeCount: FIXTURE_CATEGORIES.length,
      mainEngineCount: FIXTURE_MAIN_ENGINES.length,
      auxiliaryEngineCount: FIXTURE_AUX_ENGINES.length,
      operationalProfileCount: 0,
    },
    fuelDefaultPrices: FIXTURE_FUEL_PRICES,
    ...overrides,
  };
}

/** Vessel-type operational profile — every value distinct from what scenario 03 stores. */
export function fixtureOperationalProfile(
  overrides: Partial<VesselOperationalProfile> = {},
): VesselOperationalProfile {
  return {
    vesselTypeName: 'Offshore Support 11,000 dwt',
    sizeCategory: 'Medium',
    transit: { hotelLoadPowerKW: 999, annualHours: 8000, percentageOfYear: 91.3 },
    port: { hotelLoadPowerKW: 111, annualHours: 400, percentageOfYear: 4.6 },
    anchor: { hotelLoadPowerKW: 222, annualHours: 200, percentageOfYear: 2.3 },
    maneuvering: { hotelLoadPowerKW: 333, propulsionPowerKW: 444, annualHours: 160, percentageOfYear: 1.8 },
    dP: null,
    ...overrides,
  };
}

/**
 * The per-size/speed vessel configuration. Carries the vessel-type engine DEFAULTS, which are the
 * values that must lose to a restored profile.
 */
export function fixtureVesselConfig(overrides: Partial<FullVesselData> = {}): FullVesselData {
  return {
    vesselConfig: {
      vesselTypeName: 'Offshore Support 11,000 dwt',
      calmWaterPowerKW: 8888, // scenario 03 stores 12036.15 — the two must never collide
      seaMargin: 20, // scenario 03 stores 0
    },
    operationalProfile: fixtureOperationalProfile(),
    mainEngineData: FIXTURE_MAIN_ENGINES.find(e => e.id === 2),
    auxEngineData: FIXTURE_AUX_ENGINES.find(e => e.id === 7),
    resolution: {
      lowerRefSize: 10000,
      upperRefSize: 12000,
      t: 0.5,
      profileSource: 'Offshore Support 11,000 dwt',
      clamped: false,
    },
    ...overrides,
  };
}

const variant = (): VariantResult => ({
  fuelSavings: 0,
  fuelSavingsPercentage: 0,
  optimizedFOC: 0,
  optimizedCO2: 0,
  co2Reduction: 0,
  co2ReductionPercentage: 0,
  annualCostSavings: 0,
  totalInvestment: 0,
  paybackPeriod: 0,
  roi: 0,
  efficiencyFactor: 0,
  optimizedME: 0,
  optimizedAE: 0,
  optimizedMeCO2: 0,
  optimizedAeCO2: 0,
  mainEngineLoadPercent: 0,
  auxiliaryEngineLoadPercent: 0,
  level1SavingsTonPerYear: 0,
  level2SavingsTonPerYear: 0,
  level3SavingsTonPerYear: 0,
  warnings: [],
});

/** A structurally valid, numerically meaningless result. Specs here assert on REQUESTS. */
export function fixtureCalculationResult(
  overrides: Partial<AllVariantsCalculationResult> = {},
): AllVariantsCalculationResult {
  return {
    powerDemands: {
      totalDemand: 0,
      meInstalled: 0,
      aeInstalled: 0,
      mainEnginePowerKw: 0,
      auxiliaryEnginePowerKw: 0,
      modeBreakdowns: [],
    },
    baselineFOC: 0,
    baselineCO2: 0,
    baselineME: 0,
    baselineAE: 0,
    baselineMeCO2: 0,
    baselineAeCO2: 0,
    warnings: [],
    advanced: variant(),
    pro: variant(),
    premium: variant(),
    ...overrides,
  };
}

// ─── The fixture ──────────────────────────────────────────────────────────────

interface TrackedRequest {
  req: TestRequest;
  answered: boolean;
}

export class ApiFixture {
  private readonly catalogue: TrackedRequest[] = [];
  private readonly vesselConfig: TrackedRequest[] = [];
  private readonly calculations: TrackedRequest[] = [];

  constructor(private readonly httpMock: HttpTestingController) {}

  /**
   * Pull newly-issued requests out of the testing backend into our own queues.
   *
   * `HttpTestingController.match()` REMOVES what it returns from the backend's open list, so this
   * class — not `httpMock.verify()` — owns the outstanding-request check from here on. Every public
   * accessor drains first, so a request issued a microtask ago is always visible.
   */
  private drain(): void {
    for (const req of this.httpMock.match(() => true)) {
      const url = req.request.url;
      if (url === CATALOGUE_URL) {
        this.catalogue.push({ req, answered: false });
      } else if (url === VESSEL_CONFIG_URL) {
        this.vesselConfig.push({ req, answered: false });
      } else if (url === CALCULATE_URL) {
        this.calculations.push({ req, answered: false });
      } else {
        throw new Error(`ApiFixture: unexpected request ${req.request.method} ${url}`);
      }
    }
  }

  private nextUnanswered(queue: TrackedRequest[], label: string): TrackedRequest {
    this.drain();
    const pending = queue.find(t => !t.answered);
    if (!pending) {
      throw new Error(`ApiFixture: no pending ${label} request to answer`);
    }
    return pending;
  }

  // ─── Catalogue: GET /api/app-data/initial ───────────────────────────────────

  /** Answers the engine catalogue + vessel categories. Releases FOUR subscribers at once. */
  answerCatalogue(overrides: Partial<AppInitialData> = {}): void {
    const tracked = this.nextUnanswered(this.catalogue, 'catalogue');
    tracked.answered = true;
    tracked.req.flush(fixtureCatalogue(overrides));
  }

  failCatalogue(status = 500): void {
    const tracked = this.nextUnanswered(this.catalogue, 'catalogue');
    tracked.answered = true;
    tracked.req.flush('catalogue unavailable', { status, statusText: 'Server Error' });
  }

  pendingCatalogue(): boolean {
    this.drain();
    return this.catalogue.some(t => !t.answered);
  }

  catalogueRequestCount(): number {
    this.drain();
    return this.catalogue.length;
  }

  // ─── Vessel config: GET /api/app-data/vessel-config ─────────────────────────

  answerVesselConfig(overrides: Partial<FullVesselData> = {}): void {
    const tracked = this.nextUnanswered(this.vesselConfig, 'vessel-config');
    tracked.answered = true;
    tracked.req.flush(fixtureVesselConfig(overrides));
  }

  /** Answers every queued vessel-config request with the same payload. */
  answerAllVesselConfig(overrides: Partial<FullVesselData> = {}): number {
    this.drain();
    let count = 0;
    while (this.vesselConfig.some(t => !t.answered)) {
      this.answerVesselConfig(overrides);
      count++;
    }
    return count;
  }

  failVesselConfig(status = 500): void {
    const tracked = this.nextUnanswered(this.vesselConfig, 'vessel-config');
    tracked.answered = true;
    tracked.req.flush('vessel config unavailable', { status, statusText: 'Server Error' });
  }

  pendingVesselConfig(): number {
    this.drain();
    return this.vesselConfig.filter(t => !t.answered).length;
  }

  vesselConfigRequestCount(): number {
    this.drain();
    return this.vesselConfig.length;
  }

  /** The query parameters of every vessel-config request, in order. */
  vesselConfigParams(): Array<{ category: string; size: string; speed: string }> {
    this.drain();
    return this.vesselConfig.map(t => ({
      category: t.req.request.params.get('category') ?? '',
      size: t.req.request.params.get('size') ?? '',
      speed: t.req.request.params.get('speed') ?? '',
    }));
  }

  // ─── Calculation: POST /api/calculator/calculate-all-variants ───────────────

  /** Every calculation request body issued so far, in order. The spine of C-B and invariant I2. */
  postedBodies(): CalculatorInput[] {
    this.drain();
    return this.calculations.map(t => t.req.request.body as CalculatorInput);
  }

  postCount(): number {
    this.drain();
    return this.calculations.length;
  }

  lastPostedBody(): CalculatorInput | null {
    const bodies = this.postedBodies();
    return bodies.length > 0 ? bodies[bodies.length - 1] : null;
  }

  /** Answers every queued calculation. A cancelled request (switchMap) is skipped, not flushed. */
  answerAllCalculations(overrides: Partial<AllVariantsCalculationResult> = {}): number {
    this.drain();
    let count = 0;
    for (const tracked of this.calculations) {
      if (tracked.answered) {
        continue;
      }
      tracked.answered = true;
      if (!tracked.req.cancelled) {
        tracked.req.flush(fixtureCalculationResult(overrides));
        count++;
      }
    }
    return count;
  }

  // ─── Verification ───────────────────────────────────────────────────────────

  /** Unanswered requests, by URL. `httpMock.verify()` cannot see them — `drain()` took them. */
  outstanding(): string[] {
    this.drain();
    const out: string[] = [];
    for (const [label, queue] of [
      ['catalogue', this.catalogue],
      ['vessel-config', this.vesselConfig],
      ['calculate-all-variants', this.calculations],
    ] as Array<[string, TrackedRequest[]]>) {
      const n = queue.filter(t => !t.answered && !t.req.cancelled).length;
      if (n > 0) {
        out.push(`${label} × ${n}`);
      }
    }
    return out;
  }

  verifyNoOutstanding(): void {
    const outstanding = this.outstanding();
    if (outstanding.length > 0) {
      throw new Error(`ApiFixture: unanswered requests — ${outstanding.join(', ')}`);
    }
  }
}
