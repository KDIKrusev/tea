namespace VoyageEnergyAdvisor.Data.DataRepositories
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using VoyageEnergyAdvisor.Core.CommonModels;
    using VoyageEnergyAdvisor.Core.Repositories;
    using VoyageEnergyAdvisor.Core.Services.RouteService;

    public class RouteRepository : IRouteRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RouteRepository(
                ApplicationDbContext dbContext, 
                IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<string>> GetRoutesListAsync()
        {
            var vesselId = GetCurrentVesselId();
            if (vesselId == null)
            {
                throw new UnauthorizedAccessException("Vessel ID not found in user claims.");
            }

            return await _dbContext.VesselRoutes
                .Where(vr => vr.VesselId == vesselId)
                .Select(vr => vr.Route.RouteName)
                .ToListAsync();
        }

        public async Task<Route?> GetRouteAsync(string routeName)
        {
            var routeEntity = await _dbContext.Routes
                .FirstOrDefaultAsync(r => r.RouteName == routeName);

            return routeEntity == null
                ? null
                : RouteXmlSerializer.DeserializeRouteFromString(routeEntity.RouteXml);
        }

        private int? GetCurrentVesselId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return null;

            var vesselIdClaim = user.FindFirst("VesselId")?.Value;
            return vesselIdClaim != null ? int.Parse(vesselIdClaim) : null;
        }
    }
}
