using HRM.Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HRMBackend.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            // Ensure roles
            string[] roles = new[] { "HR", "Candidate" };
            foreach (var r in roles)
            {
                if (!await roleManager.RoleExistsAsync(r))
                    await roleManager.CreateAsync(new IdentityRole(r));
            }

            // OPTIONAL: seed an initial HR user for testing
            var adminEmail = "hr@xyzcorp.com";
            var existing = await userManager.FindByEmailAsync(adminEmail);
            if (existing == null)
            {
                var user = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "HR Admin"
                };
                var result = await userManager.CreateAsync(user, "HrAdmin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "HR");
                }
            }
        }
    }
}
