using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.Services;
using ShivFurnitureERP.ViewModels.AdminUsers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ShivFurnitureERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UsersController> _logger;
    private readonly ApplicationDbContext _dbContext;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        IEmailNotificationService emailNotificationService,
        IConfiguration configuration,
        ILogger<UsersController> logger,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _emailNotificationService = emailNotificationService;
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.LoginId)
            .ToListAsync();

        var items = new List<UserListItemViewModel>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            items.Add(new UserListItemViewModel
            {
                Id = user.Id,
                LoginId = user.LoginId,
                DisplayName = string.IsNullOrWhiteSpace(user.FullName) ? user.LoginId : user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "PortalUser",
                MustChangePassword = user.MustChangePassword
            });
        }

        return View(items);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(PrepareModel(new CreateUserViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(PrepareModel(model));
        }

        var trimmedLoginId = model.LoginId.Trim();
        var trimmedEmail = model.Email.Trim();

        if (await _userManager.Users.AnyAsync(u => u.LoginId == trimmedLoginId, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.LoginId), "Login ID already exists.");
        }

        if (await _userManager.FindByEmailAsync(trimmedEmail) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email is already registered.");
        }

        if (!ModelState.IsValid)
        {
            return View(PrepareModel(model));
        }

        var user = new ApplicationUser
        {
            LoginId = trimmedLoginId,
            UserName = trimmedLoginId,
            Email = trimmedEmail,
            EmailConfirmed = true,
			FullName = model.FullName.Trim(),
			MustChangePassword = true
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(PrepareModel(model));
        }

        var selectedRole = string.Equals(model.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "PortalUser";
        var roleResult = await _userManager.AddToRoleAsync(user, selectedRole);
        if (!roleResult.Succeeded)
        {
            foreach (var error in roleResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await _userManager.DeleteAsync(user);
            return View(PrepareModel(model));
        }

        if (selectedRole == "PortalUser")
        {
            var contact = new Contact
            {
                Name = user.FullName ?? user.LoginId,
                Email = user.Email!,
                Type = ContactType.Customer,
                CreatedOn = DateTime.UtcNow,
                IsArchived = false
            };

            _dbContext.Contacts.Add(contact);
            await _dbContext.SaveChangesAsync(cancellationToken);

            user.ContactId = contact.ContactId;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Created Contact {ContactId} with Type=Customer for Portal user {LoginId}.", contact.ContactId, user.LoginId);
        }

        var loginUrl = GetLoginUrl(selectedRole);
        try
        {
            await _emailNotificationService.SendContactInviteAsync(user.Email!, user.LoginId, model.Password, loginUrl, cancellationToken);
            _logger.LogInformation("Invite email sent to {Email} for user {LoginId}.", user.Email, user.LoginId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invite email to {Email} for user {LoginId}. User created successfully but email notification failed.", user.Email, user.LoginId);
            TempData["WarningMessage"] = $"User {user.LoginId} created successfully, but email notification failed. Please contact the user manually.";
        }

        if (!TempData.ContainsKey("WarningMessage"))
        {
            TempData["SuccessMessage"] = $"User {user.LoginId} created successfully and invite email sent.";
        }

        _logger.LogInformation("User {LoginId} created with role {Role}.", user.LoginId, selectedRole);

        return RedirectToAction(nameof(Create));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var model = new EditUserViewModel
        {
            Id = user.Id,
            LoginId = user.LoginId,
            FullName = user.FullName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? "PortalUser",
            MustChangePassword = user.MustChangePassword
        };

        return View(PrepareModel(model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(PrepareModel(model));
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user is null)
        {
            return NotFound();
        }

        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim();
        user.MustChangePassword = model.MustChangePassword;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(PrepareModel(model));
        }

        var desiredRole = string.Equals(model.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "PortalUser";
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(desiredRole, StringComparer.OrdinalIgnoreCase))
        {
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    foreach (var error in removeResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return View(PrepareModel(model));
                }
            }

            var addResult = await _userManager.AddToRoleAsync(user, desiredRole);
            if (!addResult.Succeeded)
            {
                foreach (var error in addResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(PrepareModel(model));
            }
        }

        TempData["SuccessMessage"] = $"User {user.LoginId} updated successfully.";
        return RedirectToAction(nameof(Index));
    }

	private CreateUserViewModel PrepareModel(CreateUserViewModel model)
	{
		model.AvailableRoles = BuildRoleSelectList(model.Role);
		return model;
	}

	private EditUserViewModel PrepareModel(EditUserViewModel model)
	{
		model.AvailableRoles = BuildRoleSelectList(model.Role);
		return model;
	}

	private IEnumerable<SelectListItem> BuildRoleSelectList(string? selectedRole)
	{
		var normalized = string.Equals(selectedRole, "Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "PortalUser";
		return new[]
		{
			new SelectListItem("Admin", "Admin", normalized == "Admin"),
			new SelectListItem("Portal", "PortalUser", normalized == "PortalUser")
		};
	}

    private string GetLoginUrl(string role)
    {
        return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
            ? (_configuration["Admin:LoginUrl"] ?? "/Admin/Account/Login")
            : (_configuration["Portal:LoginUrl"] ?? "/Portal/Account/Login");
    }
}
