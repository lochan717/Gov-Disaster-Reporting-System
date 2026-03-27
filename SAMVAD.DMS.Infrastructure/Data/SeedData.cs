using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SAMVAD.DMS.Domain.Entities;

namespace SAMVAD.DMS.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureRoleAsync(roleManager, "SuperAdmin");
        await EnsureRoleAsync(roleManager, "DistrictAdmin");

        var superAdminEmail = "superadmin@samvad.gov.in";
        var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);

        if (superAdmin is null)
        {
            superAdmin = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                FullName = "System Administrator",
                RoleLabel = "Super Admin",
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(superAdmin, "Admin@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
            }
        }

    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}