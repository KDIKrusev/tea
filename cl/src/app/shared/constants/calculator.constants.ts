// Used for per-engine CO2 breakdown in UI — backend provides total CO2 only, not per-engine split
/**
 * @deprecated Legacy single CO2 factor. Per-fuel factors live server-side (CalculatorSettings)
 * and all CO2 values — including the per-engine ones — arrive computed in the API response.
 * Never multiply a fuel figure by this constant: it silently disagrees with the totals
 * whenever the vessel burns anything other than the legacy default.
 */
export const CO2_EMISSION_FACTOR = 3.206; // kg CO2 per kg fuel (IMO standard)
