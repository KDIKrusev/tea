using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Services.CalmWaterResistanceService;
using VoyageEnergyAdvisor.Core.Services.CurrentResistanceService;
using VoyageEnergyAdvisor.Core.Services.SailContributionService;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models;
using VoyageEnergyAdvisor.Core.Services.WaveResistanceService;
using VoyageEnergyAdvisor.Core.Services.WindResistanceService;
using WeatherData = VoyageEnergyAdvisor.Core.CommonModels.WeatherData;

namespace VoyageEnergyAdvisorService.Test
{
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService;
    using VoyageEnergyAdvisor.Core.Models.VoyageEnergyAdvisor;
    using VoyageEnergyAdvisor.Core.Services.WeatherService;
    using VoyageEnergyAdvisor.Core.Services.ProgressService;
    using VoyageEnergyAdvisor.Core.Services.FuelConsumptionService;
    using VoyageEnergyAdvisor.Core.Services.CostCalculationService;
    using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Helpers;

    [Collection("Non-Parallel Tests")]
    public class VoyageEnergyAdvisorVoyageOptionsBuilderServiceTest
    {
        private readonly Mock<IWeatherService> _weatherService =
            new Mock<IWeatherService>();
        
        private readonly Mock<ICalmWaterResistanceService> _calmWaterResistanceService =
            new Mock<ICalmWaterResistanceService>();
        
        private readonly Mock<IWindResistanceService> _windResistanceService =
            new Mock<IWindResistanceService>();

        private readonly Mock<ICurrentResistanceService> _currentResistanceService =
            new Mock<ICurrentResistanceService>();
        
        private readonly Mock<IWaveResistanceService> _waveResistanceService =
            new Mock<IWaveResistanceService>();

        private readonly Mock<ISailContributionService> _sailContributionService =
            new Mock<ISailContributionService>();

        private readonly Mock<IFuelConsumptionService> _fuelConsumptionService =
             new Mock<IFuelConsumptionService>();

        private readonly Mock<ICostCalculationService> _costCalculationService =
            new Mock<ICostCalculationService>();

        private readonly Mock<IProgressService> _progressService =
            new Mock<IProgressService>();


        public VoyageEnergyAdvisorVoyageOptionsBuilderServiceTest()
        {
            _weatherService
                 .Setup(x => x.GetWeather(It.IsAny<IEnumerable<WeatherRequestInstance>>(), It.IsAny<Func<double, string, Task>>()))
                 .ReturnsAsync((IEnumerable<WeatherRequestInstance> requests, Func<double, string, Task>? callback) =>
                 {
                     if (callback != null)
                     {
                         callback(50.0, "Test progress").Wait();
                     }

                     return GetWeatherMockData(requests);
                 });

          // _weatherService.SetupGet(x => x.MaxForecastRange).Returns(TimeSpan.FromDays(9));

            _calmWaterResistanceService.Setup(x => x.GetCalmWaterResistancePower(It.IsAny<double>())).Returns(200);
            
            _windResistanceService.Setup(x => x.GetWindResistancePower(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns((double param1, double param2, double param3) => param2 > 179 && param2 < 181  ? 1000 : 1400);
            
            _currentResistanceService.Setup(x => x.GetCurrentResistancePower(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns((double param1, double param2, double param3) => param2 > 179 && param2 < 181  ? 500 : 700);

            _currentResistanceService.Setup(x => x.GetCurrentResistancePower(It.IsAny<double>(),180,It.IsAny<double>())).Returns(500);
            _currentResistanceService.Setup(x => x.GetCurrentResistancePower(It.IsAny<double>(),It.IsNotIn(180),It.IsAny<double>())).Returns(700);

            _waveResistanceService.Setup(x => x.GetWaveResistancePower(It.IsAny<double>(), It.IsAny<double>(),It.IsAny<double>(),It.IsAny<double>())).Returns(14);

            _fuelConsumptionService.Setup(x => x.GetFuelConsumption(It.IsAny<double>())).Returns(200);

            _sailContributionService.Setup(x => x.GetSailContributionPower(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(50); 
        }

        [Fact]
        void CanGetVoyageOptionsArray()
        {
            var request = GetRequestMockData();
            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);
            var response = optionsBuilder.GetVoyageOptionsArray(request).ToList();

            Assert.Equal(request.EtdMin!.Value, response.OrderBy(e => e.Etd).First().Etd);
            Assert.Equal(request.EtdMax!.Value, response.OrderBy(e => e.Etd).Last().Etd);
            Assert.Equal(request.EtaMin!.Value, response.OrderBy(e => e.Eta).First().Eta);
            Assert.Equal(request.EtaMax!.Value, response.OrderBy(e => e.Eta).Last().Eta);

            foreach (var option in response)
            {
                var resultDistance = option.AverageSpeed * (option.Eta - option.Etd).TotalSeconds;
                Assert.Equal(40064731.11, Math.Round(resultDistance, 2));
            }

            Assert.Equal(Math.Pow(request.ReturnArrayDimension, 2), response.Count());
        }

        [Fact]
        void CanFilterOnSpeed()

        {
            var request = GetRequestMockData();

            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var response = optionsBuilder.GetVoyageOptionsArray(request).ToList();
            Assert.Equal(16, response.Count());
            var optionsWithTenHourDuration = response.Count(e => (e.Eta - e.Etd).TotalHours == 10);
            var optionsWithTenHourDurationSpeed = optionsBuilder.FilterOnSpeed(response, 2160.0.KnotsToMetersPerSecond(), 2165.0.KnotsToMetersPerSecond()).Where(e => e.IsValid).ToList();
            Assert.Equal(optionsWithTenHourDuration, optionsWithTenHourDurationSpeed.Count());
        }

        [Fact]
        void CanAddRouteSegments()
        {
            var request = GetRequestMockData();

            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var response = optionsBuilder.GetVoyageOptionsArray(request).ToList();
            response = optionsBuilder.AddRouteSegments(response, request.Route).ToList();

            foreach (var option in response)
            {
                Assert.NotEmpty(option.RouteSegments);
                var routeSegmentGeoCoordinates = option.RouteSegments.Select(e => e.StartPosition);
                routeSegmentGeoCoordinates = routeSegmentGeoCoordinates.Append(option.RouteSegments.Last().EndPosition);
                Assert.Equal(21633.224, Math.Round(new Route() { Waypoints = routeSegmentGeoCoordinates.ToList()! }.GetVoyageDistance().MetersToNauticalMiles(), 3));

                foreach (var routeSegment in option.RouteSegments)
                {
                    Assert.True(10000.0 >= routeSegment.StartPosition!.GetDistanceTo(routeSegment.EndPosition!));
                    Assert.Equal(option.AverageSpeed, routeSegment.AverageSpeed);
                }
            }
        }

        [Fact]
        void CanAddTimeToRouteSegments()
        {
            var request = GetRequestMockData();

            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var response = optionsBuilder.GetVoyageOptionsArray(request).ToList();


            response = optionsBuilder.AddRouteSegments(response, request.Route).ToList();
            response = optionsBuilder.AddTimeToRouteSegments(response).ToList();

            foreach (var option in response)
            {
                var optionDuration = option.Eta - option.Etd;
                var sumSegmentsDuration = new TimeSpan();
                int i = 0;
                foreach (var segment in option.RouteSegments)
                {
                    var segmentDuration = segment.EndTime - segment.StartTime;
                    sumSegmentsDuration = sumSegmentsDuration + segmentDuration;
                    Assert.Equal(segment.DurationInSeconds, segmentDuration.TotalSeconds);
                    if (i > 0)
                    {
                        Assert.Equal(option.RouteSegments[i - 1].EndTime, segment.StartTime);
                    }
                    i++;
                }

                Assert.Equal(Math.Round(optionDuration.TotalHours, 2), Math.Round(sumSegmentsDuration.TotalHours, 2));
            }
        }

        [Fact]
        void CanAddCourseToRouteSegments()
        {
            var request = GetRequestMockData();
            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var response = optionsBuilder.GetVoyageOptionsArray(request).ToList();
            response = optionsBuilder.AddRouteSegments(response, request.Route).ToList();
            response = optionsBuilder.AddCourseToRouteSegments(response).ToList();

            foreach (var option in response)
            {
                foreach (var segment in option.RouteSegments)
                {
                    Assert.Equal(segment.StartPosition!.GetCourse(segment.EndPosition!), segment.Course);
                }
            }
        }

        [Fact]
        async Task CanAddTrueWeatherToRouteSegments()
        {
            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var request = GetRequestMockData();
            var response = optionsBuilder.GetVoyageOptionsArray(request).ToList();
            response = optionsBuilder.AddRouteSegments(response, request.Route).ToList();
            response = optionsBuilder.AddTimeToRouteSegments(response).ToList();
            response = (await optionsBuilder.AddTrueWeatherToRouteSegments(response)).ToList();

            foreach (var option in response)
            {
                foreach (var segment in option.RouteSegments)
                {
                    var expectedWeather = segment.StartPosition!.Latitude + segment.StartPosition.Longitude;
                    Assert.Equal(expectedWeather, segment.TrueWeather!.CurrentFromDirection);
                    Assert.Equal(expectedWeather, segment.TrueWeather.CurrentSpeed);
                    Assert.Equal(expectedWeather, segment.TrueWeather.WaveFromDirection);
                    Assert.Equal(expectedWeather, segment.TrueWeather.WaveHeight);
                    Assert.Equal(expectedWeather, segment.TrueWeather.WindFromDirection);
                    Assert.Equal(expectedWeather, segment.TrueWeather.WindSpeed);
                    Assert.Equal(expectedWeather, segment.TrueWeather.WavePeakPeriod);
                }
            }
        }

        [Fact]
        void CanRequestDistinctGeoCoordinates()
        {
            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);


            var request = GetRequestMockData();

            var response = optionsBuilder.GetVoyageOptionsArray(request).ToList();

            var allWeather = response.SelectMany(voyageOptions =>
            {
                return voyageOptions.RouteSegments.Select(segment => new { segment.StartTime, segment.StartPosition });
            }).ToList();

            _weatherService.Setup(e => e.GetWeather(It.IsAny<IEnumerable<WeatherRequestInstance>>(), It.IsAny<Func<double, string, Task>>()))
                .ReturnsAsync((IEnumerable<WeatherRequestInstance> weatherRequest, Func<double, string, Task>? _) =>
                {
                    var weatherRequestList = weatherRequest.ToList();
                    var distinctWeatherRequestList =
                        weatherRequestList.GroupBy(e => new { e.Time, e.Location }).ToList();

                    Assert.Equal(weatherRequestList.Count, distinctWeatherRequestList.Count);
                    Assert.True(allWeather.Count > weatherRequestList.Count);
                    return new List<WeatherResponseInstance>();
                });

            response = optionsBuilder.AddRouteSegments(response, request.Route).ToList();
        }

        [Fact]
        async Task CanAddRelativeWeatherToRouteSegments()
        {
            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var request = GetRequestMockData();
            var response = optionsBuilder.GetVoyageOptionsArray(request).ToList();
            response = optionsBuilder.AddRouteSegments(response, request.Route).ToList();
            response = optionsBuilder.AddTimeToRouteSegments(response).ToList();
            response = optionsBuilder.AddCourseToRouteSegments(response).ToList();
            response = (await optionsBuilder.AddTrueWeatherToRouteSegments(response)).ToList();
            response = optionsBuilder.AddApparentWeatherToRouteSegments(response).ToList();
            foreach (var option in response)
            {
                foreach (var segment in option.RouteSegments)
                {
                    Assert.NotNull(segment.ApparentWeather!.WindSpeed);
                    Assert.NotNull(segment.ApparentWeather.WindFromDirection);
                    Assert.NotNull(segment.TrueWeather!.WindSpeed);
                    Assert.NotNull(segment.TrueWeather.WindFromDirection);

                    Assert.NotEqual(segment.TrueWeather.WindSpeed, segment.ApparentWeather.WindSpeed);

                    if (segment.TrueWeather.WindSpeed != 0)
                    {
                        Assert.NotEqual(segment.TrueWeather.WindFromDirection, segment.ApparentWeather.WindFromDirection);
                    }

                    Assert.NotNull(segment.ApparentWeather.CurrentSpeed);
                    Assert.NotNull(segment.ApparentWeather.CurrentFromDirection);
                    
                    if (segment.TrueWeather.CurrentSpeed != 0)
                    {
                        Assert.NotEqual(segment.TrueWeather.CurrentFromDirection, segment.ApparentWeather.CurrentFromDirection);
                    }
                }
            }
        }

        [Fact]
        public void AddCalmWaterPowerToRouteSegments_ShouldAddCalmWaterPowerToAllRouteSegments()
        {
            // Arrange
            var mockCalmWaterResistanceService = new Mock<ICalmWaterResistanceService>();
            mockCalmWaterResistanceService
                .Setup(service => service.GetCalmWaterResistancePower(It.IsAny<double>()))
                .Returns(200); // Example value

            var voyageOptions = new List<VoyageEnergyAdvisorVoyageOption>
            {
                new VoyageEnergyAdvisorVoyageOption
                {
                    RouteSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>()
                    {
                        {new VoyageEnergyAdvisorVoyageOptionRouteSegment(){AverageSpeed = 10}},
                        {new VoyageEnergyAdvisorVoyageOptionRouteSegment(){AverageSpeed = 15}},
                    }
                },
                new VoyageEnergyAdvisorVoyageOption
                {
                    RouteSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>()
                    {
                        {new VoyageEnergyAdvisorVoyageOptionRouteSegment(){AverageSpeed = 35}},
                        {new VoyageEnergyAdvisorVoyageOptionRouteSegment(){AverageSpeed = 45}},
                    }
                }
            };

            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                mockCalmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            // Act
            var result = optionsBuilder.AddCalmWaterPowerToRouteSegments(voyageOptions);

            // Assert
            var expectedCalmWaterPowers = new[] { 200.0, 200.0, 200.0, 200.0 };
            var actualCalmWaterPowers = result.SelectMany(vo => vo.RouteSegments).Select(rs => rs.AvgCalmWaterResistancePower!.Value).ToArray();

            Assert.Equal(expectedCalmWaterPowers, actualCalmWaterPowers);
        }

        [Fact]
        public void CanAddWindPowerToRouteSegments()
        {
            // Arrange
            var mockWindResistanceService = new Mock<IWindResistanceService>();
            mockWindResistanceService
                .Setup(service => service.GetWindResistancePower(It.IsAny<double>(), It.Is<double>(x => Math.Abs(x) <= 1), It.IsAny<double>()))
                .Returns(500);

            mockWindResistanceService
                .Setup(service => service.GetWindResistancePower(It.IsAny<double>(), It.Is<double>(x => Math.Abs(x) > 1), It.IsAny<double>()))
                .Returns(2000);

            var voyageOptions = new List<VoyageEnergyAdvisorVoyageOption>
            {
                new VoyageEnergyAdvisorVoyageOption
                {
                    RouteSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>()
                    {
                        {new VoyageEnergyAdvisorVoyageOptionRouteSegment(){AverageSpeed = 10, ApparentWeather = new WeatherData( ){WindFromDirection = 10, WindSpeed = 1}}},
                        {new VoyageEnergyAdvisorVoyageOptionRouteSegment(){AverageSpeed = 20, ApparentWeather = new WeatherData( ){WindFromDirection = 20, WindSpeed = 1}}},
                    }
                },
                new VoyageEnergyAdvisorVoyageOption
                {
                    RouteSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>()
                    {
                        {new VoyageEnergyAdvisorVoyageOptionRouteSegment(){AverageSpeed = 30, ApparentWeather = new WeatherData( ){WindFromDirection = 20, WindSpeed = 1}}},
                        {new VoyageEnergyAdvisorVoyageOptionRouteSegment(){AverageSpeed = 40, ApparentWeather = new WeatherData( ){WindFromDirection = 50, WindSpeed = 1}}},
                    }
                }
            };

            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                mockWindResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            // Act
            var result = optionsBuilder.AddWindPowerToRouteSegments(voyageOptions);

            // Assert
            var expectedWindresistancePowers = new[] { 1500.0, 1500.0, 1500.0, 1500.0 };
            var actualWindresistancePowers = result.SelectMany(vo => vo.RouteSegments).Select(rs => rs.AvgWindResistancePower!.Value).ToArray();

            Assert.Equal(expectedWindresistancePowers, actualWindresistancePowers);
        }

        [Fact]
        public void CanAddCurrentPowerToRouteSegments()
        {
            // Arrange
            var mockCurrentResistanceService = new Mock<ICurrentResistanceService>();
            mockCurrentResistanceService
                .Setup(service => service.GetCurrentResistancePower(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(200);

            var voyageOptions = new List<VoyageEnergyAdvisorVoyageOption>
            {
                new VoyageEnergyAdvisorVoyageOption
                {
                    RouteSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>()
                    {
                        {new VoyageEnergyAdvisorVoyageOptionRouteSegment(){AverageSpeed = 10, ApparentWeather = new WeatherData( ){CurrentFromDirection = 10, WindSpeed = 1}}},
                        {new VoyageEnergyAdvisorVoyageOptionRouteSegment(){AverageSpeed = 20, ApparentWeather = new WeatherData( ){CurrentFromDirection = 20, WindSpeed = 1}}},
                    }
                },
                new VoyageEnergyAdvisorVoyageOption
                {
                    RouteSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>()
                    {
                        {new VoyageEnergyAdvisorVoyageOptionRouteSegment(){AverageSpeed = 30, ApparentWeather = new WeatherData( ){CurrentFromDirection = 20, CurrentSpeed = 1}}},
                        {new VoyageEnergyAdvisorVoyageOptionRouteSegment(){AverageSpeed = 40, ApparentWeather = new WeatherData( ){CurrentFromDirection = 50, CurrentSpeed = 1}}},
                    }
                }
            };

            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                mockCurrentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            // Act
            var result = optionsBuilder.AddCurrentPowerToRouteSegments(voyageOptions);

            // Assert
            var expectedCurrentResistancePowers = new[] { 200.0, 200.0, 200.0, 200.0 };
            var actualCurrentResistancePowers = result.SelectMany(vo => vo.RouteSegments).Select(rs => rs.AvgCurrentResistancePower!.Value).ToArray();

            Assert.Equal(expectedCurrentResistancePowers, actualCurrentResistancePowers);
        }

        [Fact]
        void CanAddWavePowerToRouteSegments()
        {
            var input = new List<VoyageEnergyAdvisorVoyageOption>()
            {
                new VoyageEnergyAdvisorVoyageOption()
                {
                    RouteSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>()
                    {
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment()
                        {
                            AverageSpeed = 10,
                            ApparentWeather = new WeatherData()
                            {
                                WaveFromDirection = 50,
                                WaveHeight = 2,
                                WavePeakPeriod = 10
                            }
                        }
                    }
                }
            };

            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var result = optionsBuilder.AddWavePowerToRouteSegments(input);
            Assert.NotNull(result.First().RouteSegments.First().AvgWaveResistancePower);
            Assert.NotEqual(0, result.First().RouteSegments.First().AvgWaveResistancePower);
        }

        [Fact]
        public void CanAddSailPowerToRouteSegments()
        {
            var input = new List<VoyageEnergyAdvisorVoyageOption>()
            {
                new VoyageEnergyAdvisorVoyageOption()
                {
                    RouteSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>()
                    {
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment()
                        {
                            AverageSpeed = 10,
                            ApparentWeather = new WeatherData()
                            {
                                WaveFromDirection = 50,
                                WaveHeight = 2,
                                WavePeakPeriod = 10
                            }
                        }
                    }
                }
            };

            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var result = optionsBuilder.AddSailPowerToRouteSegments(input);
            Assert.NotNull(result.First().RouteSegments.First().AvgSailResistancePower);
            Assert.NotEqual(0, result.First().RouteSegments.First().AvgSailResistancePower);
            Assert.Equal(-50, result.First().RouteSegments.First().AvgSailResistancePower); // 50 is the sail contribution power, thus -50 will be the "resistance" from sail
        }

        [Fact]
        void CanAddTotalPowerToRouteSegment()
        {
            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var voyageOptions = GetVoyageOptionsMockData().ToList();
            voyageOptions = optionsBuilder.AddTotalPowerToRouteSegments(voyageOptions).ToList();
            Assert.Equal(86, voyageOptions.First().RouteSegments.First().AvgTotalResistancePower.GetValueOrDefault());
            Assert.Equal(166, voyageOptions.First().RouteSegments.Last().AvgTotalResistancePower.GetValueOrDefault());
            Assert.Equal(65, voyageOptions.Last().RouteSegments.First().AvgTotalResistancePower.GetValueOrDefault());
            Assert.Equal(90, voyageOptions.Last().RouteSegments.Last().AvgTotalResistancePower.GetValueOrDefault());
            Assert.Equal(54, voyageOptions.Last().RouteSegments.First().AvgNetWeatherResistancePower.GetValueOrDefault());
            Assert.Equal(74, voyageOptions.Last().RouteSegments.Last().AvgNetWeatherResistancePower.GetValueOrDefault());
        }

        [Fact]
        void CanAddFavorableWeatherIndexToRouteSegments()
        {
            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            // Arrange
            var voyageOptions = new List<VoyageEnergyAdvisorVoyageOption>()
            {
                new VoyageEnergyAdvisorVoyageOption()
                {
                    RouteSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>()
                    {
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment() { AvgCalmWaterResistancePower = 5, AvgTotalResistancePower = 6 },
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment() { AvgCalmWaterResistancePower = 5, AvgTotalResistancePower = 7 },
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment() { AvgCalmWaterResistancePower = 5, AvgTotalResistancePower = 9 },
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment() { AvgCalmWaterResistancePower = 5, AvgTotalResistancePower = 8 },
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment() { AvgCalmWaterResistancePower = 5, AvgTotalResistancePower = 4 },
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment() { AvgCalmWaterResistancePower = 5, AvgTotalResistancePower = 1 },
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment() { AvgCalmWaterResistancePower = 5, AvgTotalResistancePower = 2 },
                    }
                }
            };

            var expectedFavorableWeatherIndices = new List<double>
            {
                0.625, 0.75, 1, 0.875, 0.375, 0, 0.125
            };

            // Act
            var voyageOptionsResult = optionsBuilder.AddFavorableWeatherIndexToVoyageOptions(voyageOptions).ToList();

            // Assert
            var actualFavorableWeatherIndices = voyageOptionsResult.First().RouteSegments
                .Select(segment => segment.FavorableWeatherIndex)
                .ToList();

            Assert.Equal(expectedFavorableWeatherIndices, expectedFavorableWeatherIndices);
        }

        [Fact]
        void CanAddTotalPowerAndEnergyToVoyageOptions()
        {
            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var voyageOptions = GetVoyageOptionsMockData().ToList();
            voyageOptions = optionsBuilder.AddTotalPowerToRouteSegments(voyageOptions).ToList();
            voyageOptions = optionsBuilder.AddTotalPowerAndEnergyToVoyageOptions(voyageOptions).ToList();

            Assert.Equal(126, voyageOptions.First().AverageResistancePower.GetValueOrDefault());
            Assert.Equal(504, voyageOptions.First().TotalResistanceEnergyConsumption.GetValueOrDefault());
            Assert.Equal(62.580645161290313, voyageOptions.First().EnergyConsumptionRelative);
            Assert.Equal(600, voyageOptions.First().TotalCalmWaterResistanceEnergyConsumption.GetValueOrDefault());
            Assert.Equal(18, voyageOptions.First().AbsTotalWindEnergy.GetValueOrDefault());
            Assert.Equal(-3, voyageOptions.First().RelativeWindEnergyConsumption.GetValueOrDefault());
            Assert.Equal(22, voyageOptions.First().AbsTotalCurrentEnergy.GetValueOrDefault());
            Assert.Equal(-3.6666666666666665, voyageOptions.First().RelativeCurrentEnergyConsumption.GetValueOrDefault());
            Assert.Equal(26, voyageOptions.First().AbsTotalWaveEnergy.GetValueOrDefault());
            Assert.Equal(-4.333333333333333, voyageOptions.First().RelativeWaveEnergyConsumption.GetValueOrDefault());
            Assert.Equal(30, voyageOptions.First().AbsTotalSailEnergy.GetValueOrDefault());
            Assert.Equal(-5, voyageOptions.First().RelativeSailEnergyConsumption.GetValueOrDefault());
            Assert.Equal(77.5, voyageOptions.Last().AverageResistancePower.GetValueOrDefault());
            Assert.Equal(77.5 * 4, voyageOptions.Last().TotalResistanceEnergyConsumption.GetValueOrDefault());
            Assert.Equal(0, voyageOptions.Last().EnergyConsumptionRelative);
            Assert.Equal(58, voyageOptions.Last().AbsTotalWindEnergy.GetValueOrDefault());
            Assert.Equal(107.4074074074074, voyageOptions.Last().RelativeWindEnergyConsumption.GetValueOrDefault());
            Assert.Equal(62, voyageOptions.Last().AbsTotalCurrentEnergy.GetValueOrDefault());
            Assert.Equal(114.81481481481481, voyageOptions.Last().RelativeCurrentEnergyConsumption.GetValueOrDefault());
            Assert.Equal(66, voyageOptions.Last().AbsTotalWaveEnergy.GetValueOrDefault());
            Assert.Equal(122.22222222222223, voyageOptions.Last().RelativeWaveEnergyConsumption.GetValueOrDefault());
            Assert.Equal(70, voyageOptions.Last().AbsTotalSailEnergy.GetValueOrDefault());
            Assert.Equal(129.62962962962962, voyageOptions.Last().RelativeSailEnergyConsumption.GetValueOrDefault());
        }

        [Fact]
        public async Task AddTrueWeatherCanHandleInvalidRouteSegments2()
        {
            var instant = new DateTimeOffset(2022, 10, 11, 12, 0, 0, TimeSpan.Zero).DateTime;
            const double lat = 1, lon = 1;
            var voyageOptionsWithInvalidOption = GetVoyageOptionsWithInvalidOption(instant, lat, lon);

            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var results = (await optionsBuilder.AddTrueWeatherToRouteSegments(voyageOptionsWithInvalidOption)).ToList();

            Assert.True(results.Count > 0);
        }

        [Fact]
        public async Task CanPrepareVoyageOptionsWhenNoEtd()
        {
            var requestData = GetRequestMockData();
            requestData.EtdMin = null;
            requestData.EtdMax = null;

            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var responseData = await optionsBuilder.PrepareVoyageOptions(requestData);
            var filtered = responseData.Where(e =>
                e.Eta >= requestData.EtaMin!.Value && e.Eta <= requestData.EtaMax!.Value);

            //Assert we return 4x4 etd/ eta array
            Assert.Equal(Math.Pow(requestData.ReturnArrayDimension, 2), filtered.Count());
        }


        [Fact]
        public async Task CanGetVoyageOptions()
        {
            var requestData = GetRequestMockData();
            requestData.EtaMin = null;
            requestData.EtaMax = null;

            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var responseData = await optionsBuilder.PrepareVoyageOptions(requestData);

            var optionsWithData = responseData.Where(
                e => e.IsValid);
            optionsWithData = optionsWithData.ToList().OrderBy(e => e.TotalResistanceEnergyConsumption).ToList();
            var energyConsumptionMin = optionsWithData.First().TotalResistanceEnergyConsumption;

            foreach (var option in optionsWithData)
            {
                var etdInstant = option.Etd;
                var etaInstant = option.Eta;
                var interval = etaInstant - etdInstant;

                //Check duration vs eta/ etd
                Assert.Equal(Math.Round(interval.TotalSeconds, 2), Math.Round(option.DurationInSeconds, 2));

                //Check that power matches energy and duration
                var calcPower = option.TotalResistanceEnergyConsumption / interval.TotalHours;
                Assert.Equal(Math.Round(option.AverageResistancePower!.Value, 1), Math.Round(calcPower!.Value, 1));

                //Check energy consumption relative
                Assert.Equal(Math.Round(100 * ((option.TotalResistanceEnergyConsumption!.Value / energyConsumptionMin!.Value) - 1), 2), Math.Round(option.EnergyConsumptionRelative!.Value, 2));
            }
        }

        [Fact]
        public async Task CanGetMultipleVoyageOptionsWhenNoEta()
        {
            var requestData = GetRequestMockData();
            requestData.EtaMin = null;
            requestData.EtaMax = null;

            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);
            var responseData = await optionsBuilder.PrepareVoyageOptions(requestData);

            // Assert we still return 4x4 etd/ eta array
            var etdAxis = responseData.Select(e => e.Etd).ToList().Distinct();
            var etaAxis = responseData.Select(e => e.Eta).ToList().Distinct();

            Assert.Equal(requestData.ReturnArrayDimension, etdAxis.Count());
            Assert.Equal(requestData.ReturnArrayDimension, etaAxis.Count());
        }

        [Fact]
        public async Task CanGetNoVoyageOptionsWhenSpeedRangeTooStrict()
        {
            var requestData = GetRequestMockData();
            requestData.SpeedMin = 0.0001;
            requestData.SpeedMax = 0.0002;

            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var responseData = await optionsBuilder.PrepareVoyageOptions(requestData);

            //Assert we return 4x4 etd/ eta array
            Assert.Equal(Math.Pow(requestData.ReturnArrayDimension, 2), responseData.Count());

            //Assert we get no voyage options(due to speed limitations)
            Assert.DoesNotContain(responseData, e => e.IsValid);
        }

        [Fact]
        public async Task HandleZeroTimeSpan()
        {
            var requestData = GetRequestMockData();
            requestData.SpeedMin = 1;
            requestData.SpeedMax = 10000000;
            requestData.EtaMax = requestData.EtaMin;
            requestData.EtdMax = requestData.EtdMin;

            //Assert we get 1 option back when no ETA and ETD time span
            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var responseData = await optionsBuilder.PrepareVoyageOptions(requestData);

            Assert.Single(responseData);

            //Assert we get ReturnArrayDimension option back when no ETA time span
            requestData = GetRequestMockData();
            requestData.EtaMin = requestData.EtaMax;
            requestData.EtdMin = null;
            requestData.EtdMax = null;
            responseData = await optionsBuilder.PrepareVoyageOptions(requestData);
            Assert.Equal(requestData.ReturnArrayDimension, responseData.Count());
        }

        // [Fact]
        private async void AllFieldsHaveData()
        {
            var request = GetRequestMockData();
            request.EtaMin = null;
            request.EtaMax = null;
            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var responseData = await optionsBuilder.PrepareVoyageOptions(request);
            var haveValidOptions = false;

            foreach (var option in responseData)
            {
                if (option.IsValid)
                {
                    haveValidOptions = true;
                    Assert.True(option.AverageResistancePower.HasValue);
                    Assert.True(option.TotalResistanceEnergyConsumption.HasValue);
                    Assert.True(option.AbsTotalWindEnergy.HasValue);
                    Assert.True(option.AbsTotalWaveEnergy.HasValue);
                    Assert.True(option.AbsTotalCurrentEnergy.HasValue);
                    Assert.NotNull(option.RouteSegments);
                    Assert.NotEmpty(option.RouteSegments);
                    foreach (var segment in option.RouteSegments)
                    {
                        Assert.True(segment.Course.HasValue);
                        Assert.True(segment.AverageSpeed.HasValue);
                        Assert.True(segment.AvgTotalResistancePower.HasValue);
                        Assert.True(segment.AvgWindResistancePower.HasValue);
                        Assert.True(segment.DurationInSeconds.HasValue);
                        Assert.True(segment.AvgCalmWaterResistancePower.HasValue);

                        Assert.NotNull(segment.TrueWeather);
                        Assert.True(segment.TrueWeather.CurrentFromDirection.HasValue);
                        Assert.True(segment.TrueWeather.CurrentSpeed.HasValue);
                        Assert.True(segment.TrueWeather.WaveFromDirection.HasValue);
                        Assert.True(segment.TrueWeather.WaveHeight.HasValue);
                        Assert.True(segment.TrueWeather.WavePeakPeriod.HasValue);
                        Assert.True(segment.TrueWeather.WindFromDirection.HasValue);
                        Assert.True(segment.TrueWeather.WindSpeed.HasValue);

                        Assert.NotNull(segment.ApparentWeather);
                        Assert.True(segment.ApparentWeather.WindFromDirection.HasValue);
                        Assert.True(segment.ApparentWeather.WindSpeed.HasValue);

                        Assert.True(segment.ApparentWeather.CurrentFromDirection.HasValue);
                        Assert.True(segment.ApparentWeather.CurrentSpeed.HasValue);
                        Assert.True(segment.AvgCurrentResistancePower.HasValue);

                        Assert.True(segment.AvgWindResistancePower.HasValue);
                        Assert.True(segment.AvgSailResistancePower.HasValue);

                        Assert.True(segment.ApparentWeather.WaveFromDirection.HasValue);
                        Assert.True(segment.ApparentWeather.WaveHeight.HasValue);
                        Assert.True(segment.ApparentWeather.WavePeakPeriod.HasValue);
                    }
                }
            }
            Assert.Contains(responseData, e => e.RouteSegments.Any(e => e.AvgWaveResistancePower.HasValue));
            Assert.True(haveValidOptions);
        }

        [Fact]
        public void AddFavorableWeatherIndexToVoyageOptions_WithVariedPowerValues_CalculatesCorrectIndices()
        {
            // Arrange
            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var voyageOptions = new List<VoyageEnergyAdvisorVoyageOption>()
            {
                new VoyageEnergyAdvisorVoyageOption()
                {
                    RouteSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>()
                    {
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment() { AvgCalmWaterResistancePower = 5, AvgTotalResistancePower = 6 }, // diff = 1, index = 0.625
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment() { AvgCalmWaterResistancePower = 5, AvgTotalResistancePower = 7 }, // diff = 2, index = 0.75
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment() { AvgCalmWaterResistancePower = 5, AvgTotalResistancePower = 9 }, // diff = 4, index = 1.0 (max)
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment() { AvgCalmWaterResistancePower = 5, AvgTotalResistancePower = 8 }, // diff = 3, index = 0.875
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment() { AvgCalmWaterResistancePower = 5, AvgTotalResistancePower = 4 }, // diff = -1, index = 0.375
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment() { AvgCalmWaterResistancePower = 5, AvgTotalResistancePower = 1 }, // diff = -4, index = 0.0 (min)
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment() { AvgCalmWaterResistancePower = 5, AvgTotalResistancePower = 2 }, // diff = -3, index = 0.125
                    }
                }
            };

            var expectedIndices = new List<double> { 0.625, 0.75, 1.0, 0.875, 0.375, 0.0, 0.125 };

            // Act
            var result = optionsBuilder.AddFavorableWeatherIndexToVoyageOptions(voyageOptions).ToList();

            // Assert
            var actualIndices = result.First().RouteSegments
                .Select(segment => segment.FavorableWeatherIndex ?? 0.0) // Handle nullable with null-coalescing operator
                .ToList();

            for (int i = 0; i < expectedIndices.Count; i++)
            {
                Assert.Equal(expectedIndices[i], actualIndices[i], precision: 3); // 3 decimal places precision
            }
        }

        [Fact]
        public void AddTotalPowerAndEnergyToVoyageOptions_ComputesCorrectEnergyValues()
        {
            // Arrange
            var mockProgressService = new Mock<IProgressService>();
            var builder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                weatherService: null!,
                calmWaterResistanceService: null!,
                windResistanceService: null!,
                currentResistanceService: null!,
                waveResistanceService: null!,
                sailContributionService: null!,
                fuelConsumptionService: null!,
                costCalculationService: null!,
                progressService: mockProgressService.Object
            ); ;

            var etd = DateTime.UtcNow;

            var routeSegment0 = new VoyageEnergyAdvisorVoyageOptionRouteSegment()
            {
                StartTime = etd,
                EndTime = etd.AddHours(2),
                AvgCalmWaterResistancePower = 50,
                AvgWaveResistancePower = 10,
                AvgCurrentResistancePower = 5,
                AvgWindResistancePower = 20,
                AvgSailResistancePower = -45,
                AvgTotalResistancePower = 40,
            };

            var routeSegment1 = new VoyageEnergyAdvisorVoyageOptionRouteSegment()
            {
                StartTime = etd.AddHours(2),
                EndTime = etd.AddHours(4),
                AvgCalmWaterResistancePower = 50,
                AvgWaveResistancePower = 10,
                AvgCurrentResistancePower = 5,
                AvgWindResistancePower = 20,
                AvgSailResistancePower = 5,
                AvgTotalResistancePower = 90,
            };

            var voyageOption = new VoyageEnergyAdvisorVoyageOption
            {
                IsValid = true,
                Etd = routeSegment0.StartTime,
                Eta = routeSegment1.EndTime,
                RouteSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment> { routeSegment0, routeSegment1 }
            };

            var inputOptions = new List<VoyageEnergyAdvisorVoyageOption> { voyageOption };

            // Act
            var result = builder.AddTotalPowerAndEnergyToVoyageOptions(inputOptions).ToList();

            // Assert
            var output = result.First();

            Assert.Equal(260, output.TotalResistanceEnergyConsumption);
            Assert.Equal(200, output.TotalCalmWaterResistanceEnergyConsumption);

            Assert.Equal(80, output.AbsTotalWindEnergy);
            Assert.Equal(40, output.RelativeWindEnergyConsumption);

            Assert.Equal(40, output.AbsTotalWaveEnergy);
            Assert.Equal(20, output.RelativeWaveEnergyConsumption);

            Assert.Equal(20, output.AbsTotalCurrentEnergy);
            Assert.Equal(10, output.RelativeCurrentEnergyConsumption);

            Assert.Equal(80, output.AbsTotalSailEnergy);
            Assert.Equal(-40, output.RelativeSailEnergyConsumption);

            Assert.Equal(65, output.AverageResistancePower);
        }

        [Fact]
        public void AddFavorableWeatherIndexToVoyageOptions_WithNoPowerDeviation_SetsDefaultIndex()
        {
            // Arrange
            var optionsBuilder = new VoyageEnergyAdvisorVoyageOptionsBuilder(
                _weatherService.Object,
                _calmWaterResistanceService.Object,
                _windResistanceService.Object,
                _currentResistanceService.Object,
                _waveResistanceService.Object,
                _sailContributionService.Object,
                _fuelConsumptionService.Object,
                _costCalculationService.Object,
                _progressService.Object);

            var voyageOptions = new List<VoyageEnergyAdvisorVoyageOption>()
        {
        new VoyageEnergyAdvisorVoyageOption()
        {
            RouteSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>()
            {
                new VoyageEnergyAdvisorVoyageOptionRouteSegment() { AvgCalmWaterResistancePower = 10, AvgTotalResistancePower = 10 },
                new VoyageEnergyAdvisorVoyageOptionRouteSegment() { AvgCalmWaterResistancePower = 15, AvgTotalResistancePower = 15 },
                new VoyageEnergyAdvisorVoyageOptionRouteSegment() { AvgCalmWaterResistancePower = 20, AvgTotalResistancePower = 20 },
            }
        }
    };

            // Act
            var result = optionsBuilder.AddFavorableWeatherIndexToVoyageOptions(voyageOptions).ToList();

            // Assert
            foreach (var segment in result.First().RouteSegments)
            {
                Assert.Equal(0.5, segment.FavorableWeatherIndex ?? 0.0, precision: 3); // Handle nullable with null-coalescing operator
            }
        }


        private static IList<VoyageEnergyAdvisorVoyageOption> GetVoyageOptionsWithInvalidOption(DateTime validInstant, double latitude, double longitude)
        {
            return new[]
            {
                    GetVoyageOption(validInstant, latitude, longitude),
                    GetVoyageOption(DateTimeOffset.FromUnixTimeMilliseconds(0).DateTime, latitude, longitude)
                };
        }

        private static VoyageEnergyAdvisorVoyageOption GetVoyageOption(DateTime instant, double latitude, double longitude)
        {
            return new VoyageEnergyAdvisorVoyageOption
            {
                Etd = instant,
                Eta = instant + TimeSpan.FromHours(1),
                RouteSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>
                    {
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment
                        {
                            StartTime = instant,
                            StartPosition = new GeoCoordinate(latitude, longitude)
                        }
                    }
            };
        }

        private static IList<WeatherResponseInstance> GetWeatherData(DateTime instant, double latitude, double longitude)
        {
            return new[]
            {
                    new WeatherResponseInstance
                    {
                        Time = instant,
                        Location = new GeoCoordinate(latitude, longitude),
                        Weather = new WeatherData(1, 1, 1, 1, 1, 1, 1)
                    }
                };
        }

        private static VoyageEnergyAdvisorRequest GetRequestMockData()
        {
            var etdMin = DateTime.UtcNow.AddHours(1); 

            return new VoyageEnergyAdvisorRequest()
            {
                EtdMin = etdMin,
                EtdMax = etdMin + TimeSpan.FromHours(4),
                EtaMin = etdMin + TimeSpan.FromHours(10),
                EtaMax = etdMin + TimeSpan.FromHours(14),
                SpeedMin = 1,
                SpeedMax = 1000,
                ReturnArrayDimension = 4,

                Route = new Route()
                {
                    Waypoints = new List<GeoCoordinate>()
            {
                new GeoCoordinate(45,150),
                new GeoCoordinate(0, 0),
                new GeoCoordinate(-45, -150),
                new GeoCoordinate(0, -180),
                new GeoCoordinate(45, 150)
            }
                }
            };
        }

        private static IEnumerable<VoyageEnergyAdvisorVoyageOption> GetVoyageOptionsMockData()
        {
            return new List<VoyageEnergyAdvisorVoyageOption>()
            {
                new VoyageEnergyAdvisorVoyageOption()
                {
                    Etd = DateTimeOffset.FromUnixTimeSeconds(0).DateTime,
                    Eta = DateTimeOffset.FromUnixTimeSeconds(4 * 3600).DateTime,
                    IsValid = true,
                    RouteSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>()
                    {
                       new VoyageEnergyAdvisorVoyageOptionRouteSegment()
                       {
                            StartTime = DateTimeOffset.FromUnixTimeSeconds(0).DateTime,
                            EndTime = DateTimeOffset.FromUnixTimeSeconds(2 * 3600).DateTime,
                            AverageSpeed = 10.0,
                            StartPosition = new GeoCoordinate(0, 0),
                            EndPosition = new GeoCoordinate(90, 0),
                            AvgCalmWaterResistancePower = 100,
                            AvgWindResistancePower = -2,
                            AvgCurrentResistancePower = -3,
                            AvgWaveResistancePower = -4,
                            AvgSailResistancePower = -5
                        },
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment()
                        {
                          StartTime = DateTimeOffset.FromUnixTimeSeconds(2 * 3600).DateTime,
                            EndTime = DateTimeOffset.FromUnixTimeSeconds(4 * 3600).DateTime,
                            AverageSpeed = 12.0,
                            StartPosition = new GeoCoordinate(0, 0),
                            EndPosition = new GeoCoordinate(90, 0),
                            AvgCalmWaterResistancePower = 200,
                            AvgWindResistancePower = -7,
                            AvgCurrentResistancePower = -8,
                            AvgWaveResistancePower = -9,
                            AvgSailResistancePower = -10
                        }
                    }
                },
                new VoyageEnergyAdvisorVoyageOption()
                {
                    Etd = DateTimeOffset.FromUnixTimeSeconds(0).DateTime,
                    Eta = DateTimeOffset.FromUnixTimeSeconds(4 * 3600).DateTime,
                    IsValid = true,
                    RouteSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>()
                    {
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment()
                        {
                          StartTime = DateTimeOffset.FromUnixTimeSeconds(0).DateTime,
                            EndTime = DateTimeOffset.FromUnixTimeSeconds(2 * 3600).DateTime,
                            AverageSpeed = 30.0,
                            StartPosition = new GeoCoordinate(0, 0),
                            EndPosition = new GeoCoordinate(90, 0),
                            AvgCalmWaterResistancePower = 11,
                            AvgWindResistancePower = 12,
                            AvgCurrentResistancePower = 13,
                            AvgWaveResistancePower = 14,
                            AvgSailResistancePower = 15
                        },
                        new VoyageEnergyAdvisorVoyageOptionRouteSegment()
                        {
                            StartTime = DateTimeOffset.FromUnixTimeSeconds(2 * 3600).DateTime,
                            EndTime = DateTimeOffset.FromUnixTimeSeconds(4 * 3600).DateTime,
                            AverageSpeed = 12.0,
                            StartPosition = new GeoCoordinate(0, 0),
                            EndPosition = new GeoCoordinate(90, 0),
                            AvgCalmWaterResistancePower = 16,
                            AvgWindResistancePower = 17,
                            AvgCurrentResistancePower = 18,
                            AvgWaveResistancePower = 19,
                            AvgSailResistancePower = 20
                        }
                    }
                }
            }.ToList();
        }

        private static IEnumerable<WeatherResponseInstance> GetWeatherMockData(
            IEnumerable<WeatherRequestInstance> request)
        {
            return request.Select(e =>
            {
                // For mock weather data is sum of time, latitude and longitude
                var returnWeatherValue = e.Location.Latitude + e.Location.Longitude;
                return new WeatherResponseInstance()
                {
                    Time = e.Time,
                    Location = e.Location,
                    Weather = new WeatherData()
                    {
                        CurrentFromDirection = returnWeatherValue,
                        CurrentSpeed = returnWeatherValue,
                        WaveFromDirection = returnWeatherValue,
                        WaveHeight = returnWeatherValue,
                        WindFromDirection = returnWeatherValue,
                        WindSpeed = returnWeatherValue,
                        WavePeakPeriod = returnWeatherValue
                    }
                };
            });
        }
    }
}

