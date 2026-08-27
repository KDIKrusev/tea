namespace VoyageEnergyAdvisor.Core.Configuration.RouteConfiguration.Models
{
    using Newtonsoft.Json;

    public class NavBoxRouteProviderConfiguration
    {
        [JsonProperty("ApiUrl")]
        public string? ApiUrl { get; set; }

        [JsonProperty("OemToken")]
        public string? OemToken { get; set; }

        [JsonProperty("NavboxToken")]
        public string? NavboxToken { get; set; }
    }
}
