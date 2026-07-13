/**
 * Default values for fields NOT populated from backend
 */
export const DEFAULT_VALUES = {
  ME_COUNT: 2,
  AE_COUNT: 3,
  BATTERY_CAPACITY: 0,
  // Default fuel price = MGO (USD/ton), matching the default fuel (Parameter_data).
  // The per-engine fuel selector (Epic 3) re-prefills this from the chosen Main fuel.
  FUEL_PRICE: 800,
  ANNUAL_HOURS: 4000
} as const;

/**
 * Validation limits for form inputs
 */
export const VALIDATION_LIMITS = {
  SEA_MARGIN: { MIN: 0, MAX: 100 },
  ANNUAL_HOURS: { MIN: 1, MAX: 8760 },
  POWER: { MIN: 0, MIN_POSITIVE: 1 },
  COUNT: { MIN: 1 },
  FUEL_PRICE: { MIN: 1 }
} as const;

/**
 * Debounce times for form updates (in milliseconds)
 */
export const DEBOUNCE_TIMES = {
  FORM_INPUT: 500,
  SEARCH: 300,
  RESIZE: 150
} as const;
