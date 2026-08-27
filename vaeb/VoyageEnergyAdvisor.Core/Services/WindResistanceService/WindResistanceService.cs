namespace VoyageEnergyAdvisor.Core.Services.WindResistanceService;

using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Services.WindResistanceService.Models;

public class WindResistanceService : IWindResistanceService
{
    private WindResistanceServiceConfiguration _config;

    public WindResistanceService(WindResistanceServiceConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public double GetWindResistancePower(double apparentWindSpeed, double apparentWindDirection, double sog)
    {
        // TODO check scaling if necessary.
        return (_config.WindResistanceItems.Select(e => new MatrixCell(e.ApparentWindAngle, e.ApparentWindSpeed, e.WindResistanceForce))
            .GetClosestValue(apparentWindDirection, apparentWindSpeed) * sog);
    }
}