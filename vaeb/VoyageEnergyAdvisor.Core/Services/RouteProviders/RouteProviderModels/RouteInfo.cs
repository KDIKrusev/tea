using Newtonsoft.Json;

namespace VoyageEnergyAdvisor.Core.Services.RouteProviders
{
    public class RouteInfo
    {
        public required string Id { get; set; }
        public string? Title { get; set; }
        [JsonProperty("modification_time")]
        public string? ModificationTime { get; set; }
        public string? Author { get; set; }
        public string? Status { get; set; }
    }
}
