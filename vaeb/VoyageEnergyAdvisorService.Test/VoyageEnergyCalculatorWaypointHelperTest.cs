using Route = VoyageEnergyAdvisor.Core.CommonModels.Route;
using GeoCoordinate = VoyageEnergyAdvisor.Core.CommonModels.GeoCoordinate;

namespace VoyageEnergyAdvisorService.Test
{
    using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Helpers;
    using Xunit;

    public class VoyageEnergyAdvisorGeoCoordinateHelperTest
    {
        [Fact]
        public void CanGetDistance()
        {
            var route = new Route()
            {
                Waypoints = new List<GeoCoordinate>()
                {
                    new GeoCoordinate(0,0),
                    new GeoCoordinate(0,10),
                    new GeoCoordinate(10, 0)
                }
            };
            Assert.Equal(1448.5873851917086, route.GetVoyageDistance().MetersToNauticalMiles());
        }

        [Fact]
        public void CanConvertMetersToNauticalMiles()
        {
            Assert.Equal((1000.0 / 1852), ((double)1000).MetersToNauticalMiles());
        }

        [Fact]
        public void CanConvertNauticalMilesToMeters()
        {
            Assert.Equal(1852, ((double)1).NauticalMilesToMeters());
        }

        [Fact]
        public void CanSplitIntoSegments()
        {
            // Around the globe..
            var route = new Route()
            {
                Waypoints = new List<GeoCoordinate>()
                {
                    new GeoCoordinate(45, 150),
                    new GeoCoordinate(0, 0),
                    new GeoCoordinate(-45, -150),
                    new GeoCoordinate(0, -180),
                    new GeoCoordinate(45, 150)
                }
            };
                
            var routeDistance = Math.Round(route.GetVoyageDistance(), 4);
            var splittedRoute = route.SplitToSegments(routeDistance / 1000);
            //var splittedRouteDistance = Math.Round(splittedRoute.GetVoyageDistance(), 4);
            var splittedRouteDistance = Math.Round(splittedRoute.GetVoyageDistance(), 4);
            
            Assert.Equal(21633.224142116629, routeDistance.MetersToNauticalMiles()); // ~ Circumference of the earth
            Assert.Equal(21633.224142116629, splittedRouteDistance.MetersToNauticalMiles()); // ~ Circumference of the earth
            Assert.Equal(1003, splittedRoute.Waypoints.Count);

            WriteRouteToFile("inputRoute.txt", route.Waypoints); // TODO inconsistent
            WriteRouteToFile("splittedRoute.txt", splittedRoute.Waypoints);
        }

        [Fact]
        public void CanGetCourse()
        {
            var testVectors = new List<Tuple<GeoCoordinate, GeoCoordinate, double>>()
            {
                new Tuple<GeoCoordinate, GeoCoordinate, double>(new GeoCoordinate(0, 0), new GeoCoordinate(50,0), 0), // Straight north
                new Tuple<GeoCoordinate, GeoCoordinate, double>(new GeoCoordinate(0, 0), new GeoCoordinate(0,66), 90), // Straight east
                new Tuple<GeoCoordinate, GeoCoordinate, double>(new GeoCoordinate(0, 0), new GeoCoordinate(-80,0), 180), // Straight south
                new Tuple<GeoCoordinate, GeoCoordinate, double>(new GeoCoordinate(0, 0), new GeoCoordinate(0,-44), -90), // Straight west
                new Tuple<GeoCoordinate, GeoCoordinate, double>(new GeoCoordinate(50, 0), new GeoCoordinate(45, 45), 81.8622),
                new Tuple<GeoCoordinate, GeoCoordinate, double>(new GeoCoordinate(10, 20), new GeoCoordinate(30, 40), 40.1528),
                new Tuple<GeoCoordinate, GeoCoordinate, double>(new GeoCoordinate(-85, 0), new GeoCoordinate(-60, 180), 180), // Heading straight south to go "west" around earth
                new Tuple<GeoCoordinate, GeoCoordinate, double>(new GeoCoordinate(85, 10), new GeoCoordinate(20, -170), 0), // Heading straight north to go "west" around earth
            };

            foreach (var testVector in testVectors)
            {
                var course = Math.Round(testVector.Item1.GetCourse(testVector.Item2), 4);
                Assert.Equal(testVector.Item3, course);
            }
        }

        [Fact]
        public void CanGetRemainingRoute()
        {
            var route = new Route()
            {
                Waypoints = new List<GeoCoordinate>()
                {
                    new GeoCoordinate(0, 0),
                    new GeoCoordinate(10, 10),
                    new GeoCoordinate(20, 20),
                    new GeoCoordinate(30, 30),
                    new GeoCoordinate(30, 40)

                }
            };

            var remainingRoute = route.GetRemainingRoute(new GeoCoordinate(10, 10));
            Assert.Equal(new List<GeoCoordinate> { new GeoCoordinate(10, 10), new GeoCoordinate(20, 20), new GeoCoordinate(30, 30), new GeoCoordinate(30, 40) }, remainingRoute.Waypoints);
            remainingRoute = route.GetRemainingRoute(new GeoCoordinate(14, 10));
            Assert.Equal(new List<GeoCoordinate> { new GeoCoordinate(10, 10), new GeoCoordinate(20, 20), new GeoCoordinate(30, 30), new GeoCoordinate(30, 40) }, remainingRoute.Waypoints);
            remainingRoute = route.GetRemainingRoute(new GeoCoordinate(18, 18));
            Assert.Equal(new List<GeoCoordinate> { new GeoCoordinate(20, 20), new GeoCoordinate(30, 30), new GeoCoordinate(30, 40) }, remainingRoute.Waypoints);
            remainingRoute = route.GetRemainingRoute(new GeoCoordinate(30, 35));
            Assert.Equal(new List<GeoCoordinate> { new GeoCoordinate(30, 30), new GeoCoordinate(30, 40) }, remainingRoute.Waypoints);
            remainingRoute = route.GetRemainingRoute(new GeoCoordinate(30, 35.1));
            Assert.Equal(new List<GeoCoordinate> { new GeoCoordinate(30, 40) }, remainingRoute.Waypoints);
        }

        private void WriteRouteToFile(string path, List<GeoCoordinate> route)
        {
            List<string> fileOutput = new List<string>();
            foreach (var point in route)
            {
                var longitude = Math.Round(point.Longitude, 4).ToString().Replace(',', '.');
                var latitude = Math.Round(point.Latitude, 4).ToString().Replace(',', '.');
                fileOutput.Add(longitude + ',' + latitude);
            }
            File.WriteAllLines(path, fileOutput);
        }
    }
}
