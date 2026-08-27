using VoyageEnergyAdvisor.Core.CommonModels;

namespace VoyageEnergyAdvisor.Core.Services.RouteService
{
    using System.Xml.Serialization;
    using VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels;

    public static class RouteXmlSerializer
    {
        public static CommonModels.Route? DeserializeRoute(string filename)
        {
            var xmlContent = File.ReadAllText(filename);
            return DeserializeRouteFromString(xmlContent);
        }

        public static CommonModels.Route? DeserializeRouteFromString(string xmlContent)
        {
            if (xmlContent.Contains("http://www.cirm.org/RTZ/1/1"))
            {
                var routeV11 = Deserialize<RouteV11>(xmlContent, "http://www.cirm.org/RTZ/1/1");
                return ConvertToCommonRoute(routeV11);
            }
            else if (xmlContent.Contains("http://www.cirm.org/RTZ/1/0"))
            {
                var routeV10 = Deserialize<Route>(xmlContent, "http://www.cirm.org/RTZ/1/0");
                return ConvertToCommonRoute(routeV10);
            }
            throw new InvalidOperationException("Unsupported RTZ schema version.");
        }

        private static T Deserialize<T>(string xmlContent, string xmlNamespace)
        {
            var serializer = new XmlSerializer(typeof(T), xmlNamespace);
            using var reader = new StringReader(xmlContent);
            return (T)serializer.Deserialize(reader)!;
        }

        private static CommonModels.Route ConvertToCommonRoute(object xmlModel)
        {
            return xmlModel switch
            {
                Route routeV10 => new CommonModels.Route
                {
                    RouteName = routeV10.routeInfo.routeName,
                    Waypoints = routeV10.waypoints.waypoint
                        .Select(w => new GeoCoordinate((double)w.position.lat, (double)w.position.lon))
                        .ToList()
                },
                RouteV11 routeV11 => new CommonModels.Route
                {
                    RouteName = routeV11.RouteInfo.RouteName,
                    Waypoints = routeV11.Waypoints
                        .Select(w => new GeoCoordinate(w.Position.Latitude, w.Position.Longitude))
                        .ToList()
                },
                _ => throw new InvalidOperationException("Unsupported route type.")
            };
        }

    }
}
