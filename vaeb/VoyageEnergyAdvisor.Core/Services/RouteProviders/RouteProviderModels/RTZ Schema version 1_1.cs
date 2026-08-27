namespace VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels
{
    using System.Xml.Serialization;

    [XmlRoot("route", Namespace = "http://www.cirm.org/RTZ/1/1")]
    public class RouteV11
    {
        [XmlElement("routeInfo")]
        public RouteInfoV11 RouteInfo { get; set; } = null!;

        [XmlArray("waypoints")]
        [XmlArrayItem("waypoint", Namespace = "http://www.cirm.org/RTZ/1/1")]
        public List<WaypointV11> Waypoints { get; set; } = null!;
    }

    public class RouteInfoV11
    {
        [XmlAttribute("routeName")]
        public string RouteName { get; set; } = null!;
    }

    public class WaypointV11
    {
        [XmlAttribute("id")]
        public string Id { get; set; } = null!;

        [XmlElement("position", Namespace = "http://www.cirm.org/RTZ/1/1")]
        public Position Position { get; set; } = null!;
    }

    public class Position
    {
        [XmlAttribute("lat")]
        public double Latitude { get; set; }

        [XmlAttribute("lon")]
        public double Longitude { get; set; }
    }

}
