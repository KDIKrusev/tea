using KSailCalc.Api.Models;

namespace KSailCalc.Api.Services.Interfaces;

/// <summary>
/// Service for aggregating and composing application data from multiple sources
/// Handles caching and optimization of data retrieval
/// </summary>
public interface IAppDataAggregationService
{
    /// <summary>
    /// Get complete initial application data with caching
    /// </summary>
    Task<AppInitialData> GetInitialAppDataAsync();

    /// <summary>
    /// Get full vessel data for a category + size + speed, with calm water power
    /// interpolated in two dimensions (speed within curves, size between reference curves).
    /// Returns null when the category is unknown.
    /// </summary>
    Task<FullVesselData?> GetFullVesselDataByCategoryAsync(string category, decimal size, decimal speed);

    /// <summary>
    /// Clear the application data cache
    /// </summary>
    void ClearCache();
}
