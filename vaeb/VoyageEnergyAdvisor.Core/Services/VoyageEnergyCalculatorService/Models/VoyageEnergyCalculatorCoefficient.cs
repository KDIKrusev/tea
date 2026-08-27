using Newtonsoft.Json;

namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models
{
    public class VoyageEnergyAdvisorCoefficient
    {
        [JsonProperty("Direction")]
        public double Direction { get; set; }

        [JsonProperty("Coefficient")]
        public double Coefficient { get; set; }
    }
}
