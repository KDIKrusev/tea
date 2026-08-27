namespace VoyageEnergyAdvisor.WebApi.Dtos;

public class WeatherDataDto
{
    public WeatherDataDto(double? windSpeed, double? windDirection, double? waveHeight, double? wavePeakPeriod, double? waveDirection, double? currentSpeed, double? currentDirection)
    {
        WindSpeed = windSpeed;
        WindDirection = windDirection;
        WaveHeight = waveHeight;
        WavePeakPeriod = wavePeakPeriod;
        WaveDirection = waveDirection;
        CurrentSpeed = currentSpeed;
        CurrentDirection = currentDirection;
    }

    public WeatherDataDto()
    {

    }

    // Wind
    public double? WindSpeed { get; set; }         
    public double? WindDirection { get; set; }       

    // Wave
    public double? WaveHeight { get; set; }        
    public double? WavePeakPeriod { get; set; }     
    public double? WaveDirection { get; set; }
    
    // Current
    public double? CurrentSpeed { get; set; }     
    public double? CurrentDirection { get; set; }    
}
