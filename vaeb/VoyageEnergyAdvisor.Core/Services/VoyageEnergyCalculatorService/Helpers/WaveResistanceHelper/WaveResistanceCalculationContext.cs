namespace VoyageEnergyAdvisor.Core.Services.WeatherService.Helpers.WaveResistanceHelper
{
    public class WaveResistanceCalculationContext
    {
        private IWaveResistanceHelper _strategy;

        public WaveResistanceCalculationContext(IWaveResistanceHelper strategy)
        {
            _strategy = strategy;
        }

        public double? PerformCalculation(double speedOverGround, double waveHeight, double relativeWaveToDirection)
        {
            return _strategy.GetWaveResistancePower(speedOverGround, waveHeight, relativeWaveToDirection);
        }
    }
}
