using System.Globalization;
using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Models.VoyageEnergyAdvisor;
using VoyageEnergyAdvisor.Core.Services.CalmWaterResistanceService;
using VoyageEnergyAdvisor.Core.Services.CostCalculationService;
using VoyageEnergyAdvisor.Core.Services.CurrentResistanceService;
using VoyageEnergyAdvisor.Core.Services.FuelConsumptionService;
using VoyageEnergyAdvisor.Core.Services.ProgressService;
using VoyageEnergyAdvisor.Core.Services.SailContributionService;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Exceptions;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Helpers;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyCalculatorService.Helpers;
using VoyageEnergyAdvisor.Core.Services.WaveResistanceService;
using VoyageEnergyAdvisor.Core.Services.WeatherService;
using VoyageEnergyAdvisor.Core.Services.WindResistanceService;
using WeatherData = VoyageEnergyAdvisor.Core.CommonModels.WeatherData;

namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService
{
    public class VoyageEnergyAdvisorVoyageOptionsBuilder(
        IWeatherService weatherService,
        ICalmWaterResistanceService calmWaterResistanceService,
        IWindResistanceService windResistanceService,
        ICurrentResistanceService currentResistanceService,
        IWaveResistanceService waveResistanceService,
        ISailContributionService sailContributionService,
        IFuelConsumptionService fuelConsumptionService,
        ICostCalculationService costCalculationService,
        IProgressService progressService) : IVoyageEnergyAdvisorVoyageOptionsBuilder
    {

        private const double MinSpeed = 0.51444;

        // Optimal voyage (constant propulsion power) search bounds.
        private const int MaxPowerIterations = 12;
        private const int MaxSpeedIterationsPerSegment = 12;
        private const double SpeedTolerance = 0.01; // m/s
        private const double TimeTolerance = 1.0; // seconds
        private const double PowerBalanceTolerance = 1.0; // Watts

        // Optimal voyage power/speed search band: the outer power search and the per-segment speed search
        // are bounded to +-80% of the required average speed, rather than the request's SpeedMin/SpeedMax.
        private const double AverageSpeedSearchBandFraction = 0.8;

        public async Task<IEnumerable<VoyageEnergyAdvisorVoyageOption>> PrepareVoyageOptions(
            VoyageEnergyAdvisorRequest request)
        {
        
            var voyageOptions = GetVoyageOptionsArray(request);
            voyageOptions = FilterOnSpeed(voyageOptions, request.SpeedMin, request.SpeedMax).ToList();
            var voyageOptionsList = voyageOptions.ToList();
            var invalidOptions = voyageOptionsList.Where(e => !e.IsValid);
            var validOptions = voyageOptionsList.Where(e => e.IsValid);
            validOptions = await PopulateVoyageOptions(validOptions, request.Route);
            return validOptions.Concat(invalidOptions);
        }

        public async Task<IEnumerable<VoyageEnergyAdvisorVoyageOption>> PopulateVoyageOptions(IEnumerable<VoyageEnergyAdvisorVoyageOption> validOptions, Route route)
        {
            validOptions = AddRouteSegments(validOptions, route);
            validOptions = AddTimeToRouteSegments(validOptions);
            validOptions = AddCourseToRouteSegments(validOptions);
            validOptions = await AddTrueWeatherToRouteSegments(validOptions);
            validOptions = AddApparentWeatherToRouteSegments(validOptions);
            validOptions = AddCalmWaterPowerToRouteSegments(validOptions);
            validOptions = AddWindPowerToRouteSegments(validOptions);
            validOptions = AddWavePowerToRouteSegments(validOptions);
            validOptions = AddCurrentPowerToRouteSegments(validOptions);
            validOptions = AddSailPowerToRouteSegments(validOptions);
            validOptions = AddTotalPowerToRouteSegments(validOptions);
            validOptions = AddFuelConsumptionToRouteSegments(validOptions);
            validOptions = AddCostToRouteSegments(validOptions);
            validOptions = AddTotalPowerAndEnergyToVoyageOptions(validOptions);
            validOptions = AddTotalFuelConsumptionToVoyageOptions(validOptions);
            validOptions = AddTotalCostToVoyageOptions(validOptions);
            validOptions = AddFavorableWeatherIndexToVoyageOptions(validOptions);
            return validOptions;
        }

        public VoyageEnergyAdvisorRequest? ToValidRequest(VoyageEnergyAdvisorRequest request)
        {
            if (request.ReturnArrayDimension < 1)
            {
                return null;
            }
            request.SpeedMax = request.SpeedMax <= 0 ? 1 : request.SpeedMax;
            request.SpeedMin = request.SpeedMin <= 0 ? MinSpeed : request.SpeedMin;
            return request.SpeedMax < request.SpeedMin ? null : request;
        }

        public IEnumerable<VoyageEnergyAdvisorVoyageOption> GetVoyageOptionsArray(VoyageEnergyAdvisorRequest request)
        {
            double voyageDistance = request.Route.GetVoyageDistance();
            var minTravelDuration = TimeSpan.FromSeconds(voyageDistance / request.SpeedMax);
            var maxTravelDuration = TimeSpan.FromSeconds(voyageDistance / request.SpeedMin);

            var currentUtcTime = DateTime.UtcNow;

            // Has ETA, but not ETD:
            if ((!request.EtdMin.HasValue ||
                 !request.EtdMax.HasValue) &&
                (request.EtaMin.HasValue ||
                 request.EtaMax.HasValue))
            {
                request.EtdMin = request.EtaMin.GetValueOrDefault() - maxTravelDuration;
                request.EtdMax = request.EtaMax
                    .GetValueOrDefault() - minTravelDuration;
                request.TimeSelectionMode = TimeSelectionMode.ETA;
            }
            else // Has ETD, but not ETA:
            if ((request.EtdMin.HasValue ||
                 request.EtdMax.HasValue) &&
                (!request.EtaMin.HasValue ||
                 !request.EtaMax.HasValue))
            {
                request.EtaMax = request.EtdMax
                    .GetValueOrDefault() + maxTravelDuration;
                request.EtaMin = request.EtdMin
                    .GetValueOrDefault() + minTravelDuration;
                request.TimeSelectionMode = TimeSelectionMode.ETD;
            }

            var etdOptions = GetTimeOptions(request.EtdMin, request.EtdMax, request.ReturnArrayDimension);
            var etaOptions = GetTimeOptions(request.EtaMin, request.EtaMax, request.ReturnArrayDimension);

            foreach (var etd in etdOptions)
            {
                foreach (var eta in etaOptions)
                {
                    // Check if option is valid: ETD must be before ETA, ETD must not be in the past, and ETA must not be in the past
                    var optionValid =
                           etd < eta &&
                           etd >= currentUtcTime &&
                           eta >= currentUtcTime; 

                    var newVoyageOption = new VoyageEnergyAdvisorVoyageOption()
                    {
                        Eta = eta,
                        Etd = etd,
                        AverageSpeed = optionValid ? voyageDistance / (eta - etd).TotalSeconds : 0,
                        DurationInSeconds = optionValid ? (eta - etd).TotalSeconds : 0,
                        IsValid = optionValid
                    };
                    yield return newVoyageOption;
                }
            }
        }

        public IEnumerable<VoyageEnergyAdvisorVoyageOption> FilterOnSpeed(IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions, double minSpeed, double maxSpeed)
        {
            if (voyageOptions == null)
            {
                throw new ArgumentNullException(nameof(voyageOptions));
            }

            return voyageOptions.Select(e =>
            {
                if (e == null)
                {
                    throw new ArgumentNullException(nameof(e));
                }

                var margin = 0.1;
                var averageSpeedRounded = Math.Round(e.AverageSpeed, 1);
                e.IsValid &= averageSpeedRounded + margin >= Math.Round(minSpeed, 1) && averageSpeedRounded - margin <= Math.Round(maxSpeed, 1);
                return e;
            });
        }

        public IEnumerable<VoyageEnergyAdvisorVoyageOption> AddRouteSegments(IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions, Route route)
        {
            return voyageOptions.Select(e =>
            {
                var splittedRoute = route.SplitToSegments(10000.0); // TODO max 10 km long segments
                var routeSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>();
                for (int i = 1; i < splittedRoute.Waypoints.Count(); i++)
                {
                    var startPos = splittedRoute.Waypoints[i - 1];
                    var endPos = splittedRoute.Waypoints[i];

                    routeSegments.Add(new VoyageEnergyAdvisorVoyageOptionRouteSegment()
                    {
                        StartPosition = new GeoCoordinate(startPos.Latitude, startPos.Longitude),
                        EndPosition = new GeoCoordinate(endPos.Latitude, endPos.Longitude),
                        AverageSpeed = e.AverageSpeed
                    });
                }
                e.RouteSegments = routeSegments;
                return e;
            });
        }

        public IEnumerable<VoyageEnergyAdvisorVoyageOption> AddCostToRouteSegments(
                IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions)
        {
            var fuelPricePerKg = costCalculationService.GetFuelPricePerKg();

            return voyageOptions.Select(voyageOption =>
            {
                voyageOption.RouteSegments = voyageOption.RouteSegments.Select(routeSegment =>
                {
                    if (routeSegment.AvgCalmWaterResistanceFuelConsumption.HasValue)
                    {
                        routeSegment.AvgCalmWaterResistanceCost =
                            routeSegment.AvgCalmWaterResistanceFuelConsumption.Value * fuelPricePerKg;
                    }

                    if (routeSegment.AvgWindResistanceFuelConsumption.HasValue)
                    {
                        routeSegment.AvgWindResistanceCost =
                            routeSegment.AvgWindResistanceFuelConsumption.Value * fuelPricePerKg;
                    }

                    if (routeSegment.AvgWaveResistanceFuelConsumption.HasValue)
                    {
                        routeSegment.AvgWaveResistanceCost =
                            routeSegment.AvgWaveResistanceFuelConsumption.Value * fuelPricePerKg;
                    }

                    if (routeSegment.AvgCurrentResistanceFuelConsumption.HasValue)
                    {
                        routeSegment.AvgCurrentResistanceCost =
                            routeSegment.AvgCurrentResistanceFuelConsumption.Value * fuelPricePerKg;
                    }

                    if (routeSegment.AvgSailResistanceFuelConsumption.HasValue)
                    {
                        routeSegment.AvgSailResistanceCost =
                            routeSegment.AvgSailResistanceFuelConsumption.Value * fuelPricePerKg;
                    }

                    if (routeSegment.AvgTotalResistanceFuelConsumption.HasValue)
                    {
                        routeSegment.AvgTotalResistanceCost =
                            routeSegment.AvgTotalResistanceFuelConsumption.Value * fuelPricePerKg;
                    }

                    if (routeSegment.AvgNetWeatherResistanceFuelConsumption.HasValue)
                    {
                        routeSegment.AvgNetWeatherResistanceCost =
                            routeSegment.AvgNetWeatherResistanceFuelConsumption.Value * fuelPricePerKg;
                    }

                    return routeSegment;
                }).ToList();

                return voyageOption;
            });
        }

        public IEnumerable<VoyageEnergyAdvisorVoyageOption> AddTotalCostToVoyageOptions(
           IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions
       )
        {
            var voyageOptionsList = voyageOptions.ToList();
            int total = voyageOptionsList.Count;
            int processed = 0;

            double startPercent = 97;
            double endPercent = 100;

            var optionsWithTotalCost = voyageOptions.Select(voyageOption =>
            {
                if (voyageOption.IsValid)
                {
                    double totalOptionResistanceCost = 0;
                    double totalOptionWindCost = 0;
                    double totalOptionWaveCost = 0;
                    double totalOptionCurrentCost = 0;
                    double totalOptionSailCost = 0;
                    double totalOptionCalmWaterResistanceCost = 0;

                    foreach (var routeSegment in voyageOption.RouteSegments)
                    {
                        var segmentDurationHour = (routeSegment.EndTime - routeSegment.StartTime).TotalHours;

                        totalOptionResistanceCost += routeSegment.AvgTotalResistanceCost.GetValueOrDefault() * segmentDurationHour;
                        totalOptionWindCost += routeSegment.AvgWindResistanceCost.GetValueOrDefault() * segmentDurationHour;
                        totalOptionWaveCost += routeSegment.AvgWaveResistanceCost.GetValueOrDefault() * segmentDurationHour;
                        totalOptionCurrentCost += routeSegment.AvgCurrentResistanceCost.GetValueOrDefault() * segmentDurationHour;
                        totalOptionSailCost += routeSegment.AvgSailResistanceCost.GetValueOrDefault() * segmentDurationHour;
                        totalOptionCalmWaterResistanceCost += routeSegment.AvgCalmWaterResistanceCost.GetValueOrDefault() * segmentDurationHour;
                    }

                    voyageOption.TotalResistanceCost = totalOptionResistanceCost;
                    voyageOption.TotalCalmWaterResistanceCost = totalOptionCalmWaterResistanceCost;

                    // Relative numbers
                    voyageOption.RelativeWindCost = 100 * totalOptionWindCost / totalOptionCalmWaterResistanceCost;
                    voyageOption.AbsTotalWindCost = Math.Abs(totalOptionWindCost);

                    voyageOption.RelativeWaveCost = 100 * totalOptionWaveCost / totalOptionCalmWaterResistanceCost;
                    voyageOption.AbsTotalWaveCost = Math.Abs(totalOptionWaveCost);

                    voyageOption.RelativeCurrentCost = 100 * totalOptionCurrentCost / totalOptionCalmWaterResistanceCost;
                    voyageOption.AbsTotalCurrentCost = Math.Abs(totalOptionCurrentCost);

                    voyageOption.RelativeSailCost = 100 * totalOptionSailCost / totalOptionCalmWaterResistanceCost;
                    voyageOption.AbsTotalSailCost = Math.Abs(totalOptionSailCost);

                    voyageOption.AverageCostRate = totalOptionResistanceCost /
                        (voyageOption.Eta - voyageOption.Etd).TotalHours;
                }

                processed++;
                progressService.UpdateProgressDynamic(processed, total, startPercent, endPercent, "Calculating cost")
                    .ConfigureAwait(false);

                return voyageOption;
            }).ToList();

            var validOptions = optionsWithTotalCost.Where(e => e.IsValid);
            double? minCost = validOptions.Any()
                ? validOptions.OrderBy(e => e.TotalResistanceCost.GetValueOrDefault())
                              .First().TotalResistanceCost
                : null;

            return optionsWithTotalCost.Select(e =>
            {
                e.CostRelative = e.TotalResistanceCost.HasValue && minCost.HasValue && minCost > 0
                    ? 100 * (e.TotalResistanceCost.Value / minCost.Value - 1)
                    : null;
                return e;
            }).ToList();
        }


        public IEnumerable<VoyageEnergyAdvisorVoyageOption> AddTimeToRouteSegments(IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions)
        {
            if (voyageOptions == null) throw new ArgumentNullException(nameof(voyageOptions));
            return voyageOptions.Select(voyageOption =>
            {
                if (voyageOption.RouteSegments == null) throw new ArgumentNullException(nameof(voyageOption.RouteSegments));

                voyageOption.RouteSegments = voyageOption.RouteSegments.Select((routeSegment, i) =>
                {
                    if (routeSegment.AverageSpeed > 0)
                    {
                        var segmentDistance = (routeSegment.StartPosition != null && routeSegment.EndPosition != null)
                            ? routeSegment.StartPosition.GetDistanceTo(routeSegment.EndPosition)
                            : 0;
                        var segmentDuration = TimeSpan.FromSeconds(segmentDistance / routeSegment.AverageSpeed.GetValueOrDefault());
                        routeSegment.StartTime = i == 0 ? voyageOption.Etd : voyageOption.RouteSegments[i - 1]?.EndTime ?? DateTime.MinValue;
                        routeSegment.EndTime = routeSegment.StartTime + segmentDuration;
                        routeSegment.DurationInSeconds = segmentDuration.TotalSeconds;
                    }
                    return routeSegment;
                }).ToList();
                return voyageOption;
            });
        }


        public IEnumerable<VoyageEnergyAdvisorVoyageOption> AddCourseToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions)
        {
            if (voyageOptions == null) throw new ArgumentNullException(nameof(voyageOptions));

            return voyageOptions.Select(voyageOption =>
            {
                if (voyageOption.RouteSegments == null) throw new ArgumentNullException(nameof(voyageOption.RouteSegments));

                voyageOption.RouteSegments = voyageOption.RouteSegments.Select(routeSegment =>
                {
                    if (routeSegment.StartPosition != null && routeSegment.EndPosition != null)
                    {
                        routeSegment.Course = routeSegment.StartPosition.GetCourse(routeSegment.EndPosition);
                    }
                    else
                    {
                        routeSegment.Course = null; // or some default value if applicable
                    }
                    return routeSegment;
                }).ToList();
                return voyageOption;
            });
        }

        public async Task<IEnumerable<VoyageEnergyAdvisorVoyageOption>> AddTrueWeatherToRouteSegments(
        IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions)
        {
            if (voyageOptions == null) throw new ArgumentNullException(nameof(voyageOptions));

            var voyageOptionsList = voyageOptions.ToList();

            var weatherRequest = voyageOptionsList
                .SelectMany(voyageOption => voyageOption.RouteSegments?
                    .Where(segment => segment.StartPosition?.Latitude != null && segment?.StartPosition?.Longitude != null)
                    .Select(segment => new WeatherRequestInstance
                    {
                        Time = segment.StartTime,

                        Location = new GeoCoordinate(segment.StartPosition!.Latitude, segment.StartPosition.Longitude),
                        IsLiveMode = voyageOption.IsLiveMode
                    }) ?? Enumerable.Empty<WeatherRequestInstance>())
                .GroupBy(e => new { e.Time, e.Location })
                .Select(g => g.First())

                .ToList();


            var allWeatherData = (await weatherService.GetWeather(
                        weatherRequest,
                        async (progress, message) => await progressService.UpdateProgress(progress, message)))
                .ToList()
                .ToLookup(e => GetLookupKey(
                    e.Time,
                    e.Location.Latitude,
                    e.Location.Longitude));

            return voyageOptionsList.Select(voyageOption =>
            {
                if (voyageOption.RouteSegments == null) throw new ArgumentNullException(nameof(voyageOption.RouteSegments));

                voyageOption.RouteSegments = voyageOption.RouteSegments.Select(routeSegment =>
               {
                   if (routeSegment.StartPosition != null)
                   {
                       var lookupKey = GetLookupKey(
                           routeSegment.StartTime,
                           routeSegment.StartPosition.Latitude,
                           routeSegment.StartPosition.Longitude
                           );

                       var trueWeatherLookup = allWeatherData[lookupKey];
                       var trueWeatherInstance = trueWeatherLookup.FirstOrDefault();
                       if (trueWeatherInstance != null)
                       {
                           routeSegment.TrueWeather = trueWeatherInstance.Weather;
                       }
                       else
                       {
                           var test = routeSegment;
                       }
                   }
                   return routeSegment;
               }).ToList();
                return voyageOption;
            });
        }


        private static (DateTime Time, double Lat, double Lon) GetLookupKey(DateTime time, double latitude, double longitude)
        {
            var decimalsRounding = 6;

            
            var normalizedTime = new DateTime(
                time.Year, time.Month, time.Day,
                time.Hour, time.Minute, time.Second,
                DateTimeKind.Utc);

            return (normalizedTime, Math.Round(latitude, decimalsRounding), Math.Round(longitude, decimalsRounding));
        }

        public IEnumerable<VoyageEnergyAdvisorVoyageOption> AddApparentWeatherToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions)
        {
            return voyageOptions.Select(voyageOption =>
            {
                voyageOption.RouteSegments = voyageOption.RouteSegments.Select(routeSegment =>
                {
                    if (routeSegment.TrueWeather != null)
                    {
                        var apparentWindSpeed = VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindSpeed(
                            routeSegment.TrueWeather.WindSpeed.GetValueOrDefault(),
                            routeSegment.TrueWeather.WindFromDirection.GetValueOrDefault(),
                            routeSegment.Course.GetValueOrDefault(),
                            routeSegment.AverageSpeed.GetValueOrDefault()
                        );
                        var relativeWindDirection =
                            VoyageEnergyAdvisorApparentWeatherHelper.GetApparentWindFromDirection(
                                routeSegment.TrueWeather.WindSpeed.GetValueOrDefault(),
                                routeSegment.TrueWeather.WindFromDirection.GetValueOrDefault(),
                                routeSegment.Course.GetValueOrDefault(),
                                routeSegment.AverageSpeed.GetValueOrDefault()
                            );
                        var relativeCurrentSpeed = VoyageEnergyAdvisorApparentWeatherHelper.GetRelativeCurrentSpeed(
                            routeSegment.TrueWeather.CurrentSpeed.GetValueOrDefault(),
                            routeSegment.TrueWeather.CurrentFromDirection.GetValueOrDefault(),
                            routeSegment.Course.GetValueOrDefault(),
                            routeSegment.AverageSpeed.GetValueOrDefault()
                        );
                        var relativeCurrentDirection =
                            VoyageEnergyAdvisorApparentWeatherHelper.GetRelativeCurrentFromDirection(
                                routeSegment.TrueWeather.CurrentSpeed.GetValueOrDefault(),
                                routeSegment.TrueWeather.CurrentFromDirection.GetValueOrDefault(),
                                routeSegment.Course.GetValueOrDefault(),
                                routeSegment.AverageSpeed.GetValueOrDefault()
                            );

                        var relativeWaveDirection = VoyageEnergyAdvisorApparentWeatherHelper.GetRelativeWaveFromDirection(
                                routeSegment.TrueWeather.WaveFromDirection.GetValueOrDefault(),
                                routeSegment.Course.GetValueOrDefault());

                        routeSegment.ApparentWeather = new WeatherData()
                        {
                            WindSpeed = apparentWindSpeed,
                            WindFromDirection = relativeWindDirection,
                            CurrentSpeed = relativeCurrentSpeed,
                            CurrentFromDirection = relativeCurrentDirection,
                            WaveHeight = routeSegment.TrueWeather.WaveHeight,
                            WavePeakPeriod = routeSegment.TrueWeather.WavePeakPeriod,
                            WaveFromDirection = relativeWaveDirection
                        };
                    }
                    return routeSegment;
                }).ToList();
                return voyageOption;
            });
        }

        public IEnumerable<VoyageEnergyAdvisorVoyageOption> AddCalmWaterPowerToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions)
        {
            return voyageOptions.Select(voyageOption =>
            {
                voyageOption.RouteSegments = voyageOption.RouteSegments.Select(routeSegment =>
                {
                    routeSegment.AvgCalmWaterResistancePower = calmWaterResistanceService.GetCalmWaterResistancePower(routeSegment.AverageSpeed.GetValueOrDefault());
                    return routeSegment;
                }).ToList();
                return voyageOption;
            });
        }

        public IEnumerable<VoyageEnergyAdvisorVoyageOption> AddFuelConsumptionToRouteSegments(
                 IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions)
        {
            return voyageOptions.Select(voyageOption =>
            {
                voyageOption.RouteSegments = voyageOption.RouteSegments.Select(routeSegment =>
                {
                    if (routeSegment.AvgCalmWaterResistancePower.HasValue)
                    {
                        routeSegment.AvgCalmWaterResistanceFuelConsumption =
                            fuelConsumptionService.GetFuelConsumption(routeSegment.AvgCalmWaterResistancePower.Value);
                    }

                    if (routeSegment.AvgWindResistancePower.HasValue)
                    {
                        routeSegment.AvgWindResistanceFuelConsumption =
                            fuelConsumptionService.GetFuelConsumption(routeSegment.AvgWindResistancePower.Value);
                    }

                    if (routeSegment.AvgWaveResistancePower.HasValue)
                    {
                        routeSegment.AvgWaveResistanceFuelConsumption =
                            fuelConsumptionService.GetFuelConsumption(routeSegment.AvgWaveResistancePower.Value);
                    }

                    if (routeSegment.AvgCurrentResistancePower.HasValue)
                    {
                        routeSegment.AvgCurrentResistanceFuelConsumption =
                            fuelConsumptionService.GetFuelConsumption(routeSegment.AvgCurrentResistancePower.Value);
                    }

                    if (routeSegment.AvgSailResistancePower.HasValue)
                    {
                        routeSegment.AvgSailResistanceFuelConsumption =
                            fuelConsumptionService.GetFuelConsumption(routeSegment.AvgSailResistancePower.Value);
                    }

                    if (routeSegment.AvgTotalResistancePower.HasValue)
                    {
                        routeSegment.AvgTotalResistanceFuelConsumption =
                            fuelConsumptionService.GetFuelConsumption(routeSegment.AvgTotalResistancePower.Value);
                    }

                    if (routeSegment.AvgNetWeatherResistancePower.HasValue)
                    {
                        routeSegment.AvgNetWeatherResistanceFuelConsumption =
                            fuelConsumptionService.GetFuelConsumption(routeSegment.AvgNetWeatherResistancePower.Value);
                    }

                    return routeSegment;
                }).ToList();

                return voyageOption;
            });
        }

        public IEnumerable<VoyageEnergyAdvisorVoyageOption> AddWindPowerToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions)
        {
            return voyageOptions.Select(voyageOption =>
            {
                voyageOption.RouteSegments = voyageOption.RouteSegments.Select(routeSegment =>
                {
                    if (routeSegment.ApparentWeather != null)
                    {
                        var totalWindResistancePower = windResistanceService.GetWindResistancePower(
                            routeSegment.ApparentWeather.WindSpeed.GetValueOrDefault(),
                            routeSegment.ApparentWeather.WindFromDirection.GetValueOrDefault(),
                            routeSegment.AverageSpeed.GetValueOrDefault());
                        var calmWaterWindResistancePower = windResistanceService.GetWindResistancePower(
                            routeSegment.AverageSpeed.GetValueOrDefault(),
                            0,
                            routeSegment.AverageSpeed.GetValueOrDefault());
                        routeSegment.AvgWindResistancePower = totalWindResistancePower - calmWaterWindResistancePower;
                    }
                    return routeSegment;
                }).ToList();
                return voyageOption;
            });
        }

        public IEnumerable<VoyageEnergyAdvisorVoyageOption> AddCurrentPowerToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions)
        {
            return voyageOptions.Select(voyageOption =>
            {
                voyageOption.RouteSegments = voyageOption.RouteSegments.Select(routeSegment =>
                {
                    if (routeSegment.ApparentWeather != null)
                    {
                        routeSegment.AvgCurrentResistancePower = currentResistanceService.GetCurrentResistancePower(
                            routeSegment.ApparentWeather.CurrentSpeed.GetValueOrDefault(),
                            routeSegment.ApparentWeather.CurrentFromDirection.GetValueOrDefault(),
                            routeSegment.AverageSpeed.GetValueOrDefault());
                    }
                    return routeSegment;
                }).ToList();
                return voyageOption;
            });
        }

        public IEnumerable<VoyageEnergyAdvisorVoyageOption> AddSailPowerToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions)
        {
            return voyageOptions.Select(voyageOption =>
            {
                voyageOption.RouteSegments = voyageOption.RouteSegments.Select(routeSegment =>
                {
                    if (routeSegment.ApparentWeather != null)
                    {
                        // Note: Use minus here to change from "contribution" to "resistance"
                        routeSegment.AvgSailResistancePower = -sailContributionService.GetSailContributionPower
                        (
                            routeSegment.ApparentWeather.WindSpeed.GetValueOrDefault(),
                            routeSegment.ApparentWeather.WindFromDirection.GetValueOrDefault(),
                            routeSegment.AverageSpeed.GetValueOrDefault()
                         );
                    }
                    return routeSegment;
                }).ToList();
                return voyageOption;
            });
        }



        public IEnumerable<VoyageEnergyAdvisorVoyageOption> AddWavePowerToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions)
        {
            return voyageOptions.Select(voyageOption =>
            {
                voyageOption.RouteSegments = voyageOption.RouteSegments.Select(routeSegment =>
                {
                    if (routeSegment.ApparentWeather != null)
                    {
                        routeSegment.AvgWaveResistancePower = waveResistanceService.GetWaveResistancePower(
                            routeSegment.ApparentWeather.WavePeakPeriod.GetValueOrDefault(),
                            routeSegment.ApparentWeather.WaveHeight.GetValueOrDefault(),
                            routeSegment.ApparentWeather.WaveFromDirection.GetValueOrDefault(),
                            routeSegment.AverageSpeed.GetValueOrDefault());
                    }
                    return routeSegment;
                }).ToList();
                return voyageOption;
            });
        }

        public IEnumerable<VoyageEnergyAdvisorVoyageOption> AddTotalPowerToRouteSegments(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions)
        {
            return voyageOptions.Select(voyageOption =>
            {
                voyageOption.RouteSegments = voyageOption.RouteSegments.Select(routeSegment =>
                {
                    routeSegment.AvgTotalResistancePower = routeSegment.AvgCalmWaterResistancePower.GetValueOrDefault() +
                                                 routeSegment.AvgWindResistancePower.GetValueOrDefault() +
                                                 routeSegment.AvgCurrentResistancePower.GetValueOrDefault() +
                                                 routeSegment.AvgWaveResistancePower.GetValueOrDefault() +
                                                 routeSegment.AvgSailResistancePower.GetValueOrDefault();

                    routeSegment.AvgNetWeatherResistancePower = routeSegment.AvgTotalResistancePower.GetValueOrDefault() - routeSegment.AvgCalmWaterResistancePower.GetValueOrDefault();
                    return routeSegment;
                }).ToList();
                return voyageOption;
            });
        }

        public IEnumerable<VoyageEnergyAdvisorVoyageOption> AddTotalPowerAndEnergyToVoyageOptions(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions
        )
        {

            var voyageOptionsList = voyageOptions.ToList();
            int total = voyageOptionsList.Count;
            int processed = 0;

            double startPercent = 85;
            double endPercent = 95;

            var optionsWithTotalEnergy = voyageOptions.Select(voyageOption =>
            {
                if (voyageOption.IsValid)
                {
                    double totalOptionResistanceEnergy = 0;
                    double totalOptionWindEnergy = 0;
                    double totalOptionWaveEnergy = 0;
                    double totalOptionCurrentEnergy = 0;
                    double totalOptionSailEnergy = 0;
                    double totalOptionCalmWaterResistanceEnergy = 0;

                    foreach (var routeSegment in voyageOption.RouteSegments)
                    {
                        var segmentDurationHour = (routeSegment.EndTime - routeSegment.StartTime).TotalHours;
                        totalOptionResistanceEnergy += routeSegment.AvgTotalResistancePower.GetValueOrDefault() * segmentDurationHour;
                        totalOptionWindEnergy += routeSegment.AvgWindResistancePower.GetValueOrDefault() * segmentDurationHour;
                        totalOptionWaveEnergy += routeSegment.AvgWaveResistancePower.GetValueOrDefault() * segmentDurationHour;
                        totalOptionCurrentEnergy += routeSegment.AvgCurrentResistancePower.GetValueOrDefault() * segmentDurationHour;
                        totalOptionSailEnergy += routeSegment.AvgSailResistancePower.GetValueOrDefault() * segmentDurationHour;
                        totalOptionCalmWaterResistanceEnergy += routeSegment.AvgCalmWaterResistancePower.GetValueOrDefault() * segmentDurationHour;
                    }
                    voyageOption.TotalResistanceEnergyConsumption = totalOptionResistanceEnergy;
                    voyageOption.TotalCalmWaterResistanceEnergyConsumption = totalOptionCalmWaterResistanceEnergy;

                    voyageOption.RelativeWindEnergyConsumption = 100 * totalOptionWindEnergy / totalOptionCalmWaterResistanceEnergy;
                    voyageOption.AbsTotalWindEnergy = Math.Abs(totalOptionWindEnergy);
                    
                    voyageOption.RelativeWaveEnergyConsumption = 100 * totalOptionWaveEnergy / totalOptionCalmWaterResistanceEnergy;
                    voyageOption.AbsTotalWaveEnergy = Math.Abs(totalOptionWaveEnergy);
                    
                    voyageOption.RelativeCurrentEnergyConsumption = 100 * totalOptionCurrentEnergy / totalOptionCalmWaterResistanceEnergy;
                    voyageOption.AbsTotalCurrentEnergy = Math.Abs(totalOptionCurrentEnergy);
                    
                    voyageOption.RelativeSailEnergyConsumption = 100 * totalOptionSailEnergy / totalOptionCalmWaterResistanceEnergy;
                    voyageOption.AbsTotalSailEnergy = Math.Abs(totalOptionSailEnergy);
                    
                    voyageOption.AverageResistancePower = totalOptionResistanceEnergy / (voyageOption.Eta - voyageOption.Etd).TotalHours;
                }

                processed++;
                progressService.UpdateProgressDynamic(processed, total, startPercent, endPercent, "Calculating energy consumption")
                    .ConfigureAwait(false);

                return voyageOption;
            }).ToList();

            var validOptions = optionsWithTotalEnergy.Where(e => e.IsValid);
            double? minConsumption = validOptions.Count() > 0
                ? validOptions
                    .OrderBy(e => e.TotalResistanceEnergyConsumption.GetValueOrDefault())
                    .First().TotalResistanceEnergyConsumption
                : null;

            return optionsWithTotalEnergy.Select(e =>
            {
                e.EnergyConsumptionRelative = e.TotalResistanceEnergyConsumption.HasValue && minConsumption.HasValue
                    ? 100 * (e.TotalResistanceEnergyConsumption.Value / minConsumption.Value - 1)
                    : null;
                return e;
            }).ToList();
        }

        public IEnumerable<VoyageEnergyAdvisorVoyageOption> AddTotalFuelConsumptionToVoyageOptions(
         IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions
       )
        {
            var voyageOptionsList = voyageOptions.ToList();
            int total = voyageOptionsList.Count;
            int processed = 0;

            double startPercent = 95; 
            double endPercent = 100;   

            var optionsWithTotalFuel = voyageOptions.Select(voyageOption =>
            {
                if (voyageOption.IsValid)
                {
                    double totalOptionResistanceFuel = 0;
                    double totalOptionWindFuel = 0;
                    double totalOptionWaveFuel = 0;
                    double totalOptionCurrentFuel = 0;
                    double totalOptionSailFuel = 0;
                    double totalOptionCalmWaterResistanceFuel = 0;

                    foreach (var routeSegment in voyageOption.RouteSegments)
                    {
                        var segmentDurationHour = (routeSegment.EndTime - routeSegment.StartTime).TotalHours;

                        totalOptionResistanceFuel += routeSegment.AvgTotalResistanceFuelConsumption.GetValueOrDefault() * segmentDurationHour;
                        totalOptionWindFuel += routeSegment.AvgWindResistanceFuelConsumption.GetValueOrDefault() * segmentDurationHour;
                        totalOptionWaveFuel += routeSegment.AvgWaveResistanceFuelConsumption.GetValueOrDefault() * segmentDurationHour;
                        totalOptionCurrentFuel += routeSegment.AvgCurrentResistanceFuelConsumption.GetValueOrDefault() * segmentDurationHour;
                        totalOptionSailFuel += routeSegment.AvgSailResistanceFuelConsumption.GetValueOrDefault() * segmentDurationHour;
                        totalOptionCalmWaterResistanceFuel += routeSegment.AvgCalmWaterResistanceFuelConsumption.GetValueOrDefault() * segmentDurationHour;
                    }

                    voyageOption.TotalFuelConsumption = totalOptionResistanceFuel;
                    voyageOption.TotalCalmWaterResistanceFuelConsumption = totalOptionCalmWaterResistanceFuel;

                    // Relative numbers
                    voyageOption.RelativeWindFuelConsumption = 100 * totalOptionWindFuel / totalOptionCalmWaterResistanceFuel;

                    voyageOption.AbsTotalWindFuelConsumption = Math.Abs(totalOptionWindFuel);

                    voyageOption.RelativeWaveFuelConsumption = 100 * totalOptionWaveFuel / totalOptionCalmWaterResistanceFuel;

                    voyageOption.AbsTotalWaveFuelConsumption = Math.Abs(totalOptionWaveFuel);

                    voyageOption.RelativeCurrentFuelConsumption = 100 * totalOptionCurrentFuel / totalOptionCalmWaterResistanceFuel;

                    voyageOption.AbsTotalCurrentFuelConsumption = Math.Abs(totalOptionCurrentFuel);

                    voyageOption.RelativeSailFuelConsumption = 100 * totalOptionSailFuel / totalOptionCalmWaterResistanceFuel;

                    voyageOption.AbsTotalSailFuelConsumption = Math.Abs(totalOptionSailFuel);

                    voyageOption.AverageFuelConsumptionRate = totalOptionResistanceFuel /
                        (voyageOption.Eta - voyageOption.Etd).TotalHours;
                }

                processed++;
                progressService.UpdateProgressDynamic(processed, total, startPercent, endPercent, "Calculating fuel consumption")
                    .ConfigureAwait(false);

                return voyageOption;
            }).ToList();

            var validOptions = optionsWithTotalFuel.Where(e => e.IsValid);
            double? minFuel = validOptions.Any()
                ? validOptions.OrderBy(e => e.TotalFuelConsumption.GetValueOrDefault())
                              .First().TotalFuelConsumption
                : null;

            return optionsWithTotalFuel.Select(e =>
            {
                e.FuelConsumptionRelative = e.TotalFuelConsumption.HasValue && minFuel.HasValue
                    ? 100 * (e.TotalFuelConsumption.Value / minFuel.Value - 1)
                    : null;
                return e;
            }).ToList();
        }


        public IEnumerable<VoyageEnergyAdvisorVoyageOption> AddFavorableWeatherIndexToVoyageOptions(
            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions)
        {
            return voyageOptions.Select(voyageOption =>
            {
                if (voyageOption.RouteSegments == null) throw new ArgumentNullException(nameof(voyageOption.RouteSegments));

                var maxWeatherPowerDeviation = voyageOption.RouteSegments
                    .Select(e => Math.Abs(e.AvgTotalResistancePower.GetValueOrDefault() - e.AvgCalmWaterResistancePower.GetValueOrDefault()))
                    .Max();

                // Avoid division by zero
                if (Math.Abs(maxWeatherPowerDeviation) < double.Epsilon)
                {
                    voyageOption.RouteSegments = voyageOption.RouteSegments.Select(routeSegment =>
                    {
                        routeSegment.FavorableWeatherIndex = 0.5; // Default value if no variation
                        return routeSegment;
                    }).ToList();
                }
                else
                {
                    voyageOption.RouteSegments = voyageOption.RouteSegments.Select(routeSegment =>
                    {
                        var weatherResistancePower = routeSegment.AvgTotalResistancePower.GetValueOrDefault() - routeSegment.AvgCalmWaterResistancePower.GetValueOrDefault();
                        routeSegment.FavorableWeatherIndex = 0.5 + (0.5 * weatherResistancePower / maxWeatherPowerDeviation);
                        return routeSegment;
                    }).ToList();
                }

                return voyageOption;
            });
        }
        
        private IEnumerable<DateTime> GetTimeOptions(DateTime? minTime, DateTime? maxTime, int numberOfOptions)
        {
            if (!minTime.HasValue || !maxTime.HasValue || minTime > maxTime)
            {
                yield break;
            }
            if (minTime == maxTime)
            {
                yield return minTime.GetValueOrDefault();
            }
            else
            {
                var stepSize = TimeSpan.FromMilliseconds((maxTime - minTime).Value.TotalMilliseconds / (numberOfOptions - 1));
                yield return minTime.GetValueOrDefault();
                for (int i = 1; i < numberOfOptions - 1; i++)
                {
                    yield return minTime.GetValueOrDefault() + (stepSize * i);
                }
                yield return maxTime.GetValueOrDefault();
            }
        }

        public string BuildValidationMessage(VoyageEnergyAdvisorRequest request)
        {
            var nowUtc = DateTime.UtcNow;

            var distance = request.Route.GetVoyageDistance();
            var speedMin = Math.Max(request.SpeedMin, 0.000001);
            var speedMax = Math.Max(request.SpeedMax, speedMin);

            // Fastest = shortest duration; Slowest = longest duration
            var minTravelDuration = TimeSpan.FromSeconds(distance / speedMax);

            // Normalize ETD min (cannot be in the past)
            var normalizedEtdMin = request.EtdMin.HasValue && request.EtdMin.Value > nowUtc
                ? request.EtdMin.Value
                : nowUtc;

            string F(DateTime dt) => dt.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);

            switch (request.TimeSelectionMode)
            {
                case TimeSelectionMode.ETD:
                    // ETD mode: minimum ETD is always now
                    return $"No valid voyage options available. Please select ETD after {F(nowUtc)} UTC.";

                case TimeSelectionMode.ETA:
                    {
                        var earliestFeasibleEta = normalizedEtdMin + minTravelDuration;

                        if (request.EtaMin.HasValue && request.EtaMin.Value > earliestFeasibleEta)
                            earliestFeasibleEta = request.EtaMin.Value;

                        return $"No valid voyage options available. Please select ETA after {F(earliestFeasibleEta)} UTC.";
                    }

                default:
                    return "Invalid time selection mode.";
            }
        }

        public double CalculateRequiredAverageSpeed(double voyageDistance, DateTime etd, DateTime eta)
        {
            var voyageDurationSeconds = (eta - etd).TotalSeconds;
            return voyageDurationSeconds > 0 ? voyageDistance / voyageDurationSeconds : 0;
        }

        public double CalculateSegmentPowerBalance(
            VoyageEnergyAdvisorVoyageOptionRouteSegment segment, double constantPropulsionPower, double candidateSpeed)
        {
            var resistancePower = GetSegmentResistancePowerAtSpeed(segment, candidateSpeed);
            return constantPropulsionPower - resistancePower;
        }

        // Solves for the steady state speed of a single segment at a given constant propulsion power.
        // Note on convention: the existing resistance services (calm water, wind, current, wave, sail) are all
        // expressed in Power (Watts), not Force. At steady state Force_net == 0 is equivalent to Power_net == 0
        // (Power = Force * speed, speed > 0), so the balance below is done in the Power domain to reuse the
        // existing Add*PowerToRouteSegments pipeline as-is instead of introducing a parallel force-based model.
        public double SolveSegmentSpeedForConstantPower(
            VoyageEnergyAdvisorVoyageOptionRouteSegment segment, double constantPropulsionPower, double speedMin, double speedMax)
        {
            var lowerSpeed = speedMin;
            var upperSpeed = speedMax;
            var bestSpeed = lowerSpeed;

            for (var i = 0; i < MaxSpeedIterationsPerSegment; i++)
            {
                var candidateSpeed = (lowerSpeed + upperSpeed) / 2.0;
                var netPower = CalculateSegmentPowerBalance(segment, constantPropulsionPower, candidateSpeed);

                if (netPower >= 0)
                {
                    // Surplus (or exactly enough) power at this speed: the vessel can sustain at least this speed.
                    bestSpeed = candidateSpeed;
                    lowerSpeed = candidateSpeed;
                }
                else
                {
                    // Insufficient power at this speed: the vessel must slow down.
                    upperSpeed = candidateSpeed;
                }

                if (Math.Abs(netPower) <= PowerBalanceTolerance || upperSpeed - lowerSpeed <= SpeedTolerance)
                {
                    break;
                }
            }

            return bestSpeed;
        }

        private double GetSegmentResistancePowerAtSpeed(VoyageEnergyAdvisorVoyageOptionRouteSegment segment, double candidateSpeed)
        {
            IEnumerable<VoyageEnergyAdvisorVoyageOption> probeOptions = new[]
            {
                new VoyageEnergyAdvisorVoyageOption
                {
                    RouteSegments = new List<VoyageEnergyAdvisorVoyageOptionRouteSegment>
                    {
                        segment with { AverageSpeed = candidateSpeed }
                    }
                }
            };

            probeOptions = AddApparentWeatherToRouteSegments(probeOptions);
            probeOptions = AddCalmWaterPowerToRouteSegments(probeOptions);
            probeOptions = AddWindPowerToRouteSegments(probeOptions);
            probeOptions = AddWavePowerToRouteSegments(probeOptions);
            probeOptions = AddCurrentPowerToRouteSegments(probeOptions);
            probeOptions = AddSailPowerToRouteSegments(probeOptions);
            probeOptions = AddTotalPowerToRouteSegments(probeOptions);

            return probeOptions.First().RouteSegments[0].AvgTotalResistancePower.GetValueOrDefault();
        }

        private (IReadOnlyList<double> SegmentSpeeds, double TotalDurationSeconds) SolveSegmentSpeedsAndDuration(
            IList<VoyageEnergyAdvisorVoyageOptionRouteSegment> segments,
            double constantPropulsionPower,
            double speedLowerBound,
            double speedUpperBound)
        {
            var speeds = new double[segments.Count];
            double totalDurationSeconds = 0;

            for (var i = 0; i < segments.Count; i++)
            {
                var segmentSpeed = SolveSegmentSpeedForConstantPower(segments[i], constantPropulsionPower, speedLowerBound, speedUpperBound);
                speeds[i] = segmentSpeed;

                var segmentDistance = segments[i].StartPosition!.GetDistanceTo(segments[i].EndPosition!);
                totalDurationSeconds += segmentSpeed > 0 ? segmentDistance / segmentSpeed : double.PositiveInfinity;
            }

            return (speeds, totalDurationSeconds);
        }

        // Outer bounded binary search: finds the lowest constant propulsion power for which every segment's
        // steady state speed (see SolveSegmentSpeedForConstantPower) still lets the vessel arrive by ETA.
        // The search is bounded to +-AverageSpeedSearchBandFraction around the required average speed (not
        // the request's SpeedMin/SpeedMax), using calm water resistance at those two speeds as the power bounds.
        // The required average speed itself is derived from the segments' total distance and the allowed
        // voyage duration, rather than being supplied by the caller (the two are not independent).
        private (IReadOnlyList<double> SegmentSpeeds, double ConstantPropulsionPower) FindMinimumFeasibleConstantPower(
            IList<VoyageEnergyAdvisorVoyageOptionRouteSegment> segments,
            double allowedVoyageDurationSeconds)
        {
            var totalDistance = segments.Sum(s => s.StartPosition!.GetDistanceTo(s.EndPosition!));
            var requiredAverageSpeed = totalDistance / allowedVoyageDurationSeconds;

            var speedLowerBound = requiredAverageSpeed * (1.0 - AverageSpeedSearchBandFraction);
            var speedUpperBound = requiredAverageSpeed * (1.0 + AverageSpeedSearchBandFraction);

            var powerLowerBound = calmWaterResistanceService.GetCalmWaterResistancePower(speedLowerBound);
            var powerUpperBound = calmWaterResistanceService.GetCalmWaterResistancePower(speedUpperBound);

            var (fastestSpeeds, fastestDurationSeconds) = SolveSegmentSpeedsAndDuration(segments, powerUpperBound, speedLowerBound, speedUpperBound);

            if (fastestDurationSeconds > allowedVoyageDurationSeconds + TimeTolerance)
            {
                throw new OptimalVoyageRequestException(
                    $"No constant propulsion power can satisfy the requested ETA, even at {speedUpperBound:F2} m/s. " +
                    $"The fastest achievable voyage takes {fastestDurationSeconds / 3600.0:F2} hours, but only " +
                    $"{allowedVoyageDurationSeconds / 3600.0:F2} hours are available between ETD and ETA.");
            }

            var bestFeasibleSpeeds = fastestSpeeds;
            var bestFeasiblePower = powerUpperBound;

            var lowerPower = powerLowerBound;
            var upperPower = powerUpperBound;

            for (var i = 0; i < MaxPowerIterations; i++)
            {
                var candidatePower = (lowerPower + upperPower) / 2.0;
                var (segmentSpeeds, totalDurationSeconds) = SolveSegmentSpeedsAndDuration(segments, candidatePower, speedLowerBound, speedUpperBound);

                if (totalDurationSeconds <= allowedVoyageDurationSeconds + TimeTolerance)
                {
                    // Candidate power is feasible (arrives in time): try an even lower power.
                    bestFeasibleSpeeds = segmentSpeeds;
                    bestFeasiblePower = candidatePower;
                    upperPower = candidatePower;
                }
                else
                {
                    // Candidate power is too low: the vessel arrives too late.
                    lowerPower = candidatePower;
                }
            }

            return (bestFeasibleSpeeds, bestFeasiblePower);
        }

        public async Task<VoyageEnergyAdvisorVoyageOption> BuildOptimalVoyageOption(
            VoyageEnergyAdvisorOptimalVoyageRequest request, double requiredAverageSpeed)
        {
            var voyageOption = new VoyageEnergyAdvisorVoyageOption
            {
                Etd = request.Etd,
                Eta = request.Eta,
                AverageSpeed = requiredAverageSpeed,
                DurationInSeconds = (request.Eta - request.Etd).TotalSeconds,
                IsValid = true
            };

            IEnumerable<VoyageEnergyAdvisorVoyageOption> voyageOptions = new List<VoyageEnergyAdvisorVoyageOption> { voyageOption };
            voyageOptions = AddRouteSegments(voyageOptions, request.Route);
            voyageOptions = AddTimeToRouteSegments(voyageOptions);
            voyageOptions = AddCourseToRouteSegments(voyageOptions);
            voyageOptions = await AddTrueWeatherToRouteSegments(voyageOptions);

            voyageOption = voyageOptions.First();

            var (segmentSpeeds, _) = FindMinimumFeasibleConstantPower(
                voyageOption.RouteSegments,
                (request.Eta - request.Etd).TotalSeconds);

            voyageOption.RouteSegments = voyageOption.RouteSegments
                .Select((segment, i) => segment with { AverageSpeed = segmentSpeeds[i] })
                .ToList();

            // Re-run the same enrichment pipeline used for regular voyage options, now that every segment has
            // its solved steady-state speed, so the result carries the identical breakdown of fields.
            voyageOptions = new List<VoyageEnergyAdvisorVoyageOption> { voyageOption };
            voyageOptions = AddTimeToRouteSegments(voyageOptions);
            voyageOptions = AddApparentWeatherToRouteSegments(voyageOptions);
            voyageOptions = AddCalmWaterPowerToRouteSegments(voyageOptions);
            voyageOptions = AddWindPowerToRouteSegments(voyageOptions);
            voyageOptions = AddWavePowerToRouteSegments(voyageOptions);
            voyageOptions = AddCurrentPowerToRouteSegments(voyageOptions);
            voyageOptions = AddSailPowerToRouteSegments(voyageOptions);
            voyageOptions = AddTotalPowerToRouteSegments(voyageOptions);
            voyageOptions = AddFuelConsumptionToRouteSegments(voyageOptions);
            voyageOptions = AddCostToRouteSegments(voyageOptions);
            voyageOptions = AddTotalPowerAndEnergyToVoyageOptions(voyageOptions);
            voyageOptions = AddTotalFuelConsumptionToVoyageOptions(voyageOptions);
            voyageOptions = AddTotalCostToVoyageOptions(voyageOptions);
            voyageOptions = AddFavorableWeatherIndexToVoyageOptions(voyageOptions);

            return voyageOptions.First();
        }

    }
}
