namespace VoyageEnergyAdvisor.Core.CommonModels
{
    public record GeoCoordinate
    {
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        
        public GeoCoordinate(){}
        public GeoCoordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
