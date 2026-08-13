/**
 * Default values for fields NOT populated from backend
 */
export const DEFAULT_VALUES = {
  ME_COUNT: 2,
  AE_COUNT: 3,
  BATTERY_CAPACITY: 0,
  // Default fuel price = MGO (USD/ton), matching the default fuel (Parameter_data).
  // The per-engine fuel selector (Epic 3) re-prefills this from the chosen Main fuel.
  FUEL_PRICE: 800
} as const;

/**
 * Validation limits for form inputs
 */
export const VALIDATION_LIMITS = {
  SEA_MARGIN: { MIN: 0, MAX: 100 },
  POWER: { MIN: 0, MIN_POSITIVE: 1 },
  // Split limits (Epic E1): 0 main engines is a legal diesel-electric plant;
  // a vessel with 0 auxiliary engines is not a plant at all.
  ME_COUNT: { MIN: 0 },
  AE_COUNT: { MIN: 1 },
  FUEL_PRICE: { MIN: 1 }
} as const;

/**
 * Debounce time for form updates (in milliseconds).
 *
 * One of only two timing constants in the client — the other is the 400 ms vessel-config fetch
 * debounce in VesselConfigSectionComponent. It is a deliberate UX choice (recalculate when the
 * user stops typing), and nothing may depend on its value for sequencing.
 */
export const DEBOUNCE_TIMES = {
  FORM_INPUT: 500
} as const;
