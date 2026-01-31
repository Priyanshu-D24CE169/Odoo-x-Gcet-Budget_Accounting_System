using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Infrastructure;

public static class IdentitySeeder
{
    private static readonly string[] Roles = ["Admin", "PortalUser"];
    private const string DefaultAdminEmail = "admin@shivfurniture.local";
    private const string DefaultAdminPassword = "Admin@12345";
    private const string RecoveryAdminLoginId = "ADMINRESET01";
    private const string RecoveryAdminEmail = "admin.reset@shivfurniture.local";
    private const string RecoveryAdminPassword = "Admin@Reset123";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;
        var roleManager = scopedProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scopedProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scopedProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to create role {Role}: {Errors}", role, string.Join(',', result.Errors.Select(e => e.Description)));
                }
            }
        }

        var adminUser = await userManager.FindByEmailAsync(DefaultAdminEmail);
        if (adminUser == null)
        {
            var loginId = GenerateLoginId("ADMIN");

            adminUser = new ApplicationUser
            {
                UserName = loginId,
                Email = DefaultAdminEmail,
                LoginId = loginId,
				EmailConfirmed = true,
				FullName = "System Administrator",
				MustChangePassword = false
            };

            var createResult = await userManager.CreateAsync(adminUser, DefaultAdminPassword);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create default admin user: {Errors}", string.Join(',', createResult.Errors.Select(e => e.Description)));
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        await EnsureRecoveryAdminAsync(userManager, logger);

        // Create a new login and user for friend_login
        var friendLogin = "friend_login";
        var friendPassword = "Strong@123";
        var friendEmail = "friend@shivfurniture.local";

        var friendUser = await userManager.FindByEmailAsync(friendEmail);
        if (friendUser == null)
        {
            var friendLoginId = GenerateLoginId("FRIEND");

            friendUser = new ApplicationUser
            {
                UserName = friendLoginId,
                Email = friendEmail,
                LoginId = friendLoginId,
				EmailConfirmed = true,
				FullName = "Friend Portal User",
				MustChangePassword = false
            };

            var createFriendResult = await userManager.CreateAsync(friendUser, friendPassword);
            if (!createFriendResult.Succeeded)
            {
                logger.LogError("Failed to create friend user: {Errors}", string.Join(',', createFriendResult.Errors.Select(e => e.Description)));
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(friendUser, "PortalUser"))
        {
            await userManager.AddToRoleAsync(friendUser, "PortalUser");
        }
    }

    internal static string GenerateLoginId(string prefix)
    {
        var normalizedPrefix = new string(prefix.Where(char.IsLetterOrDigit).ToArray());
        var randomSegment = Guid.NewGuid().ToString("N").ToUpperInvariant()[..6];
        return (normalizedPrefix + randomSegment).Length > 12
            ? (normalizedPrefix + randomSegment).Substring(0, 12)
            : (normalizedPrefix + randomSegment).PadRight(12, '0');
    }

    private static async Task EnsureRecoveryAdminAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        var recoveryUser = await userManager.Users.FirstOrDefaultAsync(u => u.LoginId == RecoveryAdminLoginId)
            ?? await userManager.FindByEmailAsync(RecoveryAdminEmail);

        if (recoveryUser is null)
        {
            recoveryUser = new ApplicationUser
            {
                UserName = RecoveryAdminLoginId,
                LoginId = RecoveryAdminLoginId,
                Email = RecoveryAdminEmail,
				EmailConfirmed = true,
				FullName = "Recovery Administrator",
				MustChangePassword = false
            };

            var createResult = await userManager.CreateAsync(recoveryUser, RecoveryAdminPassword);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create recovery admin user: {Errors}", string.Join(',', createResult.Errors.Select(e => e.Description)));
                return;
            }
        }
        else
        {
            var requiresUpdate = false;
            if (!string.Equals(recoveryUser.LoginId, RecoveryAdminLoginId, StringComparison.Ordinal))
            {
                recoveryUser.LoginId = RecoveryAdminLoginId;
                recoveryUser.UserName = RecoveryAdminLoginId;
                requiresUpdate = true;
            }

            if (!string.Equals(recoveryUser.Email, RecoveryAdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                recoveryUser.Email = RecoveryAdminEmail;
                requiresUpdate = true;
            }

            if (requiresUpdate)
            {
                var updateResult = await userManager.UpdateAsync(recoveryUser);
                if (!updateResult.Succeeded)
                {
                    logger.LogError("Failed to update recovery admin user: {Errors}", string.Join(',', updateResult.Errors.Select(e => e.Description)));
                    return;
                }
            }

            var passwordValid = await userManager.CheckPasswordAsync(recoveryUser, RecoveryAdminPassword);
            if (!passwordValid)
            {
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(recoveryUser);
                var resetResult = await userManager.ResetPasswordAsync(recoveryUser, resetToken, RecoveryAdminPassword);
                if (!resetResult.Succeeded)
                {
                    logger.LogError("Failed to reset password for recovery admin user: {Errors}", string.Join(',', resetResult.Errors.Select(e => e.Description)));
                    return;
                }
            }
        }

        if (!await userManager.IsInRoleAsync(recoveryUser, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(recoveryUser, "Admin");
            if (!roleResult.Succeeded)
            {
                logger.LogError("Failed to assign Admin role to recovery admin user: {Errors}", string.Join(',', roleResult.Errors.Select(e => e.Description)));
                return;
            }
        }

        logger.LogWarning("Recovery admin ready. LoginId: {LoginId}, Password: {Password}", RecoveryAdminLoginId, RecoveryAdminPassword);
    }
}
