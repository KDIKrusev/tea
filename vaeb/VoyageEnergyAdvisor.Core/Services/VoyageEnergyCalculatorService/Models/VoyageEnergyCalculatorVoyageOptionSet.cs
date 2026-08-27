namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models
{
    // Pairs the two ways of sailing the same ETD/ETA slot:
    //  - VariablePowerOption: constant speed, propulsion power varies with the weather (the classic option).
    //  - VariableSpeedOption: constant propulsion power, speed varies with the weather (formerly /optimal).
    // Both share the same route geometry and the same true weather, so they are built from a single
    // weather fetch (see VoyageEnergyAdvisorVoyageOptionsBuilder.PrepareGeometryAndWeather).
    public class VoyageEnergyAdvisorVoyageOptionSet
    {
        // The planned voyage window. The nested options carry their own Eta/DurationInSeconds/AverageSpeed;
        // for the variable-speed option those can describe an earlier arrival than the planned Eta.
        public DateTime Etd { get; set; }
        public DateTime Eta { get; set; }
        public double DurationInSeconds { get; set; }
        public double AverageSpeed { get; set; }

        public bool IsValid { get; set; }

        public VoyageEnergyAdvisorVoyageOption VariablePowerOption { get; set; } = null!;

        // Null when this slot has no feasible constant-power solution (or the slot itself is invalid).
        public VoyageEnergyAdvisorVoyageOption? VariableSpeedOption { get; set; }

        // Populated only when VariableSpeedOption is null and a reason is known; safe to show to the user.
        public string? VariableSpeedUnavailableReason { get; set; }
    }
}
