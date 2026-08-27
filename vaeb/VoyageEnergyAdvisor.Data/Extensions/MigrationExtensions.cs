namespace VoyageEnergyAdvisor.Data.Extensions
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;

    public static class MigrationExtensions
    {
        public static async Task ApplyMigrations(this IApplicationBuilder app, WebApplicationBuilder builder)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                Console.WriteLine("🔍 Checking database existence and applying migrations...");

                if (!dbContext.Database.CanConnect())
                {
                    Console.WriteLine("⚠️ Database does not exist. Creating a new database...");
                    dbContext.Database.EnsureCreated();
                    Console.WriteLine("✅ Database created successfully.");
                }

                await dbContext.Database.MigrateAsync();
                Console.WriteLine("✅ Database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ An error occurred while applying database migrations: {ex.Message}");
            }

            var defaultResourcesPath = GetDefaultResourcesPath(builder);
            await serviceProvider.SeedUsersAndRoles();
            await serviceProvider.SeedRoutes(defaultResourcesPath);
            await serviceProvider.SeedVessels();
            await serviceProvider.SeedConfigurations(defaultResourcesPath);
        }

        private static string GetDefaultResourcesPath(WebApplicationBuilder builder)
        {
            var isRunningInDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
            var contentRootPath = builder.Environment.ContentRootPath;

            return isRunningInDocker
                ? Path.Combine(contentRootPath, "DefaultResources")
                : Path.Combine(contentRootPath, "..", "VoyageEnergyAdvisor.Core", "DefaultResources");
        }
    }
}
