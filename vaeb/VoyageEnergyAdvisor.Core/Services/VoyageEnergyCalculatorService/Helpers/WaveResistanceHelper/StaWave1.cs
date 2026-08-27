namespace VoyageEnergyAdvisor.Core.Services.WeatherService.Helpers.WaveResistanceHelper
{
    public class StaWave1 : IWaveResistanceHelper
    {
        StaWave1Config _config;

        public StaWave1(StaWave1Config config)
        {
            _config = config;
        }

        public double? GetWaveResistancePower(double speedOverGround, double signifficantWaveHeight, double apparentWaveDirection)
        {
            if (signifficantWaveHeight > 2.25 * Math.Sqrt(_config.lengthBetweenPerpendiculars / 100) ||
                Math.Abs(apparentWaveDirection) < 45)
            {
                return null;
            }

            const double seaWaterDensity = 1023.6; // [kg / m3]
            const double constantOfGravity = 9.81; // [m / s^2]
            return 1.0 / 16000 * seaWaterDensity * constantOfGravity * Math.Pow(signifficantWaveHeight, 2) * _config.shipBreadth * Math.Sqrt(_config.shipBreadth / _config.distBowToMaxBreadthWaterline) * speedOverGround;
        }
    }
}
