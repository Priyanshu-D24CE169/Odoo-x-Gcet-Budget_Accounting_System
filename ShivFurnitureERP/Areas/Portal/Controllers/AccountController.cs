using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShivFurnitureERP.Infrastructure;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.ViewModels;

namespace ShivFurnitureERP.Areas.Portal.Controllers;

[Area("Portal")]
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
    public IActionResult Login(string? returnUrl = null)
    {
        return RedirectToAction("Login", "Account", new { area = string.Empty, returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginViewModel model)
    {
        var returnUrl = model.ReturnUrl;
        return RedirectToAction("Login", "Account", new { area = string.Empty, returnUrl });
    }

    [Authorize(Policy = "PortalOnly")]
    [HttpGet]
    public async Task<IActionResult> ForcePasswordChange(string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        if (!user.MustChangePassword)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Portal" });
        }

        return View(new ForcePasswordChangeViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [Authorize(Policy = "PortalOnly")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForcePasswordChange(ForcePasswordChangeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        if (!user.MustChangePassword)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Portal" });
        }

        var changeResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!changeResult.Succeeded)
        {
            foreach (var error in changeResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);
        await _signInManager.RefreshSignInAsync(user);
        _logger.LogInformation("Portal user {LoginId} completed the forced password reset.", user.LoginId);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Dashboard", new { area = "Portal" });
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("PortalUser"))
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new RegisterViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email is already registered.");
            return View(model);
        }

        var loginId = IdentitySeeder.GenerateLoginId("PORTAL");
        var user = new ApplicationUser
        {
            UserName = loginId,
            LoginId = loginId,
            Email = model.Email,
			EmailConfirmed = true,
			MustChangePassword = false
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "PortalUser");
        if (!roleResult.Succeeded)
        {
            foreach (var error in roleResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await _userManager.DeleteAsync(user);
            return View(model);
        }

        _logger.LogInformation("Portal user {Email} registered with LoginId {LoginId}.", model.Email, loginId);

        await _signInManager.SignInAsync(user, false);
        return RedirectToLocal(model.ReturnUrl);
    }

    [Authorize(Policy = "PortalOnly")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account", new { area = string.Empty });
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard", new { area = "Portal" });
    }

}
