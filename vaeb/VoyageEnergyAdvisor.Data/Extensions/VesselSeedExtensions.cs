namespace VoyageEnergyAdvisor.Data.Extensions
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using VoyageEnergyAdvisor.Data.Entities;

    public static class VesselSeedExtensions
    {
        public static async Task SeedVessels(this IServiceProvider serviceProvider)
        {
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

            var vessels = await GetOrCreateSharedVessels(dbContext);

            // Only select default seeded users
            var defaultUsers = await dbContext.Users
                .Where(u => u.Email == "admin@example.com" || u.Email == "user@example.com")
                .ToListAsync();


            if (defaultUsers.Any())
            {
                var userIds = defaultUsers.Select(u => u.Id).ToArray();
                await AssignVesselsToUsers(dbContext, vessels, userIds);
            }

            foreach (var vessel in vessels)
            {
                await LinkVesselsToRoutes(dbContext, vessel.Id);
            }
        }

        private static async Task<List<Vessel>> GetOrCreateSharedVessels(ApplicationDbContext dbContext)
        {
            if (await dbContext.Vessels.CountAsync() >= 2) return await dbContext
                                                                            .Vessels
                                                                            .Where(v => v.Name.Equals("Shared Vessel A") || v.Name.Equals("Shared Vessel B"))
                                                                            .ToListAsync();

            var vessels = new List<Vessel>
            {
                new Vessel { Name = "Shared Vessel A", VesselNumber = "V12345" },
                new Vessel { Name = "Shared Vessel B", VesselNumber = "V67890" }
            };

            await dbContext.Vessels.AddRangeAsync(vessels);
            await dbContext.SaveChangesAsync();
            return vessels;
        }

        private static async Task AssignVesselsToUsers(ApplicationDbContext dbContext, List<Vessel> vessels, string[] userIds)
        {
            var userVesselEntries = new List<UserVessel>();

            foreach (var userId in userIds)
            {
                foreach (var vessel in vessels)
                {
                    if (!await dbContext.UserVessels.AnyAsync(uv => uv.UserId == userId && uv.VesselId == vessel.Id))
                    {
                        userVesselEntries.Add(new UserVessel { UserId = userId, VesselId = vessel.Id });
                    }
                }
            }

            if (userVesselEntries.Any())
            {
                await dbContext.UserVessels.AddRangeAsync(userVesselEntries);
                await dbContext.SaveChangesAsync();
            }
        }

        private static async Task LinkVesselsToRoutes(ApplicationDbContext dbContext, int vesselId)
        {
            var vesselRoutes = await dbContext.Routes
                .Where(route => !dbContext.VesselRoutes.Any(vr => vr.VesselId == vesselId && vr.RouteId == route.Id))
                .Select(route => new VesselRoute { VesselId = vesselId, RouteId = route.Id })
                .ToListAsync();

            if (vesselRoutes.Any())
            {
                await dbContext.VesselRoutes.AddRangeAsync(vesselRoutes);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
