// Fuel-type constants (Epic 3). Keys MUST match the backend CalculatorSettings
// FuelCo2Factors / FuelDefaultPrices keys exactly (case-sensitive on the client).

export type FuelFamily = 'Liquid' | 'LNG' | 'Ammonia' | 'DualFuel';

export type FuelType = 'MGO' | 'MDO' | 'HFO' | 'LNG' | 'Ammonia';

/** Specific fuels selectable for each engine fuel family. */
export const FUELS_BY_FAMILY: Record<FuelFamily, FuelType[]> = {
  Liquid: ['MGO', 'MDO', 'HFO'],
  LNG: ['LNG'],
  Ammonia: ['Ammonia'],
  /** Dual-fuel engines can switch between LNG and liquid distillates. */
  DualFuel: ['LNG', 'MGO', 'MDO', 'HFO'],
};

/** Default fuel when none is chosen / family is unknown. */
export const DEFAULT_FUEL: FuelType = 'MGO';

// NOTE: there is deliberately NO default-price table here.
//
// There used to be one, described as "mirrors backend CalculatorSettings.FuelDefaultPrices" — and it
// had drifted on every single fuel (MGO 800 vs 950 · MDO 800 vs 780 · HFO 400 vs 420 · LNG 557 vs
// 620 · Ammonia 1100 vs 1350). Two code paths then disagreed: the engine picker prefilled from this
// stale copy and the form immediately overwrote it from the backend, which the user saw as a price
// flickering 800 → 950.
//
// The backend already ships the real prices in AppInitialData.fuelDefaultPrices, so read them from
// AppDataService.getFuelDefaultPrices(). A copy that must be kept in sync by hand will drift again.

/**
 * Fuels compatible with the given engine fuel family.
 * Falls back to the Liquid set when the family is null/unknown (legacy engines).
 */
export function fuelsForFamily(family?: string | null): FuelType[] {
  if (family && family in FUELS_BY_FAMILY) {
    return FUELS_BY_FAMILY[family as FuelFamily];
  }
  return FUELS_BY_FAMILY.Liquid;
}
