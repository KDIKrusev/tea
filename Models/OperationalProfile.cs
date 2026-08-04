using System.Text.Json.Serialization;

namespace KSailCalc.Api.Models;

/// <summary>
/// Operational mode data (Port, Anchor, Maneuvering, Transit, DP)
/// </summary>
public class OperationalModeData
{
    public decimal HotelLoadPowerKW { get; set; }
    public decimal? PropulsionPowerKW { get; set; } // Optional - used for maneuvering mode (thrusters, low-speed propulsion)
    public decimal AnnualHours { get; set; }
    public decimal PercentageOfYear { get; set; }
}

/// <summary>
/// DP mode specific data with weather conditions
/// </summary>
public class DPModeData : OperationalModeData
{
    public decimal RequiredDPPowerKW { get; set; }
    public string WeatherCondition { get; set; } = "Moderate";
    public decimal ThrustDemandFactor { get; set; } = 1.0m;
    public decimal MinAverageThrustPowerKW { get; set; }
}

/// <summary>
/// Complete operational profile for a vessel type
/// </summary>
public class VesselOperationalProfile
{
    public string VesselTypeName { get; set; } = string.Empty;
    public string SizeCategory { get; set; } = string.Empty;
    
    // New V2 structure with "Mode" suffix
    public OperationalModeData? PortMode { get; set; }
    public OperationalModeData? AnchorMode { get; set; }
    public OperationalModeData? ManeuveringMode { get; set; }
    public OperationalModeData? TransitMode { get; set; }
    public DPModeData? DpMode { get; set; }
    
    // Backward compatibility with old structure (without "Mode" suffix)
    public OperationalModeData Port 
    { 
        get => PortMode ?? new(); 
        set => PortMode = value; 
    }
    
    public OperationalModeData Anchor 
    { 
        get => AnchorMode ?? new(); 
        set => AnchorMode = value; 
    }
    
    public OperationalModeData Maneuvering 
    { 
        get => ManeuveringMode ?? new(); 
        set => ManeuveringMode = value; 
    }
    
    public OperationalModeData Transit 
    { 
        get => TransitMode ?? new(); 
        set => TransitMode = value; 
    }
    
    [JsonPropertyName("dP")]
    public DPModeData? DP 
    { 
        get => DpMode; 
        set => DpMode = value; 
    }
}
