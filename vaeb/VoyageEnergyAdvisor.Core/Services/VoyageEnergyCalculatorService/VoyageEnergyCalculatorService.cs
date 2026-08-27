namespace VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService
{
    using VoyageEnergyAdvisor.Core.CommonModels;
    using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Exceptions;
    using VoyageEnergyAdvisor.Core.Services.VoyageEnergyAdvisorService.Models;
    using VoyageEnergyAdvisor.Core.Services.VoyageEnergyCalculatorService.Models;
    using Helpers;
    using VoyageEnergyAdvisor.Core.Services.AisService;
    using Microsoft.Extensions.Logging;
    using VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels;

    public class VoyageEnergyAdvisorService(
        IVoyageEnergyAdvisorVoyageOptionsBuilder voyageEnergyAdvisorVoyageOptionsBuilder,
        IAisService aisService,
        ILogger<VoyageEnergyAdvisorService> logger)
        : IVoyageEnergyAdvisorService
    {
        public async Task<VoyageEnergyAdvisorResponse> GetVoyageOptions(VoyageEnergyAdvisorRequest request)
        {
            var validRequest = voyageEnergyAdvisorVoyageOptionsBuilder.ToValidRequest(request);

            if (validRequest != null)
            {
                var voyageOptionSets = await voyageEnergyAdvisorVoyageOptionsBuilder.PrepareVoyageOptionSets(validRequest);
                var hasValidOptions = voyageOptionSets.Any(o => o.IsValid);

                var response = new VoyageEnergyAdvisorResponse()
                {
                    VoyageDistance = validRequest.Route.GetVoyageDistance(),
                    VoyageOptionSets = voyageOptionSets.ToList()
                };

                if (!hasValidOptions)
                {
                    response.ValidationMessage = voyageEnergyAdvisorVoyageOptionsBuilder.BuildValidationMessage(validRequest);
                }

                return response;
            }

            return new VoyageEnergyAdvisorResponse();
        }

        public async Task<VoyageEnergyAdvisorLiveResponse> GetLiveData(VoyageEnergyAdvisorLiveRequest request)
        {
            var vesselData = await aisService.GetCurrentVesselDataAsync();
            var realSpeed = vesselData?.Speed ?? 0.0;
            CurrentPosition vesselPosition = new CurrentPosition();

            if (vesselData?.Latitude.HasValue == true && vesselData?.Longitude.HasValue == true)
            {
                vesselPosition = new CurrentPosition
                {
                    Coordinate = new GeoCoordinate(vesselData.Latitude.Value, vesselData.Longitude.Value),
                    Heading = vesselData.Heading,
                    Course = vesselData.Course,
                    Status = vesselData.Status,
                    VesselName = vesselData.VesselName,
                    PositionUpdatedAt = vesselData.PositionUpdatedAt
                };
                logger.LogInformation($"Using real vessel position: {vesselPosition.Coordinate.Latitude:F6}, {vesselPosition.Coordinate.Longitude:F6}");
            }
        
            var remainingRoute = request.Route.GetRemainingRoute(vesselPosition.Coordinate).SplitToSegments(10000.0);
 
            var voyageOption = new VoyageEnergyAdvisorVoyageOption(); 
            voyageOption.Etd = DateTime.UtcNow.AddMinutes(1);
            voyageOption.AverageSpeed = realSpeed;
            voyageOption.DurationInSeconds = realSpeed > 0 ? remainingRoute.GetVoyageDistance() / realSpeed : 0;
            voyageOption.Eta = voyageOption.Etd.AddSeconds(voyageOption.DurationInSeconds);
            voyageOption.IsLiveMode = true;

            if (realSpeed > 0.3 && remainingRoute.Waypoints.Count > 1)
            {
                voyageOption = (await voyageEnergyAdvisorVoyageOptionsBuilder.PopulateVoyageOptions([voyageOption], remainingRoute)).First();
            }

            return new VoyageEnergyAdvisorLiveResponse()
            {
                CurrentSpeed = realSpeed,
                RemainingTimeInSeconds = voyageOption.DurationInSeconds,
                Eta = voyageOption.Eta,
                RemainingRouteSegments = voyageOption.RouteSegments,
                CurrentPosition = vesselPosition
            };
        }

    }
}
