using VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels;
using Xunit;

namespace VoyageEnergyAdvisor.Test.RouteProviders
{
    /// <summary>
    /// Tests for auto-generated route provider model classes (Waypoint, Schedule, DefaultWaypoint).
    /// These are XML serialization models from RTZ schema.
    /// </summary>
    public class RouteProviderModelTests
    {
        #region Waypoint Tests

        [Fact]
        public void Waypoint_Constructor_InitializesWithDefaults()
        {
            // Arrange & Act
            var waypoint = new Waypoint();

            // Assert
            Assert.Null(waypoint.position);
            Assert.Null(waypoint.leg);
            Assert.Null(waypoint.extensions);
            Assert.Null(waypoint.id);
            Assert.Null(waypoint.revision);
            Assert.Null(waypoint.name);
            Assert.Equal(0, waypoint.radius);
            Assert.False(waypoint.radiusSpecified);
        }

        [Fact]
        public void Waypoint_IdProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var waypoint = new Waypoint();
            var expectedId = "123";

            // Act
            waypoint.id = expectedId;

            // Assert
            Assert.Equal(expectedId, waypoint.id);
        }

        [Fact]
        public void Waypoint_NameProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var waypoint = new Waypoint();
            var expectedName = "Test Waypoint";

            // Act
            waypoint.name = expectedName;

            // Assert
            Assert.Equal(expectedName, waypoint.name);
        }

        [Fact]
        public void Waypoint_RevisionProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var waypoint = new Waypoint();
            var expectedRevision = "5";

            // Act
            waypoint.revision = expectedRevision;

            // Assert
            Assert.Equal(expectedRevision, waypoint.revision);
        }

        [Fact]
        public void Waypoint_RadiusProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var waypoint = new Waypoint();
            var expectedRadius = 0.5m;

            // Act
            waypoint.radius = expectedRadius;

            // Assert
            Assert.Equal(expectedRadius, waypoint.radius);
        }

        [Fact]
        public void Waypoint_RadiusSpecified_CanBeSetAndRetrieved()
        {
            // Arrange
            var waypoint = new Waypoint();

            // Act
            waypoint.radiusSpecified = true;

            // Assert
            Assert.True(waypoint.radiusSpecified);
        }

        [Fact]
        public void Waypoint_PositionProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var waypoint = new Waypoint();
            var position = new GM_Point();

            // Act
            waypoint.position = position;

            // Assert
            Assert.Same(position, waypoint.position);
        }

        [Fact]
        public void Waypoint_LegProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var waypoint = new Waypoint();
            var leg = new Leg();

            // Act
            waypoint.leg = leg;

            // Assert
            Assert.Same(leg, waypoint.leg);
        }

        [Fact]
        public void Waypoint_ExtensionsProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var waypoint = new Waypoint();
            var extensions = new VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels.Extensions();

            // Act
            waypoint.extensions = extensions;

            // Assert
            Assert.Same(extensions, waypoint.extensions);
        }

        [Fact]
        public void Waypoint_AllProperties_CanBeSetTogether()
        {
            // Arrange
            var waypoint = new Waypoint();
            var position = new GM_Point();
            var leg = new Leg();
            var extensions = new VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels.Extensions();

            // Act
            waypoint.id = "456";
            waypoint.name = "Complete Waypoint";
            waypoint.revision = "10";
            waypoint.radius = 1.5m;
            waypoint.radiusSpecified = true;
            waypoint.position = position;
            waypoint.leg = leg;
            waypoint.extensions = extensions;

            // Assert
            Assert.Equal("456", waypoint.id);
            Assert.Equal("Complete Waypoint", waypoint.name);
            Assert.Equal("10", waypoint.revision);
            Assert.Equal(1.5m, waypoint.radius);
            Assert.True(waypoint.radiusSpecified);
            Assert.Same(position, waypoint.position);
            Assert.Same(leg, waypoint.leg);
            Assert.Same(extensions, waypoint.extensions);
        }

        #endregion

        #region Schedule Tests

        [Fact]
        public void Schedule_Constructor_InitializesWithDefaults()
        {
            // Arrange & Act
            var schedule = new Schedule();

            // Assert
            Assert.Null(schedule.manual);
            Assert.Null(schedule.calculated);
            Assert.Null(schedule.extensions);
            Assert.Null(schedule.id);
            Assert.Null(schedule.name);
        }

        [Fact]
        public void Schedule_IdProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var schedule = new Schedule();
            var expectedId = "789";

            // Act
            schedule.id = expectedId;

            // Assert
            Assert.Equal(expectedId, schedule.id);
        }

        [Fact]
        public void Schedule_NameProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var schedule = new Schedule();
            var expectedName = "Test Schedule";

            // Act
            schedule.name = expectedName;

            // Assert
            Assert.Equal(expectedName, schedule.name);
        }

        [Fact]
        public void Schedule_ManualProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var schedule = new Schedule();
            var manual = new Manual();

            // Act
            schedule.manual = manual;

            // Assert
            Assert.Same(manual, schedule.manual);
        }

        [Fact]
        public void Schedule_CalculatedProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var schedule = new Schedule();
            var calculated = new Calculated();

            // Act
            schedule.calculated = calculated;

            // Assert
            Assert.Same(calculated, schedule.calculated);
        }

        [Fact]
        public void Schedule_ExtensionsProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var schedule = new Schedule();
            var extensions = new VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels.Extensions();

            // Act
            schedule.extensions = extensions;

            // Assert
            Assert.Same(extensions, schedule.extensions);
        }

        [Fact]
        public void Schedule_AllProperties_CanBeSetTogether()
        {
            // Arrange
            var schedule = new Schedule();
            var manual = new Manual();
            var calculated = new Calculated();
            var extensions = new VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels.Extensions();

            // Act
            schedule.id = "999";
            schedule.name = "Complete Schedule";
            schedule.manual = manual;
            schedule.calculated = calculated;
            schedule.extensions = extensions;

            // Assert
            Assert.Equal("999", schedule.id);
            Assert.Equal("Complete Schedule", schedule.name);
            Assert.Same(manual, schedule.manual);
            Assert.Same(calculated, schedule.calculated);
            Assert.Same(extensions, schedule.extensions);
        }

        #endregion

        #region DefaultWaypoint Tests

        [Fact]
        public void DefaultWaypoint_Constructor_InitializesWithDefaults()
        {
            // Arrange & Act
            var defaultWaypoint = new DefaultWaypoint();

            // Assert
            Assert.Null(defaultWaypoint.leg);
            Assert.Null(defaultWaypoint.extensions);
            Assert.Equal(0, defaultWaypoint.radius);
            Assert.False(defaultWaypoint.radiusSpecified);
        }

        [Fact]
        public void DefaultWaypoint_RadiusProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var defaultWaypoint = new DefaultWaypoint();
            var expectedRadius = 2.5m;

            // Act
            defaultWaypoint.radius = expectedRadius;

            // Assert
            Assert.Equal(expectedRadius, defaultWaypoint.radius);
        }

        [Fact]
        public void DefaultWaypoint_RadiusSpecified_CanBeSetAndRetrieved()
        {
            // Arrange
            var defaultWaypoint = new DefaultWaypoint();

            // Act
            defaultWaypoint.radiusSpecified = true;

            // Assert
            Assert.True(defaultWaypoint.radiusSpecified);
        }

        [Fact]
        public void DefaultWaypoint_LegProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var defaultWaypoint = new DefaultWaypoint();
            var leg = new Leg();

            // Act
            defaultWaypoint.leg = leg;

            // Assert
            Assert.Same(leg, defaultWaypoint.leg);
        }

        [Fact]
        public void DefaultWaypoint_ExtensionsProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var defaultWaypoint = new DefaultWaypoint();
            var extensions = new VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels.Extensions();

            // Act
            defaultWaypoint.extensions = extensions;

            // Assert
            Assert.Same(extensions, defaultWaypoint.extensions);
        }

        [Fact]
        public void DefaultWaypoint_AllProperties_CanBeSetTogether()
        {
            // Arrange
            var defaultWaypoint = new DefaultWaypoint();
            var leg = new Leg();
            var extensions = new VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels.Extensions();

            // Act
            defaultWaypoint.radius = 3.0m;
            defaultWaypoint.radiusSpecified = true;
            defaultWaypoint.leg = leg;
            defaultWaypoint.extensions = extensions;

            // Assert
            Assert.Equal(3.0m, defaultWaypoint.radius);
            Assert.True(defaultWaypoint.radiusSpecified);
            Assert.Same(leg, defaultWaypoint.leg);
            Assert.Same(extensions, defaultWaypoint.extensions);
        }

        #endregion

        #region Route Tests

        [Fact]
        public void Route_Constructor_InitializesVersionToDefault()
        {
            // Arrange & Act
            var route = new Route();

            // Assert
            Assert.Equal("1.0", route.version);
            Assert.Null(route.routeInfo);
            Assert.Null(route.waypoints);
            Assert.Null(route.schedules);
            Assert.Null(route.extensions);
        }

        [Fact]
        public void Route_VersionProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var route = new Route();
            var expectedVersion = "2.0";

            // Act
            route.version = expectedVersion;

            // Assert
            Assert.Equal(expectedVersion, route.version);
        }

        [Fact]
        public void Route_RouteInfoProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var route = new Route();
            var routeInfo = new RouteInfo();

            // Act
            route.routeInfo = routeInfo;

            // Assert
            Assert.Same(routeInfo, route.routeInfo);
        }

        [Fact]
        public void Route_WaypointsProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var route = new Route();
            var waypoints = new Waypoints();

            // Act
            route.waypoints = waypoints;

            // Assert
            Assert.Same(waypoints, route.waypoints);
        }

        [Fact]
        public void Route_SchedulesProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var route = new Route();
            var schedules = new Schedules();

            // Act
            route.schedules = schedules;

            // Assert
            Assert.Same(schedules, route.schedules);
        }

        [Fact]
        public void Route_ExtensionsProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var route = new Route();
            var extensions = new VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels.Extensions();

            // Act
            route.extensions = extensions;

            // Assert
            Assert.Same(extensions, route.extensions);
        }

        [Fact]
        public void Route_AllProperties_CanBeSetTogether()
        {
            // Arrange
            var route = new Route();
            var routeInfo = new RouteInfo();
            var waypoints = new Waypoints();
            var schedules = new Schedules();
            var extensions = new VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels.Extensions();

            // Act
            route.version = "1.1";
            route.routeInfo = routeInfo;
            route.waypoints = waypoints;
            route.schedules = schedules;
            route.extensions = extensions;

            // Assert
            Assert.Equal("1.1", route.version);
            Assert.Same(routeInfo, route.routeInfo);
            Assert.Same(waypoints, route.waypoints);
            Assert.Same(schedules, route.schedules);
            Assert.Same(extensions, route.extensions);
        }

        #endregion

        #region Waypoints Tests

        [Fact]
        public void Waypoints_Constructor_InitializesWithDefaults()
        {
            // Arrange & Act
            var waypoints = new Waypoints();

            // Assert
            Assert.Null(waypoints.defaultWaypoint);
            Assert.Null(waypoints.waypoint);
            Assert.Null(waypoints.extensions);
        }

        [Fact]
        public void Waypoints_DefaultWaypointProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var waypoints = new Waypoints();
            var defaultWaypoint = new DefaultWaypoint();

            // Act
            waypoints.defaultWaypoint = defaultWaypoint;

            // Assert
            Assert.Same(defaultWaypoint, waypoints.defaultWaypoint);
        }

        [Fact]
        public void Waypoints_WaypointArrayProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var waypoints = new Waypoints();
            var waypointArray = new Waypoint[] { new Waypoint(), new Waypoint() };

            // Act
            waypoints.waypoint = waypointArray;

            // Assert
            Assert.Same(waypointArray, waypoints.waypoint);
            Assert.Equal(2, waypoints.waypoint.Length);
        }

        [Fact]
        public void Waypoints_ExtensionsProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var waypoints = new Waypoints();
            var extensions = new VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels.Extensions();

            // Act
            waypoints.extensions = extensions;

            // Assert
            Assert.Same(extensions, waypoints.extensions);
        }

        [Fact]
        public void Waypoints_AllProperties_CanBeSetTogether()
        {
            // Arrange
            var waypoints = new Waypoints();
            var defaultWaypoint = new DefaultWaypoint();
            var waypointArray = new Waypoint[] { new Waypoint() };
            var extensions = new VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels.Extensions();

            // Act
            waypoints.defaultWaypoint = defaultWaypoint;
            waypoints.waypoint = waypointArray;
            waypoints.extensions = extensions;

            // Assert
            Assert.Same(defaultWaypoint, waypoints.defaultWaypoint);
            Assert.Same(waypointArray, waypoints.waypoint);
            Assert.Same(extensions, waypoints.extensions);
        }

        #endregion

        #region Schedules Tests

        [Fact]
        public void Schedules_Constructor_InitializesWithDefaults()
        {
            // Arrange & Act
            var schedules = new Schedules();

            // Assert
            Assert.Null(schedules.schedule);
            Assert.Null(schedules.extensions);
        }

        [Fact]
        public void Schedules_ScheduleArrayProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var schedules = new Schedules();
            var scheduleArray = new Schedule[] { new Schedule(), new Schedule() };

            // Act
            schedules.schedule = scheduleArray;

            // Assert
            Assert.Same(scheduleArray, schedules.schedule);
            Assert.Equal(2, schedules.schedule.Length);
        }

        [Fact]
        public void Schedules_ExtensionsProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var schedules = new Schedules();
            var extensions = new VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels.Extensions();

            // Act
            schedules.extensions = extensions;

            // Assert
            Assert.Same(extensions, schedules.extensions);
        }

        [Fact]
        public void Schedules_AllProperties_CanBeSetTogether()
        {
            // Arrange
            var schedules = new Schedules();
            var scheduleArray = new Schedule[] { new Schedule() };
            var extensions = new VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels.Extensions();

            // Act
            schedules.schedule = scheduleArray;
            schedules.extensions = extensions;

            // Assert
            Assert.Same(scheduleArray, schedules.schedule);
            Assert.Same(extensions, schedules.extensions);
        }

        #endregion

        #region GM_Point Tests

        [Fact]
        public void GMPoint_Constructor_InitializesWithDefaults()
        {
            // Arrange & Act
            var point = new GM_Point();

            // Assert
            Assert.Equal(0m, point.lat);
            Assert.Equal(0m, point.lon);
        }

        [Fact]
        public void GMPoint_LatProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var point = new GM_Point();
            var expectedLat = 59.9139m;

            // Act
            point.lat = expectedLat;

            // Assert
            Assert.Equal(expectedLat, point.lat);
        }

        [Fact]
        public void GMPoint_LonProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var point = new GM_Point();
            var expectedLon = 10.7522m;

            // Act
            point.lon = expectedLon;

            // Assert
            Assert.Equal(expectedLon, point.lon);
        }

        [Fact]
        public void GMPoint_BothCoordinates_CanBeSetTogether()
        {
            // Arrange
            var point = new GM_Point();

            // Act
            point.lat = 51.5074m;
            point.lon = -0.1278m;

            // Assert
            Assert.Equal(51.5074m, point.lat);
            Assert.Equal(-0.1278m, point.lon);
        }

        #endregion

        #region Calculated Tests

        [Fact]
        public void Calculated_Constructor_InitializesWithDefaults()
        {
            // Arrange & Act
            var calculated = new Calculated();

            // Assert
            Assert.Null(calculated.sheduleElement);
            Assert.Null(calculated.extensions);
        }

        [Fact]
        public void Calculated_SheduleElementArrayProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var calculated = new Calculated();
            var elements = new ScheduleElement[] { new ScheduleElement(), new ScheduleElement() };

            // Act
            calculated.sheduleElement = elements;

            // Assert
            Assert.Same(elements, calculated.sheduleElement);
            Assert.Equal(2, calculated.sheduleElement.Length);
        }

        [Fact]
        public void Calculated_ExtensionsProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var calculated = new Calculated();
            var extensions = new VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels.Extensions();

            // Act
            calculated.extensions = extensions;

            // Assert
            Assert.Same(extensions, calculated.extensions);
        }

        [Fact]
        public void Calculated_AllProperties_CanBeSetTogether()
        {
            // Arrange
            var calculated = new Calculated();
            var elements = new ScheduleElement[] { new ScheduleElement() };
            var extensions = new VoyageEnergyAdvisor.Core.Services.RouteProviders.RouteProviderModels.Extensions();

            // Act
            calculated.sheduleElement = elements;
            calculated.extensions = extensions;

            // Assert
            Assert.Same(elements, calculated.sheduleElement);
            Assert.Same(extensions, calculated.extensions);
        }

        #endregion
    }
}
