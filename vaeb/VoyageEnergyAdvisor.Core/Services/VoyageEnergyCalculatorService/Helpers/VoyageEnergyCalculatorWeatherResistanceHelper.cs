using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models;

namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Helpers
{
    public static class VoyageEnergyAdvisorWeatherResistanceHelper
    {
        /*public static double GetWindPowerExcludingCwr(double speedOverGround, double relativeWindSpeed,
            double relativeWindDirectionDeg, VoyageEnergyAdvisorConfiguration config)
        {
            double totalWindPower = GetWindPower(speedOverGround, relativeWindSpeed, relativeWindDirectionDeg, config);
            double calmWaterWindPower = GetWindPower(speedOverGround, speedOverGround, 180, config);
            return totalWindPower - calmWaterWindPower;
        }

        public static double GetCurrentPowerExcludingCwr(double speedOverGround, double relativeCurrentSpeed,
            double relativeCurrentDirection, VoyageEnergyAdvisorConfiguration config)
        {
            double totalCurrentPower = GetCurrentPower(relativeCurrentSpeed, relativeCurrentDirection, config);
            double calmWaterCurrentPower = GetCurrentPower(speedOverGround, 180, config);
            return totalCurrentPower - calmWaterCurrentPower;
        }

        public static double GetWindPower(double speedOverGround, double relativeWindSpeed,
            double relativeWindDirection, VoyageEnergyAdvisorConfiguration config)
        {
            const double airDensity = 1.225;
            var windForce = 0.5 * airDensity * Math.Pow(relativeWindSpeed, 2) * config.AreaTraverse *
                            config.WindCoefficients.GetClosestCoefficient(relativeWindDirection)
                                .GetValueOrDefault();
            return (double)(speedOverGround * windForce / 1000); // [kw] wind power

        }

        public static double GetCurrentPower(double relativeCurrentSpeed, double relativeCurrentDirection, VoyageEnergyAdvisorConfiguration config)
        {
            const double waterDensity = 1.03;
            var speedXDir = Math.Abs(relativeCurrentSpeed * Math.Cos(relativeCurrentDirection.DegToRad()));
            var Cx = config.CurrentCoefficientsX.GetClosestCoefficient(relativeCurrentDirection).GetValueOrDefault();
            var currentForceX = 0.5 * waterDensity * config.LateralProjection * Math.Pow(relativeCurrentSpeed, 2) * Cx;
            return (double)(speedXDir * currentForceX / 1000); // [kw] current power

        }*/
    }
}
