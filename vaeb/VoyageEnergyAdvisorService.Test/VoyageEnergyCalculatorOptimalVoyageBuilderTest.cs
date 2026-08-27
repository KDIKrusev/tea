using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Models.VoyageEnergyAdvisor;
using VoyageEnergyAdvisor.Core.Services.AisService;
using VoyageEnergyAdvisor.Core.Services.CalmWaterResistanceService;
using VoyageEnergyAdvisor.Core.Services.CostCalculationService;
using VoyageEnergyAdvisor.Core.Services.CurrentResistanceService;
using VoyageEnergyAdvisor.Core.Services.FuelConsumptionService;
using VoyageEnergyAdvisor.Core.Services.ProgressService;
using VoyageEnergyAdvisor.Core.Services.SailContributionService;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Exceptions;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Helpers;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models;
using VoyageEnergyAdvisor.Core.Services.WaveResistanceService;
using VoyageEnergyAdvisor.Core.Services.WeatherService;
using VoyageEnergyAdvisor.Core.Services.WindResistanceService;
using Xunit;
using WeatherData = VoyageEnergyAdvisor.Core.CommonModels.WeatherData;

namespace VoyageEnergyAdvisorService.Test
{
    // Dedicated mocks with speed-dependent (rather than flat) resistance values, so the power-balance
    // convergence of the optimal-voyage search can be verified against a closed-form expected result.
    [Collection("Non-Parallel Tests")]
    public class VoyageEnergyAdvisorOptimalVoyageBuilderTest
    {
        private readonly Mock<IWeatherService> _weatherService = new();
        private readonly Mock<ICalmWaterResistanceService> _calmWaterResistanceService = new();
        private readonly Mock<IWindResistanceService> _windResistanceService = new();
        private readonly Mock<ICurrentResistanceService> _currentResistanceService = new();
        private readonly Mock<IWaveResistanceService> _waveResistanceService = new();
        private readonly Mock<ISailContributionService> _sailContributionService = new();
        private readonly Mock<IFuelConsumptionService> _fuelConsumptionService = new();
        private readonly Mock<ICostCalculationService> _costCalculationService = new();
        private readonly Mock<IProgressService> _progressService = new();

        private const double CalmWaterForceConstant = 100.0; // Watts per (m/s): power = CalmWaterForceConstant * speed

        // Read by the single GetWindResistancePower setup below, so each test can steer wind contribution
        // by assignment rather than re-configuring the mock (avoiding any Moq setup-override ambiguity).
        private double _windContributionWatts;

        public VoyageEnergyAdvisorOptimalVoyageBuilderTest()
        {
            _calmWaterResistanceService
                .Setup(x => x.GetCalmWaterResistancePower(It.IsAny<double>()))
                .Returns((double speed) => CalmWaterForceConstant * speed);

            _windResistanceService
                .Setup(x => x.GetWindResistancePower(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns((double windSpeed, double windDirection, double sog) => windDirection == 0 ? 0.0 : _windContributionWatts);
            _currentResistanceService
                .Setup(x => x.GetCurrentResistancePower(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(0);
            _waveResistanceService
                .Setup(x => x.GetWaveResistancePower(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(0);
            _sailContributionService
                .Setup(x => x.GetSailContributionPower(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(0);
            _fuelConsumptionService
                .Setup(x => x.GetFuelConsumption(It.IsAny<double>()))
                .Returns(0);
            _costCalculationService
                .Setup(x => x.GetFuelPricePerKg())
                .Returns(0);

            _weatherService
                .Setup(x => x.GetWeather(It.IsAny<IEnumerable<WeatherRequestInstance>>(), It.IsAny<Func<double, string, Task>>()))
                .ReturnsAsync((IEnumerable<WeatherRequestInstance> requests, Func<double, string, Task>? _) =>
                    requests.Select(r => new WeatherResponseInstance
                    {
                        Time = r.Time,
                        Location = r.Location,
                        Weather = new WeatherData
                        {
                            WindSpeed = 5,
                            WindFromDirection = 200,
                            CurrentSpeed = 0,
                            CurrentFromDirection = 0,
                            WaveHeight = 0,
                            WavePeakPeriod = 0,
                            WaveFromDirection = 0
                        }
                    }));
        }

        private VoyageEnergyAdvisorVoyageOptionsBuilder CreateBuilder()
        {
            return new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);
        }

        private static Route GetShortSingleSegmentRoute()
        {
            return new Route
            {
                RouteName = "Optimal Test Route",
                Waypoints = new List<GeoCoordinate>
                {
                    new GeoCoordinate(0.0, 0.0),
                    new GeoCoordinate(0.0, 0.05)
                }
            };
        }

        [Fact]
        public void CalculateRequiredAverageSpeed_ReturnsDistanceOverDuration()
        {
            var builder = CreateBuilder();
            var etd = DateTime.UtcNow;
            var eta = etd.AddHours(1);

            var result = builder.CalculateRequiredAverageSpeed(3600.0 * 4, etd, eta);

            Assert.Equal(4.0, result, 6);
        }

        [Fact]
        public void CalculateRequiredAverageSpeed_ReturnsZero_WhenDurationIsNotPositive()
        {
            var builder = CreateBuilder();
            var etd = DateTime.UtcNow;

            var result = builder.CalculateRequiredAverageSpeed(1000, etd, etd);

            Assert.Equal(0, result);
        }

        [Fact]
        public void CalculateSegmentPowerBalance_ReturnsSurplus_WhenPowerExceedsCalmWaterResistance()
        {
            var builder = CreateBuilder();
            var segment = new VoyageEnergyAdvisorVoyageOptionRouteSegment { Course = 0 };

            // Calm water resistance at 5 m/s is 100*5=500W; 1000W of propulsion leaves a 500W surplus.
            var balance = builder.CalculateSegmentPowerBalance(segment, constantPropulsionPower: 1000, candidateSpeed: 5);

            Assert.Equal(500.0, balance, 3);
        }

        [Fact]
        public void SolveSegmentSpeedForConstantPower_ConvergesToSpeedWhereCalmWaterResistanceEqualsPower()
        {
            var builder = CreateBuilder();
            var segment = new VoyageEnergyAdvisorVoyageOptionRouteSegment { Course = 0 };

            // 100 * V = 800 => V = 8
            var solvedSpeed = builder.SolveSegmentSpeedForConstantPower(segment, constantPropulsionPower: 800, speedMin: 1, speedMax: 20);

            Assert.True(Math.Abs(solvedSpeed - 8.0) < 0.15, $"Expected ~8.0, got {solvedSpeed}");
        }

        [Fact]
        public void SolveSegmentSpeedForConstantPower_RequiresLowerPower_WithFavorableWindContribution()
        {
            _windContributionWatts = -300;

            var builder = CreateBuilder();
            var segment = new VoyageEnergyAdvisorVoyageOptionRouteSegment
            {
                Course = 0,
                TrueWeather = new WeatherData { WindSpeed = 8, WindFromDirection = 200 }
            };

            // (100 * V) - 300 = 800 => V = 11
            var solvedSpeed = builder.SolveSegmentSpeedForConstantPower(segment, constantPropulsionPower: 800, speedMin: 1, speedMax: 20);

            Assert.True(Math.Abs(solvedSpeed - 11.0) < 0.15, $"Expected ~11.0, got {solvedSpeed}");
        }

        [Fact]
        public void SolveSegmentSpeedForConstantPower_RequiresHigherPower_WithAdverseWindContribution()
        {
            _windContributionWatts = 300;

            var builder = CreateBuilder();
            var segment = new VoyageEnergyAdvisorVoyageOptionRouteSegment
            {
                Course = 0,
                TrueWeather = new WeatherData { WindSpeed = 8, WindFromDirection = 20 }
            };

            // (100 * V) + 300 = 800 => V = 5
            var solvedSpeed = builder.SolveSegmentSpeedForConstantPower(segment, constantPropulsionPower: 800, speedMin: 1, speedMax: 20);

            Assert.True(Math.Abs(solvedSpeed - 5.0) < 0.15, $"Expected ~5.0, got {solvedSpeed}");
        }

        [Fact]
        public async Task BuildOptimalVoyageOption_CalmWaterOnly_ConvergesNearRequiredPower()
        {
            var builder = CreateBuilder();
            var route = GetShortSingleSegmentRoute();
            var distance = route.GetVoyageDistance();

            const double requiredSpeed = 5.0;
            var etd = DateTime.UtcNow.AddHours(1);
            var eta = etd.AddSeconds(distance / requiredSpeed);

            var request = new VoyageEnergyAdvisorOptimalVoyageRequest
            {
                Etd = etd,
                Eta = eta,
                SpeedMin = 1,
                SpeedMax = 20,
                Route = route
            };

            var result = await builder.BuildOptimalVoyageOption(request, requiredSpeed);

            Assert.True(result.IsValid);
            Assert.NotEmpty(result.RouteSegments);
            Assert.NotNull(result.AverageResistancePower);
            // Expected constant power ~= CalmWaterForceConstant * requiredSpeed = 500W
            Assert.True(Math.Abs(result.AverageResistancePower!.Value - 500.0) < 60.0,
                $"Expected ~500W, got {result.AverageResistancePower}");
        }

        [Fact]
        public async Task BuildOptimalVoyageOption_FavorableWind_RequiresLowerPowerThanCalmWaterOnly()
        {
            _windContributionWatts = -300;

            var builder = CreateBuilder();
            var route = GetShortSingleSegmentRoute();
            var distance = route.GetVoyageDistance();

            const double requiredSpeed = 5.0;
            var etd = DateTime.UtcNow.AddHours(1);
            var eta = etd.AddSeconds(distance / requiredSpeed);

            var request = new VoyageEnergyAdvisorOptimalVoyageRequest
            {
                Etd = etd,
                Eta = eta,
                SpeedMin = 1,
                SpeedMax = 20,
                Route = route
            };

            var result = await builder.BuildOptimalVoyageOption(request, requiredSpeed);

            Assert.True(result.IsValid);
            // Expected constant power ~= 500 - 300 = 200W, comfortably below the calm-water-only 500W baseline.
            Assert.True(result.AverageResistancePower!.Value < 400.0,
                $"Expected power well below the 500W calm-water baseline, got {result.AverageResistancePower}");
        }

        [Fact]
        public async Task BuildOptimalVoyageOption_AdverseWind_RequiresHigherPowerThanCalmWaterOnly()
        {
            _windContributionWatts = 300;

            var builder = CreateBuilder();
            var route = GetShortSingleSegmentRoute();
            var distance = route.GetVoyageDistance();

            const double requiredSpeed = 5.0;
            var etd = DateTime.UtcNow.AddHours(1);
            var eta = etd.AddSeconds(distance / requiredSpeed);

            var request = new VoyageEnergyAdvisorOptimalVoyageRequest
            {
                Etd = etd,
                Eta = eta,
                SpeedMin = 1,
                SpeedMax = 20,
                Route = route
            };

            var result = await builder.BuildOptimalVoyageOption(request, requiredSpeed);

            Assert.True(result.IsValid);
            // Expected constant power ~= 500 + 300 = 800W, comfortably above the calm-water-only 500W baseline.
            Assert.True(result.AverageResistancePower!.Value > 600.0,
                $"Expected power well above the 500W calm-water baseline, got {result.AverageResistancePower}");
        }

        [Fact]
        public async Task BuildOptimalVoyageOption_ThrowsOptimalVoyageRequestException_WhenPowerBandCannotMeetEta()
        {
            // Adverse wind resistance so large that even at the top of the +-80% search band
            // (see AverageSpeedSearchBandFraction), calm-water-derived power can't overcome it.
            _windContributionWatts = 1_000_000;

            var builder = CreateBuilder();
            var route = GetShortSingleSegmentRoute();
            var distance = route.GetVoyageDistance();

            const double requiredSpeed = 5.0;
            var etd = DateTime.UtcNow.AddHours(1);
            var eta = etd.AddSeconds(distance / requiredSpeed);

            var request = new VoyageEnergyAdvisorOptimalVoyageRequest
            {
                Etd = etd,
                Eta = eta,
                SpeedMin = 1,
                SpeedMax = 20,
                Route = route
            };

            await Assert.ThrowsAsync<OptimalVoyageRequestException>(
                () => builder.BuildOptimalVoyageOption(request, requiredAverageSpeed: requiredSpeed));
        }

        [Fact]
        public async Task GetOptimalVoyageOption_WithVaryingWeatherAlongRoute_KeepsResistancePowerConsistentAcrossSegments()
        {
            // Sweep true wind direction from 120 to 240 degrees along the route's longitude. This range is
            // deliberately kept clear of the eastbound route's course (~90) and course+180 (~270): hitting
            // either exactly collapses the apparent wind's lateral (Y) component to zero for every candidate
            // speed at that segment, which is the degenerate coincidence that broke an earlier test in this
            // file (true wind direction landing exactly on course).
            const double routeLengthDegrees = 0.5;
            _weatherService
                .Setup(x => x.GetWeather(It.IsAny<IEnumerable<WeatherRequestInstance>>(), It.IsAny<Func<double, string, Task>>()))
                .ReturnsAsync((IEnumerable<WeatherRequestInstance> requests, Func<double, string, Task>? _) =>
                    requests.Select(r => new WeatherResponseInstance
                    {
                        Time = r.Time,
                        Location = r.Location,
                        Weather = new WeatherData
                        {
                            WindSpeed = 8,
                            WindFromDirection = 120 + (r.Location.Longitude / routeLengthDegrees) * 120,
                            CurrentSpeed = 0,
                            CurrentFromDirection = 0,
                            WaveHeight = 0,
                            WavePeakPeriod = 0,
                            WaveFromDirection = 0
                        }
                    }));

            // Unlike the flat _windContributionWatts used by the other tests, this makes the wind
            // contribution itself vary with apparent direction, so segments genuinely require different
            // steady-state speeds instead of all shifting by the same fixed offset.
            _windResistanceService
                .Setup(x => x.GetWindResistancePower(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns((double windSpeed, double windDirection, double sog) =>
                    windDirection == 0 ? 0.0 : (windDirection - 180.0) * 3.0);

            var builder = CreateBuilder();
            var service = new VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.VoyageEnergyAdvisorService(
                builder,
                new Mock<IAisService>().Object,
                new Mock<ILogger<VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.VoyageEnergyAdvisorService>>().Object);

            var route = new Route
            {
                RouteName = "Multi-Segment Optimal Test Route",
                Waypoints = new List<GeoCoordinate>
                {
                    new GeoCoordinate(0.0, 0.0),
                    new GeoCoordinate(0.0, routeLengthDegrees) // ~55 km => multiple 10 km segments
                }
            };
            var distance = route.GetVoyageDistance();

            const double requiredSpeed = 5.0;
            var etd = DateTime.UtcNow.AddHours(1);
            var eta = etd.AddSeconds(distance / requiredSpeed);

            var request = new VoyageEnergyAdvisorOptimalVoyageRequest
            {
                Etd = etd,
                Eta = eta,
                SpeedMin = 1,
                SpeedMax = 20,
                Route = route
            };

            var option = await service.GetOptimalVoyageOption(request);

            Assert.True(option.IsValid);
            Assert.True(option.RouteSegments.Count > 1,
                $"Expected the route to split into multiple segments, got {option.RouteSegments.Count}.");

            // Empirically: 6 segments, speeds spread ~3.75-6.27 m/s, powers cluster tightly at ~137-143W.
            var speeds = option.RouteSegments.Select(s => s.AverageSpeed!.Value).ToList();
            Assert.True(speeds.Max() - speeds.Min() > 1.0,
                $"Expected meaningfully different speeds across segments, got [{string.Join(", ", speeds)}].");

            var powers = option.RouteSegments.Select(s => s.AvgTotalResistancePower!.Value).ToList();
            var averagePower = powers.Average();
            Assert.All(powers, p => Assert.True(Math.Abs(p - averagePower) < 0.1 * averagePower,
                $"Expected AvgTotalResistancePower to stay close to {averagePower:F1}W across all segments, " +
                $"got [{string.Join(", ", powers.Select(x => x.ToString("F1")))}]."));
        }

        [Fact]
        public async Task MySanityTest()
        {
            // Sweep true wind direction from 120 to 240 degrees along the route's longitude. This range is
            // deliberately kept clear of the eastbound route's course (~90) and course+180 (~270): hitting
            // either exactly collapses the apparent wind's lateral (Y) component to zero for every candidate
            // speed at that segment, which is the degenerate coincidence that broke an earlier test in this
            // file (true wind direction landing exactly on course).
            const double routeLengthDegrees = 0.5;
            _weatherService
                .Setup(x => x.GetWeather(It.IsAny<IEnumerable<WeatherRequestInstance>>(), It.IsAny<Func<double, string, Task>>()))
                .ReturnsAsync((IEnumerable<WeatherRequestInstance> requests, Func<double, string, Task>? _) =>
                    requests.Select(r => new WeatherResponseInstance
                    {
                        Time = r.Time,
                        Location = r.Location,
                        Weather = new WeatherData
                        {
                            WindSpeed = 8,
                            WindFromDirection = 120 + (r.Location.Longitude / routeLengthDegrees) * 120,
                            CurrentSpeed = 0,
                            CurrentFromDirection = 0,
                            WaveHeight = 0,
                            WavePeakPeriod = 0,
                            WaveFromDirection = 0
                        }
                    }));

            // Unlike the flat _windContributionWatts used by the other tests, this makes the wind
            // contribution itself vary with apparent direction, so segments genuinely require different
            // steady-state speeds instead of all shifting by the same fixed offset.
            _windResistanceService
                .Setup(x => x.GetWindResistancePower(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns((double windSpeed, double windDirection, double sog) =>
                    windDirection == 0 ? 0.0 : (windDirection - 180.0) * 3.0);

            // Make sail contribution a function of apparent weather instead of the constructor's flat 0:
            // headwind (inside a ~30 deg no-go cone around dead ahead) is a resistance, side/tail wind is
            // a contribution, and the magnitude scales with apparent wind speed. Cos(30deg) - Cos(angle)
            // is negative for angle < 30 (resistance), crosses zero at the no-go boundary, and grows
            // positive through beam and tail wind (largest contribution dead downwind).
            const double sailContributionCoefficient = 5.0; // W per (m/s apparent wind) at full effect
            const double noGoZoneDegrees = 30.0;
            _sailContributionService
                .Setup(x => x.GetSailContributionPower(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns((double apparentWindSpeed, double apparentWindDirection, double sog) =>
                {
                    var contributionShape = Math.Cos(noGoZoneDegrees.DegToRad()) - Math.Cos(apparentWindDirection.DegToRad());
                    return contributionShape * apparentWindSpeed * sailContributionCoefficient;
                });

            var builder = CreateBuilder();
            var service = new VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.VoyageEnergyAdvisorService(
                builder,
                new Mock<IAisService>().Object,
                new Mock<ILogger<VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.VoyageEnergyAdvisorService>>().Object);

            var route = new Route
            {
                RouteName = "Multi-Segment Optimal Test Route",
                Waypoints = new List<GeoCoordinate>
                {
                    new GeoCoordinate(0.0, 0.0),
                    new GeoCoordinate(0.0, routeLengthDegrees) // ~55 km => multiple 10 km segments
                }
            };
            var distance = route.GetVoyageDistance();

            const double requiredSpeed = 5.0;
            var etd = DateTime.UtcNow.AddHours(1);
            var eta = etd.AddSeconds(distance / requiredSpeed);

            var request = new VoyageEnergyAdvisorOptimalVoyageRequest
            {
                Etd = etd,
                Eta = eta,
                SpeedMin = 1,
                SpeedMax = 20,
                Route = route
            };

            var option = await service.GetOptimalVoyageOption(request);

            Assert.True(option.IsValid);
            Assert.True(option.RouteSegments.Count > 1,
                $"Expected the route to split into multiple segments, got {option.RouteSegments.Count}.");

            foreach (var seg in option.RouteSegments)
            {
                Console.WriteLine(
                    $"speed={seg.AverageSpeed:F3} apparentWindDir={seg.ApparentWeather?.WindFromDirection:F1} " +
                    $"sail={seg.AvgSailResistancePower:F1} total={seg.AvgTotalResistancePower:F1}");
            }

            var speeds = option.RouteSegments.Select(s => s.AverageSpeed!.Value).ToList();
            var averageSpeed = speeds.Average();
            Console.WriteLine($"requiredSpeed={requiredSpeed:F3} averageSpeed={averageSpeed:F3} " +
                $"speedRange=[{speeds.Min():F3}, {speeds.Max():F3}]");

            // Speed should genuinely differ segment to segment (favorable/adverse apparent wind), and
            // should be centered on the schedule-required average speed rather than drifting off it.
            Assert.True(speeds.Max() - speeds.Min() > 1.0,
                $"Expected meaningfully different speeds across segments, got [{string.Join(", ", speeds)}].");
            Assert.True(Math.Abs(averageSpeed - requiredSpeed) < 0.5,
                $"Expected average speed near {requiredSpeed}, got {averageSpeed:F3}.");

            // But the constant-power search should hold propulsion power essentially fixed across
            // segments - that's the whole point of solving per-segment speed for a *constant* power.
            var powers = option.RouteSegments.Select(s => s.AvgTotalResistancePower!.Value).ToList();
            var averagePower = powers.Average();
            Console.WriteLine($"averagePower={averagePower:F1} powerRange=[{powers.Min():F1}, {powers.Max():F1}]");
            Assert.All(powers, p => Assert.True(Math.Abs(p - averagePower) < 0.1 * averagePower,
                $"Expected AvgTotalResistancePower to stay close to {averagePower:F1}W across all segments, " +
                $"got [{string.Join(", ", powers.Select(x => x.ToString("F1")))}]."));
        }
    }
}
