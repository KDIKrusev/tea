namespace VoyageEnergyAdvisor.Core.Services.SailContributionService;

using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Services.SailContributionService.Models;

public class SailContributionService : ISailContributionService
{
    private SailContributionServiceConfiguration _config;

    public SailContributionService(SailContributionServiceConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public double GetSailContributionPower(double apparentWindSpeed, double apparentWindDirection, double sog)
    {
        return (_config.SailContributions.Select(e => new MatrixCell(e.ApparentWindAngle, e.ApparentWindSpeed, e.SailContributionForce))
                   .GetClosestValue(apparentWindDirection, apparentWindSpeed) * sog) -
               (_config.SailActivePowers.Select(e => new MatrixCell(e.ApparentWindAngle, e.ApparentWindSpeed, e.SailActivePower))
                   .GetClosestValue(apparentWindDirection, apparentWindSpeed));
    }
}