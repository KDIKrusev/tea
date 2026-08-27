using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.WebApi.Dtos;

namespace VoyageEnergyAdvisor.WebApi
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.StaticFiles;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using VoyageEnergyAdvisor.Core.Services.RouteService;

    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class RouteController : ControllerBase
    {
        private readonly FileExtensionContentTypeProvider fileExtensionContentTypeProvider = new FileExtensionContentTypeProvider();
        private readonly ILogger<RouteController> _logger;
        private readonly IRouteService _routesService;

        public RouteController(
            IRouteService routesService, 
            ILogger<RouteController> logger,
            IConfiguration configuration)
        {
            _routesService = routesService ?? throw new ArgumentNullException(nameof(routesService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public ActionResult<List<string>> GetRoutesList()
        {
            var routes = _routesService.GetRoutesList() ?? throw new System.ArgumentNullException(nameof(_routesService));
            if (routes.Count == 0)
            {
                _logger.LogWarning("No routes found.");
                return NotFound("No routes available. ");
            }
            _logger.LogInformation("Routes retrieved successfully.");
            return Ok(routes);
        }

        [HttpGet("RouteDetails/{id}")]
        public ActionResult<RouteDto> GetRoute(string id)
        {
            var route = _routesService.GetRoute(id) ?? throw new System.ArgumentNullException(nameof(id));
            var routeDto = ConvertToDto(route);
            return Ok(routeDto);
        }

        [HttpGet("Resources")]
        public IActionResult Get(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest("File name is required.");

            // Determine the correct resources folder path
            string resourcesFolder = GetDefaultResourcesPath();
            string filePath = Path.Combine(resourcesFolder, fileName);

            Console.WriteLine($"[INFO] Looking for file: {filePath}");

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError($"File '{fileName}' not found in '{resourcesFolder}'");
                return NotFound($"File '{fileName}' not found.");
            }

            fileExtensionContentTypeProvider.TryGetContentType(fileName, out string? contentType);
            return PhysicalFile(filePath, contentType ?? "application/json");
        }

        private static string GetDefaultResourcesPath()
        {
            var isRunningInDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

            var contentRootPath = Directory.GetCurrentDirectory();

            string path = isRunningInDocker
                ? Path.Combine(contentRootPath, "DefaultResources")  // Docker & Azure path
                : Path.Combine(contentRootPath, "..", "VoyageEnergyAdvisor.Core", "DefaultResources"); // Local development path

            Console.WriteLine($"[INFO] Default Resources Path (WebApi): {path} (Running in Docker: {isRunningInDocker})");
            return path;
        }



        private RouteDto ConvertToDto(Route route)
        {
            return new RouteDto
            {
                RouteName = route.RouteName,
                Waypoints = route.Waypoints.Select(e => new GeoCoordinateDto(e.Latitude, e.Longitude)).ToList()
            };
        }
    }
}
