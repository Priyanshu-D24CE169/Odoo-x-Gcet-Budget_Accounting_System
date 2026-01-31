using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Services;


public class IdentityInitializer
{
    public static async Task SeedRolesAndUsersAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        // Create roles
        foreach (var role in UserRoles.AllRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Create default admin user
        var adminLoginId = "sysadmin";
        var adminUser = await userManager.Users.FirstOrDefaultAsync(u => u.LoginId == adminLoginId);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                LoginId = adminLoginId,
                UserName = "admin@shivfurniture.com",
                Email = "admin@shivfurniture.com",
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator",
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@1234");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
            }
        }
        else if (!await userManager.IsInRoleAsync(adminUser, UserRoles.Admin))
        {
            await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
        }
    }
}
