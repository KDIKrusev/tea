namespace KSailCalc.Api.Models
{
    /// <summary>
    /// Vessel Type model with engine references, speed/power curve, and operational profile
    /// </summary>
    public class VesselType
    {
        public int Id { get; set; }
        public string VesselTypeName { get; set; } = string.Empty;
        public string? SizeCategory { get; set; }
        public string? Category { get; set; }
        public string? Unit { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Engine configuration references
        public VesselEngineConfig? MainEngine { get; set; }
        public VesselAuxEngineConfig? AuxEngine { get; set; }
        
        // Speed/Power curve (array of speed/power points)
        public List<SpeedPowerPoint> SpeedPowerCurve { get; set; } = new();
        
        // Sea margin percentage
        public decimal SeaMarginPercent { get; set; }

        // Size metadata (Epic 1): curve anchor + bucket bounds, in units of [Unit] (dwt or TEU)
        // ReferenceSize = exact size the curve was measured for (null for bucket-only rows, e.g. Container)
        // MinSize/MaxSize = inclusive bucket bounds for profile/engine selection (MaxSize null = unbounded)
        public decimal? ReferenceSize { get; set; }
        public decimal? MinSize { get; set; }
        public decimal? MaxSize { get; set; }
        
        // Embedded operational profile
        public VesselOperationalProfile? OperationalProfile { get; set; }
        
        // Computed/interpolated power for selected speed (set by service after interpolation)
        public decimal? CalmWaterPowerKW { get; set; }
        
        // Alias for frontend compatibility
        public decimal SeaMargin => SeaMarginPercent;
    }

    /// <summary>
    /// Speed/Power point in the vessel's curve
    /// </summary>
    public class SpeedPowerPoint
    {
        public decimal SpeedKnots { get; set; }
        public decimal CalmWaterPowerKW { get; set; }
    }

    /// <summary>
    /// Response model with complete vessel type and engine data
    /// </summary>
    public class VesselTypeWithEnginesResponse
    {
        public VesselType VesselType { get; set; } = new();
        public EngineType? MainEngineData { get; set; }
        public AuxiliaryEngineType? AuxEngineData { get; set; }
    }
}
