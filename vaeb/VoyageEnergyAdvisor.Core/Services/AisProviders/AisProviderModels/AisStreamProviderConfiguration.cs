namespace VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels
{
    public class AisStreamProviderConfiguration
    {
        public string? ApiKey { get; set; }
        public string[] FilterShipMMSI { get; set; } = null!;
        public string[] FilterMessageTypes { get; set; } = null!;
        public double[][] GlobalBoundingBox { get; set; } = null!;
        public int ReconnectDelayMs { get; set; }
        public int MaxReconnectAttempts { get; set; } 
    }
}
