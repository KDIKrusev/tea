namespace VoyageEnergyAdvisor.Core.Services.SailContributionService.Models;

public record SailContributionItem(double ApparentWindAngle, double ApparentWindSpeed, double SailContributionForce);
public record SailActivePowerItem(double ApparentWindAngle, double ApparentWindSpeed, double SailActivePower);

public record SailContributionServiceConfiguration
{
    public required IEnumerable<SailContributionItem> SailContributions { get; init; }
    public required IEnumerable<SailActivePowerItem> SailActivePowers { get; init; }
}