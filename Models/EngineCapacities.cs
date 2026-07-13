namespace KSailCalc.Api.Models;

/// <summary>
/// Pre-calculated engine capacities (to avoid recalculating for each mode)
/// </summary>
public record EngineCapacities(
    double MainEngineTotalCapacityKw,
    double ShaftGeneratorTotalCapacityKw,
    double AuxEnginesMaxPower
);
