using System.Text.Json.Serialization;
using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Models;

/// <summary>
/// Battery configuration entered by the user (sketch: "Battery configuration").
/// Null / PowerKw = 0 / no relevant modes ⇒ the battery feature is inactive.
/// </summary>
public class BatteryConfigurationInput
{
    /// <summary>kWh — energy content of the battery.</summary>
    public double CapacityKwh { get; set; }

    /// <summary>
    /// kW — maximum charge/discharge power. This is the allocation budget
    /// (Excel "Max battery peak shaving and reserve capacity", Optimal Setup I2).
    /// </summary>
    public double PowerKw { get; set; }

    /// <summary>Operational modes in which the battery functions apply (sketch: "Relevant Modes").</summary>
    public List<OperationalMode> RelevantModes { get; set; } = new();

    /// <summary>Whether the battery participates in calculations at all.</summary>
    [JsonIgnore]
    public bool IsActive => PowerKw > 0 && RelevantModes.Count > 0;

    /// <summary>Whether the battery applies in the given mode.</summary>
    public bool AppliesTo(OperationalMode mode) => IsActive && RelevantModes.Contains(mode);
}
