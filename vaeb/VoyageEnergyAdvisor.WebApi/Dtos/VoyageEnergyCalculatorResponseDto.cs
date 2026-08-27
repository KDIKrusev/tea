namespace VoyageEnergyAdvisor.WebApi.Dtos
{
    public class VoyageEnergyAdvisorResponseDto
    {
        public double VoyageDistance { get; set; }

        // Each ETD/ETA slot with both the constant-speed and the constant-power way of sailing it.
        public List<VoyageEnergyAdvisorVoyageOptionSetDto> VoyageOptionSets { get; set; } = new();

        public string ValidationMessage { get; set; } = null!;
    }
}
