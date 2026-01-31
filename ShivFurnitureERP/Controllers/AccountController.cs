using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.ViewModels;

namespace ShivFurnitureERP.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var redirect = await RedirectAuthenticatedUserAsync(returnUrl);
            if (redirect is not null)
            {
                return redirect;
            }
        }

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await FindUserAsync(model.LoginId);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
        var isPortalUser = roles.Contains("PortalUser", StringComparer.OrdinalIgnoreCase);

        if (!isAdmin && !isPortalUser)
        {
            ModelState.AddModelError(string.Empty, "You do not have access to the portal or admin area.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            _logger.LogInformation("User {LoginId} signed in.", user.LoginId);

            if (user.MustChangePassword && isPortalUser)
            {
                TempData["ForcePasswordChangeMessage"] = "For security, please set a new password before continuing.";
                return RedirectToAction("ForcePasswordChange", "Account", new { area = "Portal", returnUrl = model.ReturnUrl });
            }

            return RedirectAfterLogin(model.ReturnUrl, isAdmin, isPortalUser);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var displayName = string.IsNullOrWhiteSpace(User.Identity?.Name) ? user.LoginId : User.Identity!.Name!;

        var model = new ProfileViewModel
        {
            DisplayName = displayName,
            LoginId = user.LoginId,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles.ToList()
        };

        return View(model);
    }

    private IActionResult RedirectAfterLogin(string? returnUrl, bool isAdmin, bool isPortalUser)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            if (IsReturnUrlCompatible(returnUrl, isAdmin, isPortalUser))
            {
                return Redirect(returnUrl);
            }
        }

        if (isAdmin)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        if (isPortalUser)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Portal" });
        }

        return RedirectToAction("Index", "Home");
    }

    private async Task<IActionResult?> RedirectAuthenticatedUserAsync(string? returnUrl)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            await _signInManager.SignOutAsync();
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
        var isPortalUser = roles.Contains("PortalUser", StringComparer.OrdinalIgnoreCase);

        if (IsReturnUrlCompatible(returnUrl, isAdmin, isPortalUser))
        {
            return Redirect(returnUrl!);
        }

        if (isAdmin)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        if (isPortalUser)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Portal" });
        }

        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    private bool IsReturnUrlCompatible(string? returnUrl, bool isAdmin, bool isPortalUser)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            return false;
        }

        var isAdminUrl = returnUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase);
        var isPortalUrl = returnUrl.StartsWith("/Portal", StringComparison.OrdinalIgnoreCase);

        if (isAdminUrl && !isAdmin)
        {
            return false;
        }

        if (isPortalUrl && !isPortalUser)
        {
            return false;
        }

        return true;
    }

    private async Task<ApplicationUser?> FindUserAsync(string? credential)
    {
        if (string.IsNullOrWhiteSpace(credential))
        {
            return null;
        }

        var trimmed = credential.Trim();
        var normalizedLoginId = trimmed.ToUpperInvariant();
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.LoginId.ToUpper() == normalizedLoginId);
        if (user is not null)
        {
            return user;
        }

        if (trimmed.Contains('@', StringComparison.Ordinal))
        {
            return await _userManager.FindByEmailAsync(trimmed);
        }

        return null;
    }
}
