namespace VoyageEnergyAdvisor.WebApi.Dtos
{
    public class VoyageEnergyAdvisorResponseDto
    {
        public string CorrelationId { get; set; } = new Guid().ToString(); // Todo: In use?
        public double VoyageDistance { get; set; }
        public List<VoyageEnergyAdvisorVoyageOptionDto> VoyageOptions { get; set; } = new();

        public string ValidationMessage { get; set; } = null!;
        
    }
}
    
