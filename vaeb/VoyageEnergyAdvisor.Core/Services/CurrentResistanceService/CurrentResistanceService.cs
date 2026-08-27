namespace VoyageEnergyAdvisor.Core.Services.CurrentResistanceService;

using VoyageEnergyAdvisor.Core.Services.CurrentResistanceService.Models;

public class CurrentResistanceService : ICurrentResistanceService
{
    private readonly CurrentResistanceServiceConfiguration _config;

    public CurrentResistanceService(CurrentResistanceServiceConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public double GetCurrentResistancePower(double relativeCurrentSpeed, double relativeCurrentDirection, double speedOverGround)
    {
        // Normalization ranges
        const double maxSpeedOverGround = 10;
        const double maxRelativeCurrentSpeed = 5.0;
        const double maxRelativeCurrentDirection = 360.0;

        // Normalize input values
        double normSpeedOverGround = speedOverGround / maxSpeedOverGround;
        double normRelativeCurrentSpeed = relativeCurrentSpeed / maxRelativeCurrentSpeed;
        double normRelativeCurrentDirection = relativeCurrentDirection / maxRelativeCurrentDirection;

        var closestItem = _config.CurrentResistanceItems
            .OrderBy(item =>
            {
                double normItemSpeedOverGround = item.SpeedOverGround / maxSpeedOverGround;
                double normItemRelativeCurrentSpeed = item.RelativeCurrentSpeed / maxRelativeCurrentSpeed;
                double normItemRelativeCurrentDirection = item.RelativeCurrentDirection / maxRelativeCurrentDirection;

                return Math.Sqrt(
                    Math.Pow(normItemSpeedOverGround - normSpeedOverGround, 2) +
                    Math.Pow(normItemRelativeCurrentSpeed - normRelativeCurrentSpeed, 2) +
                    Math.Pow(normItemRelativeCurrentDirection - normRelativeCurrentDirection, 2)
                );
            })
            .First();

        return closestItem.CurrentResistanceForce * speedOverGround;
    }
}