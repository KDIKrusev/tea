namespace VoyageEnergyAdvisor.WebApi.Dtos
{
    public class VoyageEnergyAdvisorVoyageOptionSetDto
    {
        // The planned voyage window (unix ms). The nested options carry their own eta/duration/averageSpeed.
        public long Etd { get; set; }
        public long Eta { get; set; }
        public double DurationInSeconds { get; set; }
        public double AverageSpeed { get; set; }

        public bool IsValid { get; set; }

        // Constant speed, power varies with the weather.
        public VoyageEnergyAdvisorVoyageOptionDto VariablePowerOption { get; set; } = null!;

        // Constant propulsion power, speed varies with the weather. Null when this slot has no solution.
        public VoyageEnergyAdvisorVoyageOptionDto? VariableSpeedOption { get; set; }

        public string? VariableSpeedUnavailableReason { get; set; }
    }
}
