namespace VoyageEnergyAdvisor.Core.Services.CurrentResistanceService.Models
{
    

    public record CurrentResistanceServiceConfigurationItem(
        double SpeedOverGround,
        double RelativeCurrentDirection,
        double RelativeCurrentSpeed,
        double CurrentResistanceForce
    );

    public record CurrentResistanceServiceConfiguration
    {
        public required IEnumerable<CurrentResistanceServiceConfigurationItem> CurrentResistanceItems { get; init; }
    }
}

