namespace VoyageEnergyAdvisor.Core.Services.CalmWaterResistanceService.Models;
public record CalmWaterResistanceServiceConfigurationItem(double SpeedOverGround, double ResistanceForce);

public record CalmWaterResistanceServiceConfiguration
{
    public required IEnumerable<CalmWaterResistanceServiceConfigurationItem> CalmWaterResistanceItems { get; init; }
}
