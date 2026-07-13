namespace KSailCalc.Api.Services.Helpers;

/// <summary>
/// Shared calculation helpers used across multiple services.
/// Eliminates duplicated formulas for FOC and load percentage calculations.
/// </summary>
internal static class CalculationHelpers
{
    /// <summary>
    /// Fuel oil consumption in tons per hour: Power (kW) × SFOC (g/kWh) / 1,000,000
    /// </summary>
    internal static double FocTonPerHour(double powerKw, double sfoc)
        => powerKw * sfoc / 1_000_000;

    /// <summary>
    /// Load percentage: power / capacity, or 0 if capacity is zero.
    /// </summary>
    internal static double LoadPercent(double power, double capacity)
        => capacity > 0 ? power / capacity : 0;
}
