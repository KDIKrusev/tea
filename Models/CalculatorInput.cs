namespace KSailCalc.Api.Models;

/// <summary>
/// Input parameters for the iEMS calculator with multi-modal operational support
/// </summary>
public class CalculatorInput
{
    // === LEGACY FIELDS (maintained for backward compatibility) ===
    public double PropulsionPower { get; set; } // kW - average propulsion power needed
    public double HotelLoad { get; set; } // kW - average hotel/mission loads needed
    public double SeaMargin { get; set; } // % - safety margin for propulsion
    
    // Main Engine Configuration
    public double MeCapacityPerEngine { get; set; } // kW - capacity of ONE main engine
    public int MeCount { get; set; } // number of main engines installed
    
    // Shaft Generator Configuration
    public double SgCapacityPerEngine { get; set; } // kW - max capacity of shaft generator per main engine
    
    // Auxiliary Engine Configuration
    public double AeCapacityPerEngine { get; set; } // kW - capacity of ONE aux engine
    public int AeCount { get; set; } // number of aux engines installed
    
    // Engine Type IDs (for SFOC lookup)
    public int MainEngineTypeId { get; set; } // ID of selected main engine type
    public int AuxEngineTypeId { get; set; } // ID of selected auxiliary engine type
    
    // Additional Systems
    /// <summary>
    /// Whether the vessel has a SAIL system physically installed (UI display flag).
    /// Note: Sail calculations are controlled by <see cref="SailEnabled"/> — this property
    /// is only used by the frontend to show/hide sail-related UI elements.
    /// </summary>
    public bool SailInstalled { get; set; }

    /// <summary>
    /// DEPRECATED: legacy stub, never used in any calculation. Kept for wire compatibility.
    /// Use <see cref="Battery"/> instead.
    /// </summary>
    public double BatteryCapacity { get; set; } // kWh - battery capacity (0 if none)

    /// <summary>
    /// Battery configuration (capacity, power budget, relevant modes).
    /// Null or inactive ⇒ all battery logic is bypassed.
    /// </summary>
    public BatteryConfigurationInput? Battery { get; set; }

    /// <summary>
    /// PTI (Power Take-In) capacity per main engine [kW] — the shaft electric motor rating
    /// (physically usually the shaft generator machine in motor mode).
    /// 0 (default) = PTI not modelled: no propulsion assist, no battery PTI feasibility gate
    /// (bus-level battery simplification, identical to pre-PTI behaviour). ADR-5.
    /// </summary>
    public double MaxPtiPerEngineKw { get; set; }

    /// <summary>
    /// DP class redundancy requirement [kW] (Excel Load Demands R5, input O2) — a pure RESERVE
    /// requirement, NOT part of the average demand: the battery covers it 1:1 (100% coverage);
    /// whatever the battery cannot cover becomes genset spinning reserve. Increment F / D4.
    /// </summary>
    public double? DpRedundancyRequirementKw { get; set; }

    /// <summary>
    /// Mission heavy-consumer maximum [kW] (Excel I3 → Load Demands R7): the Mission row's
    /// variation is this FULL value when mission operations exist (G7 = IF(E7&gt;0, I3, 0)) —
    /// the heavy consumer can start at any moment. The average mission load itself is already
    /// part of the Hotel/Mission power input (no double counting). Increment F / D4.
    /// </summary>
    public double? MissionHeavyConsumerMaxKw { get; set; }
    
    // Financial Parameters
    public double FuelPrice { get; set; } // USD/ton

    // Fuel Types (Epic 3) — optional; null falls back to the single Co2Factor (legacy behaviour).
    // ME and AE may burn different fuels (e.g. ME=LNG, AE=MGO).
    public string? MainFuelType { get; set; } // e.g. "MGO","MDO","HFO","LNG","Ammonia"
    public string? AuxFuelType { get; set; }
    
    // === OPERATIONAL MODES ===

    // Transit Mode (uses existing PropulsionPower + SeaMargin)
    public double TransitHours { get; set; }
    public double TransitHotelPowerKW { get; set; }
    
    // DP Mode (optional - for specialized vessels)
    // Note: DP fields use uppercase "DP" prefix for backward compatibility with frontend JSON contract (dpHours, dpHotelPowerKW)
    public bool DpEnabled { get; set; }
    public double? DPHours { get; set; }
    public double? DPHotelPowerKW { get; set; }
    public double? RequiredDPPowerKW { get; set; }
    public string? DPWeatherCondition { get; set; } // "Calm", "Moderate", "Rough"

    // Port Mode (no propulsion - vessel is in port)
    public double PortHotelPowerKW { get; set; }
    public double PortHours { get; set; }

    // Anchor Mode (no propulsion - vessel is at anchor)
    public double AnchorHotelPowerKW { get; set; }
    public double AnchorHours { get; set; }

    // Maneuvering Mode (propulsion + hotel)
    public double ManeuveringPropulsionPowerKW { get; set; }
    public double ManeuveringHotelPowerKW { get; set; }
    public double ManeuveringHours { get; set; }

    // === VESSEL TYPE (for Level 3 DRC variation lookup) ===
    public string? VesselTypeName { get; set; } // e.g. "Bulk Carrier", "Container", "LNG"

    // === WEATHER FIELDS ===
    public double VesselSpeedKnots { get; set; }
    public double? TrueWindSpeed { get; set; } // m/s
    public double? WindAngleRelVessel { get; set; } // degrees (0-360)
    /// <summary>
    /// Whether sail contribution should be included in calculations.
    /// Requires <see cref="TrueWindSpeed"/> and <see cref="WindAngleRelVessel"/> to be set.
    /// </summary>
    public bool SailEnabled { get; set; }

    // === LEVEL 3 DRC OVERRIDE ===
    /// <summary>
    /// Optional user-specified load variation in kW for Hotel/Mission load.
    /// When set, overrides the vessel-type-based variation lookup.
    /// Matches spec values: Bulk Carrier ±250 kW, Container ±1500 kW, etc.
    /// </summary>
    public double? HotelLoadVariationKw { get; set; }

    // === BASELINE SELECTION ===
    /// <summary>
    /// Index into the sorted valid combinations list to use as baseline.
    /// If null, defaults to second-to-last (sorted[^2]).
    /// </summary>
    public int? BaselineIndex { get; set; }

    // === COMPUTED PROPERTIES (single source of truth for common formulas) ===

    /// <summary>Propulsion power with sea margin applied: PropulsionPower * (1 + SeaMargin / 100)</summary>
    public double EffectivePropulsionPower => PropulsionPower * (1 + SeaMargin / 100);

    /// <summary>Total ME capacity across all main engines</summary>
    public double TotalMeCapacity => MeCapacityPerEngine * MeCount;

    /// <summary>Total SG capacity across all shaft generators (one per ME)</summary>
    public double TotalSgCapacity => SgCapacityPerEngine * MeCount;

    /// <summary>Total AE capacity across all auxiliary engines</summary>
    public double TotalAeCapacity => AeCapacityPerEngine * AeCount;

    /// <summary>Total annual operating hours (Transit + DP)</summary>
    public double AnnualHours => TransitHours + (DPHours ?? 0);
}
