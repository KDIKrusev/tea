using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Models;

/// <summary>
/// Optimal load setpoint for a single generator (SG or AE).
/// </summary>
public class GeneratorSetpoint
{
    public GeneratorType GeneratorType { get; set; }
    public double LoadPercent { get; set; }
    public double Sfoc { get; set; }
    public double PowerKw { get; set; }
    public double CapacityKw { get; set; }
}
