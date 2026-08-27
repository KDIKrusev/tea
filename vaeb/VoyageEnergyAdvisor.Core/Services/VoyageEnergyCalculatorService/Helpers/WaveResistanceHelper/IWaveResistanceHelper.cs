namespace VoyageEnergyAdvisor.Core.Services.WeatherService.Helpers.WaveResistanceHelper
{
    public interface IWaveResistanceHelper
    {

        /// <summary>
        /// Method estimating wave resitance in [kW]
        /// </summary>
        /// <param name="speedOverGround">Speed over ground in [m/s]</param>
        /// <param name="waveHeight">Wave height in [m]</param>
        /// <param name="apparentWaveDirection">Wave from direction relative to vessel heading in [deg]</param>
        /// <returns>Wave resistance power in [kW] or null if power could not be calculated.</returns>
        public double? GetWaveResistancePower(double speedOverGround, double waveHeight, double apparentWaveDirection);
    }

}
