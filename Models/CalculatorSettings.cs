namespace KSailCalc.Api.Models;

/// <summary>
/// Configurable business constants for the calculator engine.
/// Bound from appsettings.json section "CalculatorSettings".
/// </summary>
public class CalculatorSettings
{
    /// <summary>kg CO2 per kg fuel (IMO standard for marine diesel). Fallback when no fuel type is selected.</summary>
    public double Co2Factor { get; set; } = 3.206;

    /// <summary>
    /// Per-fuel CO2 emission factor (kg CO2 per kg fuel) per IMO MEPC.364(79) lifecycle scope.
    /// Includes both Well-to-Tank (production/transport) and Tank-to-Wake (combustion) emissions.
    /// Example: LNG = 2.753 (WtT: 0.00266 + TTW: 2.75 stoichiometric combustion).
    /// Keyed by fuel type (e.g. "MGO", "HFO", "LNG", "Ammonia"). Case-insensitive.
    /// </summary>
    public Dictionary<string, double> FuelCo2Factors { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Per-fuel default price (USD/ton). Used by the client to prefill fuel price (Story 3.6). Case-insensitive.
    /// </summary>
    public Dictionary<string, double> FuelDefaultPrices { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>USD to NOK exchange rate for pricing conversion</summary>
    public double UsdToNokRate { get; set; } = 10.0;

    /// <summary>
    /// Load variation (±kW) per vessel type for DRC calculation.
    /// Keys are vessel type names, values are kW variation.
    /// </summary>
    public Dictionary<string, double> VesselVariations { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Bulk Carrier"] = 250,
        ["Container"] = 1500,
        ["LNG"] = 1000
    };

    /// <summary>Default load variation when vessel type is not in the lookup</summary>
    public double DefaultVesselVariationKw { get; set; } = 500;

    /// <summary>
    /// CO2 factor (kg CO2 / kg fuel) for the given fuel type, or the global <see cref="Co2Factor"/>
    /// fallback when the fuel is null/empty/unknown. This fallback is what keeps no-fuel requests
    /// numerically identical to the pre-fuel behaviour.
    /// </summary>
    public double Co2FactorFor(string? fuel)
        => !string.IsNullOrWhiteSpace(fuel) && FuelCo2Factors.TryGetValue(fuel, out var f) ? f : Co2Factor;

    /// <summary>Default price (USD/ton) for the given fuel type, or null when unknown.</summary>
    public double? DefaultPriceFor(string? fuel)
        => !string.IsNullOrWhiteSpace(fuel) && FuelDefaultPrices.TryGetValue(fuel, out var p) ? p : null;
}
