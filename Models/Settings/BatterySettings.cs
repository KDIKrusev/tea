using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Models.Settings;

/// <summary>
/// Configurable battery-model constants. Bound from appsettings.json section "BatterySettings".
/// Code defaults equal the reference Excel workbook values
/// (docs/battery-feature-analysis/06-architecture-design.md §3.2).
/// </summary>
public class BatterySettings
{
    /// <summary>Transmission loss factor for PTI propulsion power drawn from the aux side (Excel I4).</summary>
    public double PtiLossFactor { get; set; } = 0.05;

    /// <summary>ηBatteryCharge (MachCalcTool Parameter_data).</summary>
    public double ChargeEfficiency { get; set; } = 0.97;

    /// <summary>ηBatteryDischarge (MachCalcTool Parameter_data).</summary>
    public double DischargeEfficiency { get; set; } = 0.97;

    /// <summary>ηElectricMotor — used for PTI/PTO electric machinery sizing (MachCalcTool).</summary>
    public double ElectricMotorEfficiency { get; set; } = 0.965;

    /// <summary>
    /// Allocation rows in priority order (Excel "Load Demands" rows 5→9).
    /// Order in this list IS the allocation priority.
    /// Deliberately empty by default: ConfigurationBinder APPENDS to a pre-populated list,
    /// which would duplicate every row when the appsettings section is present. Consumers must
    /// fall back to <see cref="CreateDefaultLoadPriorities"/> when this list is empty.
    /// </summary>
    public List<BatteryLoadPriority> LoadPriorities { get; set; } = new();

    /// <summary>Fresh copy of the Excel-default priority rows (workbook "Load Demands" 5→9).</summary>
    public static List<BatteryLoadPriority> CreateDefaultLoadPriorities() => new()
    {
        new() { Load = BatteryLoadType.DpReserve,  Function = BatteryFunction.Reserve,     CoverageFactor = 1.00, VariationFactor = 0.00 },
        new() { Load = BatteryLoadType.DpDemand,   Function = BatteryFunction.PeakShaving, CoverageFactor = 0.50, VariationFactor = 0.00 },
        new() { Load = BatteryLoadType.Mission,    Function = BatteryFunction.PeakShaving, CoverageFactor = 0.50, VariationFactor = 0.00 },
        new() { Load = BatteryLoadType.Propulsion, Function = BatteryFunction.PeakShaving, CoverageFactor = 0.35, VariationFactor = 0.05 },
        new() { Load = BatteryLoadType.Hotel,      Function = BatteryFunction.PeakShaving, CoverageFactor = 0.05, VariationFactor = 0.02 }
    };
}

/// <summary>One priority row of the battery allocation configuration.</summary>
public class BatteryLoadPriority
{
    public BatteryLoadType Load { get; set; }

    public BatteryFunction Function { get; set; }

    /// <summary>D — covered fraction of the battery usage for this load.</summary>
    public double CoverageFactor { get; set; }

    /// <summary>Relative variation of the average load (e.g. 0.05 = ±5%). Reserve rows use the full requirement.</summary>
    public double VariationFactor { get; set; }
}
