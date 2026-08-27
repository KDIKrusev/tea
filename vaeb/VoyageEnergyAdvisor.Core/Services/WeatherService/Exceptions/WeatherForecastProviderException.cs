namespace VoyageEnergyAdvisor.Core.Services.WeatherService.Exceptions
{
    public class WeatherForecastProviderException : Exception
    {
        public WeatherForecastProviderException()
        {
        }

        public WeatherForecastProviderException(string message)
            : base(message)
        {
        }

        public WeatherForecastProviderException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
