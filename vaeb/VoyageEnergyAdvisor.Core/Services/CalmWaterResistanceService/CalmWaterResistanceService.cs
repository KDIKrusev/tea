namespace VoyageEnergyAdvisor.Core.Services.CalmWaterResistanceService;

using VoyageEnergyAdvisor.Core.Services.CalmWaterResistanceService.Models;

public class CalmWaterResistanceService : ICalmWaterResistanceService
{
    private CalmWaterResistanceServiceConfiguration _config;
    public CalmWaterResistanceService(CalmWaterResistanceServiceConfiguration config)
    {

        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public double GetCalmWaterResistancePower(double speedOverGround)
    {
        var closestCalmWaterResistanceItem = _config.CalmWaterResistanceItems
            .OrderBy(sc => Math.Abs(sc.SpeedOverGround - speedOverGround))
            .FirstOrDefault();

        if (closestCalmWaterResistanceItem == null)
        {
            throw new InvalidOperationException("No calm water resistance config found for the given speed over ground.");
        }

        return closestCalmWaterResistanceItem.ResistanceForce * speedOverGround;
    }
}