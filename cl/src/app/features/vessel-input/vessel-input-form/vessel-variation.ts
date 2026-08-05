/**
 * Default Level 3 DRC load variation per vessel type, in kW.
 *
 * Mirrors the backend's `CalculatorSettings.VesselVariations` / `DefaultVesselVariationKw`. The
 * client pre-fills the field so the user can see and override the value the backend would have
 * looked up anyway — leaving it empty produces the same number server-side.
 *
 * Matching is by substring, case-insensitive, because the label is a composed parametric name
 * ("Bulk Carrier 75,000 dwt"), not a bare type.
 */
const VESSEL_VARIATION_DEFAULTS: ReadonlyArray<readonly [match: string, variationKw: number]> = [
  ['Bulk Carrier', 250],
  ['Container', 1500],
  ['LNG', 1000]
];

const DEFAULT_VARIATION_KW = 500;

export function defaultVariationForVessel(vesselTypeName: string): number {
  const name = vesselTypeName.toLowerCase();
  for (const [match, variationKw] of VESSEL_VARIATION_DEFAULTS) {
    if (name.includes(match.toLowerCase())) {
      return variationKw;
    }
  }
  return DEFAULT_VARIATION_KW;
}
