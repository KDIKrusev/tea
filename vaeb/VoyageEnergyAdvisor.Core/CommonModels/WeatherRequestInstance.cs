namespace VoyageEnergyAdvisor.Core.CommonModels
{
    public class WeatherRequestInstance
    {
        public DateTime Time;
        public GeoCoordinate Location = null!;
        public bool IsLiveMode { get; set; }
    }
}
