namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Helpers
{
    
    public static class VoyageEnergyAdvisorConfigurationHelper
    {
        /*public static double DeriveAreaTraverseFromSpeedPower(this VoyageEnergyAdvisorConfiguration config, double nominalSpeed, double cwrWindFraction)
        {
            const double airDensity = 1.225;
            var cwrPower = config.GetPower(nominalSpeed);
            return 1000 * cwrPower * cwrWindFraction
                               / (nominalSpeed * 0.5 * airDensity * Math.Pow(nominalSpeed, 2)
                                  * config.WindCoefficients
                                      .GetClosestCoefficient(-180).GetValueOrDefault());
        }

        public static double DeriveLateralProjectionFromSpeedPower(this VoyageEnergyAdvisorConfiguration config, double nominalSpeed, double cwrCurrentFraction)
        {
            const double waterDensity = 1.03;
            var cwrPower = config.GetPower(nominalSpeed);
            return 1000 * cwrPower * cwrCurrentFraction
                   / (nominalSpeed * 0.5 * waterDensity * Math.Pow(nominalSpeed, 2)
                      * config.CurrentCoefficientsX.GetClosestCoefficient(-180).GetValueOrDefault());
        }*/
    }
}
