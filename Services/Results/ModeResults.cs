using KSailCalc.Api.Models;
using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Services.Results;

/// <summary>
/// What one operational mode produced: its three optimization levels, the hours it runs, and the
/// battery outcome when a battery applied to it.
///
/// Public because it is the contract between <see cref="Interfaces.IModePipelineRunner"/> and the
/// orchestrator; the builders in this namespace stay internal and simply consume it.
/// </summary>
public sealed record ModePipelineResult(
    OperationalMode Mode, Level1Result L1, Level2Result L2, Level3Result L3, double Hours,
    BatteryModeOutcome? Battery);

/// <summary>What the battery produced in one mode: its allocation and the R3a benefit.</summary>
public sealed record BatteryModeOutcome(BatteryModeAllocation Allocation, double BenefitTonPerYear);
