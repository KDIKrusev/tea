using KSailCalc.Api.Models;
using KSailCalc.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KSailCalc.Api.Controllers
{
    /// <summary>
    /// Thin controller for providing application data
    /// Delegates all logic to AppDataAggregationService
    /// </summary>
    [ApiController]
    [Route("api/app-data")]
    public class AppDataController : ControllerBase
    {
        private readonly IAppDataAggregationService _appDataService;
        private readonly ILogger<AppDataController> _logger;

        public AppDataController(
            IAppDataAggregationService appDataService,
            ILogger<AppDataController> logger)
        {
            _appDataService = appDataService;
            _logger = logger;
        }

        /// <summary>
        /// Get ALL application initial data in a single optimized API call
        /// Returns: vessel types, engine types, operational profiles, and metadata
        /// REPLACES: /names, /all-configurations, /speeds, /operational-profile endpoints
        /// Cached for 24 hours to minimize database load
        /// </summary>
        [HttpGet("initial")]
        [ProducesResponseType(typeof(AppInitialData), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInitialAppData()
        {
            try
            {
                var appData = await _appDataService.GetInitialAppDataAsync();
                return Ok(appData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading initial app data");
                return StatusCode(500, new { error = "Failed to load application data" });
            }
        }

        /// <summary>
        /// Force refresh of cached app data
        /// Useful for admin operations after data updates
        /// </summary>
        [HttpPost("refresh-cache")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult RefreshCache()
        {
            _appDataService.ClearCache();
            return Ok(new { message = "Cache cleared successfully" });
        }

        /// <summary>
        /// Get vessel configuration for a category + size + speed (parametric, Epic 1).
        /// Calm water power is interpolated over speed AND size between reference curves.
        /// Example: ?category=Bulk Carrier&amp;size=75000&amp;speed=12.7
        /// </summary>
        [HttpGet("vessel-config")]
        [ProducesResponseType(typeof(FullVesselData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVesselConfiguration(
            [FromQuery] string? category,
            [FromQuery] decimal? size,
            [FromQuery] decimal speed)
        {
            if (string.IsNullOrWhiteSpace(category) || !size.HasValue)
            {
                return BadRequest(new { error = "Parameters 'category' and 'size' are required." });
            }
            if (size.Value <= 0)
            {
                return BadRequest(new { error = "Parameter 'size' must be a positive number." });
            }
            if (speed <= 0)
            {
                return BadRequest(new { error = "Parameter 'speed' must be a positive number of knots." });
            }

            try
            {
                var result = await _appDataService
                    .GetFullVesselDataByCategoryAsync(category, size.Value, speed);

                if (result == null)
                {
                    return NotFound($"No vessel category found for '{category}'");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting vessel configuration ({Category} {Size}) @ {Speed} knots",
                    category, size, speed);
                return StatusCode(500, new { error = "Failed to load vessel configuration" });
            }
        }
    }
}
