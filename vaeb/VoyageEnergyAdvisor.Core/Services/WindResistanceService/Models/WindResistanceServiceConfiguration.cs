namespace VoyageEnergyAdvisor.Core.Services.WindResistanceService.Models;
public record WindResistanceServiceConfigurationItem(double ApparentWindAngle, double ApparentWindSpeed, double WindResistanceForce);
public record WindResistanceServiceConfiguration
{
    public required IEnumerable<WindResistanceServiceConfigurationItem> WindResistanceItems { get; init; }
}