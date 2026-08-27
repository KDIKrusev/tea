namespace VoyageEnergyAdvisor.Core.CommonModels
{
    public class WeatherData
    {
        public WeatherData(double? windSpeed, double? windFromDirection, double? waveHeight, double? wavePeakPeriod, double? waveFromDirection, double? currentSpeed, double? currentFromDirection)
        {
            WindSpeed = windSpeed;
            WindFromDirection = windFromDirection;
            WaveHeight = waveHeight;
            WavePeakPeriod = wavePeakPeriod;
            WaveFromDirection = waveFromDirection;
            CurrentSpeed = currentSpeed;
            CurrentFromDirection = currentFromDirection;
        }

        public WeatherData()
        {

        }

        // Wind
        public double? WindSpeed { get; set; }         
        public double? WindFromDirection { get; set; }       

        // Wave
        public double? WaveHeight { get; set; }        
        public double? WavePeakPeriod { get; set; }     
        public double? WaveFromDirection { get; set; }
        
        // Current
        public double? CurrentSpeed { get; set; }     
        public double? CurrentFromDirection { get; set; }    
    }
}
