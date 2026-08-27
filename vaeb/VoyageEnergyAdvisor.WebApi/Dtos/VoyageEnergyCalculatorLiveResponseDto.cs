namespace VoyageEnergyAdvisor.WebApi.Dtos
{
    public class VoyageEnergyAdvisorLiveResponseDto
    {
        public long Eta { get; set; }
        public double RemainingTimeInSeconds { get; set; } 
        public double CurrentSpeed { get; set; } 
        public List<VoyageEnergyAdvisorVoyageOptionRouteSegmentDto> RemainingRouteSegments { get; set; } = new();
        public CurrentPositionDto? CurrentPosition { get; set; }
    }
}