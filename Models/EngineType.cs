using System.Text.Json.Serialization;
using KSailCalc.Api.Models.Enums;

namespace KSailCalc.Api.Models
{
    /// <summary>
    /// Unified Engine Type model (Main + Auxiliary combined)
    /// Maps to EngineType table in hybrid schema
    /// </summary>
    public class EngineType
    {
        public int Id { get; set; }
        public EngineCategory EngineCategory { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal MaxCapacityKW { get; set; }
        public decimal? ShaftGeneratorMaxCapacityKW { get; set; } // NULL for Auxiliary
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        // Catalogue + fuel metadata (Epic 3). Serialized so the client can group/filter/label.
        public string? FuelFamily { get; set; }   // 'Liquid' | 'LNG' | 'Ammonia' | 'DualFuel'
        public string? Maker { get; set; }         // e.g. 'Wärtsilä', 'MAN', 'Bergen'
        public string? Series { get; set; }        // e.g. 'C25', '31DF'
        public decimal? RatedPowerKW { get; set; } // nominal PMCR (advisory; user enters manual rated power)
        public int? Rpm { get; set; }
        public string? NoxTier { get; set; }

        [JsonIgnore]
        public List<SfocDataPoint> SfocData { get; set; } = new();
    }

    /// <summary>
    /// Auxiliary Engine Type model (backward compatibility)
    /// Maps to same EngineType table where EngineCategory = 'Auxiliary'
    /// </summary>
    public class AuxiliaryEngineType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal MaxCapacityKW { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        // Catalogue + fuel metadata (Epic 3) — mirrors EngineType so aux engines filter/label too.
        public string? FuelFamily { get; set; }
        public string? Maker { get; set; }
        public string? Series { get; set; }
        public decimal? RatedPowerKW { get; set; }

        [JsonIgnore]
        public List<SfocDataPoint> SfocData { get; set; } = new();
    }

    /// <summary>
    /// SFOC data point for engine load vs fuel consumption
    /// </summary>
    public class SfocDataPoint
    {
        public double Load { get; set; } // Load percentage (0.0 to 1.0)
        public double Sfoc { get; set; } // Specific fuel consumption in g/kWh
    }

    /// <summary>
    /// Engine configuration within a vessel type
    /// </summary>
    public class VesselEngineConfig
    {
        public int EngineTypeId { get; set; }
        public int NumberOfEngines { get; set; }
        public decimal ShaftGeneratorMaxCapacityKW { get; set; }
    }

    /// <summary>
    /// Auxiliary engine configuration within a vessel type
    /// </summary>
    public class VesselAuxEngineConfig
    {
        public int EngineTypeId { get; set; }
        public int NumberOfEngines { get; set; }
    }
}
