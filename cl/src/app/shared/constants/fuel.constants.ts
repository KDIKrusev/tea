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

/** Default price (USD/ton) per fuel — mirrors backend CalculatorSettings.FuelDefaultPrices. */
export const FUEL_DEFAULT_PRICES: Record<FuelType, number> = {
  MGO: 800,
  MDO: 800,
  HFO: 400,
  LNG: 557,
  Ammonia: 1100,
};

/** Default price for a fuel, or undefined when unknown. */
export function defaultPriceFor(fuel?: string | null): number | undefined {
  return fuel && fuel in FUEL_DEFAULT_PRICES ? FUEL_DEFAULT_PRICES[fuel as FuelType] : undefined;
}

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
