namespace VoyageEnergyCalculatorService.Test.Data
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using System.Security.Claims;
    using VoyageEnergyAdvisor.Data.Entities;
    using VoyageEnergyAdvisor.Data;
    using Microsoft.EntityFrameworkCore;

    public static class TestHelper
    {
        public static IServiceProvider BuildTestServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();

            services.AddLogging();
            services.AddHttpContextAccessor();

            return services.BuildServiceProvider();
        }

        public static void SetHttpContextUser(IHttpContextAccessor accessor, string userId = "test-id", string userName = "testuser")
        {
            accessor.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userName)
            }, "TestAuth"))
            };
        }

        public static IHttpContextAccessor CreateAccessorWithUser(ApplicationUser user)
        {
            var accessor = new HttpContextAccessor();
            accessor.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName ?? "TestUser")
                 }, "TestAuth"))
            };
            return accessor;
        }

        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = new[] { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }

}
