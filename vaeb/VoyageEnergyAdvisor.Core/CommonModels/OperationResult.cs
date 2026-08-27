namespace VoyageEnergyAdvisor.Core.CommonModels
{
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string>? Errors { get; set; } = null;
    }
}
