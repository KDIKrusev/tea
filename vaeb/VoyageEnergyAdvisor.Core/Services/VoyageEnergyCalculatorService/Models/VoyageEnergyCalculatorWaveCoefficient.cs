using Newtonsoft.Json;

namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models
{
    public class VoyageEnergyAdvisorWaveCoefficient
    {
        [JsonProperty("WaveFrequencyDivRelativeWaveDirection")]
        public double WaveFrequencyDivRelativeWaveDirection { get; set; }

        [JsonProperty("VesselSpeed")]
        public double VesselSpeed { get; set; }

        [JsonProperty("Coefficient")]
        public double Coefficient { get; set; }
    }
}
