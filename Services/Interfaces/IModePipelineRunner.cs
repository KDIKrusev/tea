using KSailCalc.Api.Models;
using KSailCalc.Api.Services.Results;

namespace KSailCalc.Api.Services.Interfaces;

/// <summary>
/// Runs the optimization pipeline for every operational mode the input activates.
/// </summary>
public interface IModePipelineRunner
{
    /// <summary>
    /// Transit first (full L1 → L2 → L3), then every other active mode with Level 1 only (D4/Q5).
    /// The returned order is the order the client reports modes in and is part of the response.
    /// </summary>
    /// <param name="transitPropulsionOverrideKw">
    /// Transit propulsion demand after the sail contribution, or null when sail is not applied.
    /// </param>
    Task<List<ModePipelineResult>> RunAllModesAsync(
        CalculatorInput input, double? transitPropulsionOverrideKw);
}
