namespace VoyageEnergyAdvisor.Core.Configuration.RouteConfiguration.Models
{
    using Newtonsoft.Json;

    public class NavTorTokenResponse
    {
        [JsonProperty("access_token")]
        public string? Token { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresInSeconds { get; set; }
    }
}
