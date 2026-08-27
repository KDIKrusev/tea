using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Models;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService;

namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Helpers
{
    public static class VoyageEnergyAdvisorGeoCoordinateHelper
    {
        public static double GetVoyageDistance(this Route route)
        {
            double distance = 0;
            var GeoCoordinates = route.Waypoints.Select(e => new GeoCoordinate(e.Latitude, e.Longitude)).ToList();
            for (int i = 0; i < GeoCoordinates.Count - 1; i++)
            {
                distance += GeoCoordinates[i].GetDistanceTo(GeoCoordinates[i + 1]);
            }
            return distance;
        }
        
        public static Route SplitToSegments(this Route route, double maxSegmentLength)
        {
            var interpolatedWaypoints = new List<GeoCoordinate>();

            var originalWaypoints = route.Waypoints.Select(wp => new GeoCoordinate(wp.Latitude, wp.Longitude)).ToList();

            for (int i = 1; i < originalWaypoints.Count; i++)
            {
                var start = originalWaypoints[i - 1];
                var end = originalWaypoints[i];

                double distance = start.GetDistanceTo(end);
                int segments = (int)Math.Ceiling(distance / maxSegmentLength);

                for (int j = 0; j < segments; j++)
                {
                    var interpolated = GetIntermediatePoint(start, end, (double)j / segments);
                    interpolatedWaypoints.Add(interpolated);
                }
            }

            // Ensure the last point is included
            if (originalWaypoints.Count > 0)
            {
                interpolatedWaypoints.Add(originalWaypoints.Last());
            }

            return new Route
            {
                RouteName = route.RouteName,
                Waypoints = interpolatedWaypoints
            };
        }

        public static double GetCourse(this GeoCoordinate startPos, GeoCoordinate endPos)
        {
            var longRadA = startPos.Longitude.DegToRad();
            var latRadA = startPos.Latitude.DegToRad();
            var longRadB = endPos.Longitude.DegToRad();
            var latRadB = endPos.Latitude.DegToRad();
            var deltaLong = longRadB - longRadA;
            var x = Math.Cos(latRadB) * Math.Sin(deltaLong);
            var y = Math.Cos(latRadA) * Math.Sin(latRadB) - Math.Sin(latRadA) * Math.Cos(latRadB) * Math.Cos(deltaLong);
            return Math.Atan2(x, y).RadToDeg();
        }

        public static Route GetRemainingRoute(this Route originalRoute, GeoCoordinate currentPosition)
        {
            if (originalRoute?.Waypoints == null || originalRoute.Waypoints.Count == 0)
                return new Route
                {
                    RouteName = originalRoute?.RouteName ?? "Unnamed",
                    Waypoints = new List<GeoCoordinate>()
                };

            int closestIndex = 0;
            double minDistance = double.MaxValue;

            for (int i = 0; i < originalRoute.Waypoints.Count; i++)
            {
                double distance = currentPosition.GetDistanceTo(originalRoute.Waypoints[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }

            var remainingWaypoints = originalRoute.Waypoints.Skip(closestIndex).ToList();

            return new Route
            {
                RouteName = originalRoute.RouteName,
                Waypoints = remainingWaypoints
            };
        }

        private static GeoCoordinate GetIntermediatePoint(
            GeoCoordinate startPos,
            GeoCoordinate endPos,
            double t // How much of the distance to use, from 0 through 1
        )
        {
            var alatRad = startPos.Latitude.DegToRad();
            var alonRad = startPos.Longitude.DegToRad();
            var blatRad = endPos.Latitude.DegToRad();
            var blonRad = endPos.Longitude.DegToRad();

            // Calculate distance in longitude
            var dlon = blonRad - alonRad;

            // Calculate common variables
            var alatRadSin = Math.Sin(alatRad);
            var blatRadSin = Math.Sin(blatRad);
            var alatRadCos = Math.Cos(alatRad);
            var blatRadCos = Math.Cos(blatRad);
            var dlonCos = Math.Cos(dlon);

            // Find distance from A to B
            var distance = Math.Acos(alatRadSin * blatRadSin +
                                   alatRadCos * blatRadCos *
                                   dlonCos);
            // Find course from A to B
            var course = Math.Atan2(
                Math.Sin(dlon) * blatRadCos,
                alatRadCos * blatRadSin -
                alatRadSin * blatRadCos * dlonCos);
            // Find new point
            var angularDistance = distance * t;
            var angDistSin = Math.Sin(angularDistance);
            var angDistCos = Math.Cos(angularDistance);
            var xlatRad = Math.Asin(alatRadSin * angDistCos +
                                     alatRadCos * angDistSin * Math.Cos(course));
            var xlonRad = alonRad + Math.Atan2(
                Math.Sin(course) * angDistSin * alatRadCos,
                angDistCos - alatRadSin * Math.Sin(xlatRad));

            var xlat = xlatRad.RadToDeg();
            var xlon = xlonRad.RadToDeg();

            if (xlat > 90) xlat = 90;
            if (xlat < -90) xlat = -90;
            while (xlon > 180) xlon -= 360;
            while (xlon <= -180) xlon += 360;
            return new GeoCoordinate(xlat, xlon);
        }
    }
}
