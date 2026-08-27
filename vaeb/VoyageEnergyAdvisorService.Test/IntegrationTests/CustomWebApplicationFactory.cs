using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VoyageEnergyAdvisor.Data;
using VoyageEnergyAdvisor.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace VoyageEnergyAdvisorService.Test.IntegrationTests
{
    /// <summary>
    /// Custom WebApplicationFactory for integration tests.
    /// Configures test environment with in-memory database and overrides services as needed.
    /// For testing purposes, we reference the actual Program class from VoyageEnergyAdvisor.App
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<VoyageEnergyAdvisor.Program>
    {
        // Note: Requires InternalsVisibleTo in VoyageEnergyAdvisor.App.csproj
        // to access Program class for WebApplicationFactory<Program>
        
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app's DbContext registration(s)
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<DbContextOptions>();

                // EF Core 10 requires a single DB provider per IServiceProvider.
                // Remove all previously registered EF Core provider services (SqlServer)
                // to avoid "Services for database providers ... have been registered" error.
                var efCoreDescriptors = services
                    .Where(d =>
                        (d.ServiceType?.FullName?.StartsWith("Microsoft.EntityFrameworkCore") ?? false)
                        || (d.ImplementationType?.FullName?.StartsWith("Microsoft.EntityFrameworkCore") ?? false))
                    .ToList();

                foreach (var desc in efCoreDescriptors)
                {
                    services.Remove(desc);
                }

                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryIntegrationTestDb");
                });

                // Build service provider
                var sp = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database context
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();

                    // Ensure the database is created
                    db.Database.EnsureCreated();

                    // Seed test data if needed
                    SeedTestData(db);
                }
            });

            builder.UseEnvironment("Testing");
        }

        private void SeedTestData(ApplicationDbContext context)
        {
            // Seed test users, vessels, routes, etc.
            // This runs once per test class using this factory
            
            // Example: Add test user with ASP.NET Identity
            if (!context.Users.Any())
            {
                var testUser = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = "testuser",
                    Email = "test@example.com",
                    EmailConfirmed = true,
                    NormalizedUserName = "TESTUSER",
                    NormalizedEmail = "TEST@EXAMPLE.COM",
                    SecurityStamp = Guid.NewGuid().ToString(),
                    FullName = "Test User"
                };

                // Use PasswordHasher to create a valid password hash
                var passwordHasher = new PasswordHasher<ApplicationUser>();
                testUser.PasswordHash = passwordHasher.HashPassword(testUser, "testpassword123");

                context.Users.Add(testUser);
                
                // Add test vessel
                var testVessel = new Vessel
                {
                    Id = 1,
                    Name = "Test Vessel",
                    VesselNumber = "TEST001"
                };
                context.Vessels.Add(testVessel);
                
                // Link user to vessel
                var userVessel = new UserVessel
                {
                    UserId = testUser.Id,
                    VesselId = testVessel.Id,
                    User = testUser,
                    Vessel = testVessel
                };
                context.UserVessels.Add(userVessel);
                
                context.SaveChanges();
            }
        }
    }
}
