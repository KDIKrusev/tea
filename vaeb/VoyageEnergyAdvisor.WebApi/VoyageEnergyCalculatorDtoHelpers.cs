using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Models.VoyageEnergyAdvisor;
using VoyageEnergyAdvisor.WebApi.Dtos;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models;
using VoyageEnergyAdvisor.Core.Services.VoyageEnergyCalculatorService.Models;

namespace VoyageEnergyAdvisor.WebApi;

public static class VoyageEnergyAdvisorDtoHelpers
{
    public static VoyageEnergyAdvisorResponseDto GetResponseDto(
     VoyageEnergyAdvisorResponse response)
    {
        return new VoyageEnergyAdvisorResponseDto
        {
            VoyageDistance = response.VoyageDistance,
            ValidationMessage = response.ValidationMessage,
            VoyageOptions = response.VoyageOptions.Select(option => option.ToDto()).ToList()
        };
    }

    private static VoyageEnergyAdvisorVoyageOptionDto ToDto(this VoyageEnergyAdvisorVoyageOption option)
    {
        return new VoyageEnergyAdvisorVoyageOptionDto
        {
            Etd = new DateTimeOffset(DateTime.SpecifyKind(option.Etd, DateTimeKind.Utc)).ToUnixTimeMilliseconds(),
            Eta = new DateTimeOffset(DateTime.SpecifyKind(option.Eta, DateTimeKind.Utc)).ToUnixTimeMilliseconds(),
            IsValid = option.IsValid,
            AverageSpeed = option.AverageSpeed,
            DurationInSeconds = option.DurationInSeconds,

            // Energy
            TotalWindEnergyConsumption = option.AbsTotalWindEnergy,
            TotalWaveEnergyConsumption = option.AbsTotalWaveEnergy,
            TotalCurrentEnergyConsumption = option.AbsTotalCurrentEnergy,
            TotalSailEnergyConsumption = option.AbsTotalSailEnergy,
            TotalEnergyConsumption = option.TotalResistanceEnergyConsumption,
            TotalCalmWaterResistanceEnergyConsumption = option.TotalCalmWaterResistanceEnergyConsumption,
            RelativeCurrentEnergyConsumption = option.RelativeCurrentEnergyConsumption,
            RelativeSailEnergyConsumption = option.RelativeSailEnergyConsumption,
            RelativeWaveEnergyConsumption = option.RelativeWaveEnergyConsumption,
            RelativeWindEnergyConsumption = option.RelativeWindEnergyConsumption,
            AveragePower = option.AverageResistancePower,
            EnergyConsumptionRelative = option.EnergyConsumptionRelative,

            // Fuel
            TotalResistanceFuelConsumption = option.TotalFuelConsumption,
            TotalCalmWaterResistanceFuelConsumption = option.TotalCalmWaterResistanceFuelConsumption,
            TotalWindFuelConsumption = option.AbsTotalWindFuelConsumption,
            TotalWaveFuelConsumption = option.AbsTotalWaveFuelConsumption,
            TotalCurrentFuelConsumption = option.AbsTotalCurrentFuelConsumption,
            TotalSailFuelConsumption = option.AbsTotalSailFuelConsumption,
            RelativeWindFuelConsumption = option.RelativeWindFuelConsumption,
            RelativeWaveFuelConsumption = option.RelativeWaveFuelConsumption,
            RelativeCurrentFuelConsumption = option.RelativeCurrentFuelConsumption,
            RelativeSailFuelConsumption = option.RelativeSailFuelConsumption,
            AverageFuelConsumptionRate = option.AverageFuelConsumptionRate,
            FuelConsumptionRelative = option.FuelConsumptionRelative,

            // Cost
            TotalResistanceCost = option.TotalResistanceCost,
            TotalCalmWaterResistanceCost = option.TotalCalmWaterResistanceCost,
            TotalWindCost = option.AbsTotalWindCost,
            TotalWaveCost = option.AbsTotalWaveCost,
            TotalCurrentCost = option.AbsTotalCurrentCost,
            TotalSailCost = option.AbsTotalSailCost,
            AbsTotalWindCost = option.AbsTotalWindCost,
            AbsTotalWaveCost = option.AbsTotalWaveCost,
            AbsTotalCurrentCost = option.AbsTotalCurrentCost,
            AbsTotalSailCost = option.AbsTotalSailCost,
            RelativeWindCost = option.RelativeWindCost,
            RelativeWaveCost = option.RelativeWaveCost,
            RelativeCurrentCost = option.RelativeCurrentCost,
            RelativeSailCost = option.RelativeSailCost,
            AverageCostRate = option.AverageCostRate,
            CostRelative = option.CostRelative,

            RouteSegments = option.RouteSegments.ToDto().ToList(),
        };
    }

    private static IEnumerable<VoyageEnergyAdvisorVoyageOptionRouteSegmentDto> ToDto(
     this IEnumerable<VoyageEnergyAdvisorVoyageOptionRouteSegment> segments)
    {
        return segments.Select(segment => new VoyageEnergyAdvisorVoyageOptionRouteSegmentDto
        {
            StartTime = new DateTimeOffset(DateTime.SpecifyKind(segment.StartTime, DateTimeKind.Utc)).ToUnixTimeMilliseconds(),
            EndTime = new DateTimeOffset(DateTime.SpecifyKind(segment.EndTime, DateTimeKind.Utc)).ToUnixTimeMilliseconds(),
            StartPosition = new GeoCoordinateDto(segment.StartPosition!.Latitude, segment.StartPosition.Longitude),
            EndPosition = new GeoCoordinateDto(segment.EndPosition!.Latitude, segment.EndPosition.Longitude),
            Course = segment.Course,
            AverageSpeed = segment.AverageSpeed,
            DurationInSeconds = segment.DurationInSeconds,
            TrueWeather = segment.TrueWeather!.ToDto(),
            ApparentWeather = segment.ApparentWeather!.ToDto(),

            // Power
            AvgTotalPower = segment.AvgTotalResistancePower,
            AvgCalmWaterPower = segment.AvgCalmWaterResistancePower,
            AvgWindPower = segment.AvgWindResistancePower,
            AvgWavePower = segment.AvgWaveResistancePower,
            AvgCurrentPower = segment.AvgCurrentResistancePower,
            AvgSailPower = segment.AvgSailResistancePower,
            AvgNetWeatherResistancePower = segment.AvgNetWeatherResistancePower,
            FavorableWeatherIndex = segment.FavorableWeatherIndex,

            // Fuel
            AvgTotalResistanceFuelConsumption = segment.AvgTotalResistanceFuelConsumption,
            AvgCalmWaterResistanceFuelConsumption = segment.AvgCalmWaterResistanceFuelConsumption,
            AvgWindResistanceFuelConsumption = segment.AvgWindResistanceFuelConsumption,
            AvgWaveResistanceFuelConsumption = segment.AvgWaveResistanceFuelConsumption,
            AvgCurrentResistanceFuelConsumption = segment.AvgCurrentResistanceFuelConsumption,
            AvgSailResistanceFuelConsumption = segment.AvgSailResistanceFuelConsumption,
            AvgNetWeatherResistanceFuelConsumption = segment.AvgNetWeatherResistanceFuelConsumption,

                 // Cost
            AvgTotalResistanceCost = segment.AvgTotalResistanceCost,
            AvgCalmWaterResistanceCost = segment.AvgCalmWaterResistanceCost,
            AvgWindResistanceCost = segment.AvgWindResistanceCost,
            AvgWaveResistanceCost = segment.AvgWaveResistanceCost,
            AvgCurrentResistanceCost = segment.AvgCurrentResistanceCost,
            AvgSailResistanceCost = segment.AvgSailResistanceCost,
            AvgNetWeatherResistanceCost = segment.AvgNetWeatherResistanceCost
        });
    }

    private static WeatherDataDto ToDto(this WeatherData weatherData)
    {
        return new WeatherDataDto()
        {
            WindSpeed = weatherData.WindSpeed,
            CurrentDirection = weatherData.CurrentFromDirection,
            CurrentSpeed = weatherData.CurrentSpeed,
            WaveDirection = weatherData.WaveFromDirection,
            WindDirection = weatherData.WindFromDirection,
            WavePeakPeriod = weatherData.WavePeakPeriod,
            WaveHeight = weatherData.WaveHeight,
        };
    }

    public static VoyageEnergyAdvisorRequest GetRequestFromDto(VoyageEnergyAdvisorRequestDto request)
    {
        return new VoyageEnergyAdvisorRequest()
        {
            SpeedMin = request.SpeedMin,
            SpeedMax = request.SpeedMax,
            ReturnArrayDimension = 5, //request.ReturnArrayDimension,
            EtdMin = request.EtdMin >= 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(request.EtdMin / 1000).DateTime
                : null,
            EtdMax = request.EtdMax >= 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(request.EtdMax / 1000).DateTime
                : null,
            EtaMin = request.EtaMin >= 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(request.EtaMin / 1000).DateTime
                : null,
            EtaMax = request.EtaMax >= 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(request.EtaMax / 1000).DateTime
                : null,
            Route = new Route
            {
                RouteName = request.Route.RouteName,
                Waypoints = request.Route.Waypoints.Select(e => new GeoCoordinate(e.Latitude, e.Longitude)).ToList()
            },
        };
    }



    public static VoyageEnergyAdvisorOptimalVoyageRequest GetOptimalVoyageRequestFromDto(
        VoyageEnergyAdvisorOptimalVoyageRequestDto request)
    {
        return new VoyageEnergyAdvisorOptimalVoyageRequest()
        {
            Etd = DateTimeOffset.FromUnixTimeMilliseconds(request.Etd).UtcDateTime,
            Eta = DateTimeOffset.FromUnixTimeMilliseconds(request.Eta).UtcDateTime,
            SpeedMin = request.SpeedMin,
            SpeedMax = request.SpeedMax,
            Route = new Route
            {
                RouteName = request.Route.RouteName,
                Waypoints = request.Route.Waypoints.Select(e => new GeoCoordinate(e.Latitude, e.Longitude)).ToList()
            },
        };
    }

    public static VoyageEnergyAdvisorOptimalVoyageResponseDto GetOptimalVoyageResponseDto(
        VoyageEnergyAdvisorVoyageOption option)
    {
        return new VoyageEnergyAdvisorOptimalVoyageResponseDto
        {
            OptimalVoyageOption = option.ToDto()
        };
    }

    public static VoyageEnergyAdvisorLiveRequest GetLiveRequestFromDto(VoyageEnergyAdvisorLiveRequestDto request)
    {
        return new VoyageEnergyAdvisorLiveRequest()
        {
            Route = new Route
            {
                RouteName = request.Route.RouteName,
                Waypoints = request.Route.Waypoints.Select(e => new GeoCoordinate(e.Latitude, e.Longitude)).ToList()
            },
        };
    }
    
    public static VoyageEnergyAdvisorLiveResponseDto GetLiveResponseDto(
        VoyageEnergyAdvisorLiveResponse response)
    {
        return new VoyageEnergyAdvisorLiveResponseDto
        {
            CurrentSpeed = response.CurrentSpeed,
            RemainingTimeInSeconds = response.RemainingTimeInSeconds,
            Eta = new DateTimeOffset(response.Eta).ToUnixTimeMilliseconds(),
            RemainingRouteSegments = response.RemainingRouteSegments.ToDto().ToList(),
            CurrentPosition = response.CurrentPosition != null ? new CurrentPositionDto
            {
                Latitude = response.CurrentPosition.Coordinate.Latitude,
                Longitude = response.CurrentPosition.Coordinate.Longitude,
                Heading = response.CurrentPosition.Heading,
                Course = response.CurrentPosition.Course,
                Status = response.CurrentPosition.Status,
                VesselName = response.CurrentPosition.VesselName,
                PositionUpdatedAt = response.CurrentPosition.PositionUpdatedAt
            } : null
        };
    }
}