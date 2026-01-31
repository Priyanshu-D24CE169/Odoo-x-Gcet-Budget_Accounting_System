using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Models;
using System.ComponentModel.DataAnnotations;

namespace Budget_Accounting_System.Areas.Portal.Pages.Account;


[AllowAnonymous]
[Area("Portal")]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Login ID is required")]
        [StringLength(12, MinimumLength = 6, ErrorMessage = "Login ID must be 6-12 characters")]
        [Display(Name = "Login ID")]
        public string LoginId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        ReturnUrl = returnUrl ?? Url.Page("/Dashboard", new { area = "Portal" });
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Page("/Dashboard", new { area = "Portal" });

        if (ModelState.IsValid)
        {
            // Find user by LoginId
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.LoginId == Input.LoginId);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            // Check if user is active
            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "This account has been deactivated.");
                return Page();
            }

            // Check if user is PortalUser
            if (!await _userManager.IsInRoleAsync(user, UserRoles.PortalUser))
            {
                ModelState.AddModelError(string.Empty, "You are not authorized to access the Portal area.");
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!, Input.Password, Input.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("Portal user {LoginId} logged in.", Input.LoginId);

                // Update last login date
                user.LastLoginDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("Portal user account locked out.");
                ModelState.AddModelError(string.Empty, "Account locked due to too many failed login attempts. Please try again later.");
                return Page();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }
        }

        return Page();
    }
}
