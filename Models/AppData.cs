namespace KSailCalc.Api.Models
{
    /// <summary>
    /// Complete initial application data returned in a single API call
    /// Optimized to reduce multiple HTTP requests to just one
    /// </summary>
    public class AppInitialData
    {
        public List<VesselCategoryData> Categories { get; set; } = new();
        public EngineTypesData EngineTypes { get; set; } = new();
        public List<VesselOperationalProfile> OperationalProfiles { get; set; } = new();
        public AppDataMetadata Metadata { get; set; } = new();
        /// <summary>
        /// Default fuel prices (USD/ton) per fuel type from CalculatorSettings
        /// Frontend uses this to dynamically update fuel price when user changes fuel type
        /// </summary>
        public Dictionary<string, double> FuelDefaultPrices { get; set; } = new();
    }

    /// <summary>
    /// Vessel category with size/speed bounds for parametric selection (Epic 1)
    /// Computed from VesselType rows at cache-build time
    /// </summary>
    public class VesselCategoryData
    {
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal? SizeMin { get; set; }
        public decimal? SizeMax { get; set; } // null = unbounded
        public decimal SpeedMin { get; set; }
        public decimal SpeedMax { get; set; }
    }

    /// <summary>
    /// All engine types (main and auxiliary)
    /// </summary>
    public class EngineTypesData
    {
        public List<EngineType> MainEngines { get; set; } = new();
        public List<AuxiliaryEngineType> AuxiliaryEngines { get; set; } = new();
    }

    /// <summary>
    /// Metadata about the loaded data
    /// </summary>
    public class AppDataMetadata
    {
        public string Version { get; set; } = string.Empty;
        public DateTime LoadedAt { get; set; }
        public int VesselTypeCount { get; set; }
        public int MainEngineCount { get; set; }
        public int AuxiliaryEngineCount { get; set; }
        public int OperationalProfileCount { get; set; }
    }
}
