namespace VoyageEnergyAdvisor.Data.Extensions
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using VoyageEnergyAdvisor.Data.Entities;

    public static class UserSeedExtensions
    {
        public static async Task SeedUsersAndRoles(this IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure roles exist
            var roles = new[] { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    Console.WriteLine($"✅ Role '{role}' created.");
                }
            }

            // Admin User
            var adminUser = await CreateUserAsync(userManager, "Admin", "admin@example.com", "Admin@123");
            await AssignRoleAsync(userManager, adminUser, "Admin");

            // Regular User
            var normalUser = await CreateUserAsync(userManager, "Regular", "user@example.com", "User@123");
            await AssignRoleAsync(userManager, normalUser, "User");

        }

        private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager, string userName , string email, string password)
        {
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser != null) return existingUser;

            var user = new ApplicationUser { UserName = userName, Email = email, FullName = userName };
            var result = await userManager.CreateAsync(user, password);

            Console.WriteLine(result.Succeeded
                ? $"✅ User '{email}' created successfully."
                : $"❌ Failed to create user '{email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");

            return user;
        }

        private static async Task AssignRoleAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, string role)
        {
            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
                Console.WriteLine($"✅ User '{user.Email}' assigned role '{role}'.");
            }
        }
    }
}
