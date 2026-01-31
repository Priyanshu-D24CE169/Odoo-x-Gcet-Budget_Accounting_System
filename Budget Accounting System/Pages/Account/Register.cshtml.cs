using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Models;
using Budget_Accounting_System.Data;
using System.ComponentModel.DataAnnotations;

namespace Budget_Accounting_System.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RegisterModel> _logger;
    private readonly ApplicationDbContext _context;

    public RegisterModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<RegisterModel> logger,
        ApplicationDbContext context)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _context = context;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Login ID is required")]
        [StringLength(12, MinimumLength = 6, ErrorMessage = "Login ID must be 6-12 characters")]
        [Display(Name = "Login ID")]
        public string LoginId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a role")]
        [Display(Name = "I am a")]
        public string Role { get; set; } = string.Empty;

        [Display(Name = "Link to existing contact (optional)")]
        public int? ContactId { get; set; }
    }

    public List<Contact> AvailableContacts { get; set; } = new();

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        await LoadContactsAsync();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        await LoadContactsAsync();

        if (ModelState.IsValid)
        {
            // Check if LoginId already exists
            var existingLoginId = await _userManager.Users
                .AnyAsync(u => u.LoginId == Input.LoginId);
            if (existingLoginId)
            {
                ModelState.AddModelError(string.Empty, "This Login ID is already taken. Please choose another.");
                return Page();
            }

            // Check if Email already exists
            var existingEmail = await _userManager.FindByEmailAsync(Input.Email);
            if (existingEmail != null)
            {
                ModelState.AddModelError(string.Empty, "This email address is already registered.");
                return Page();
            }

            // Validate role
            if (Input.Role != UserRoles.Admin && Input.Role != UserRoles.PortalUser)
            {
                ModelState.AddModelError(string.Empty, "Invalid role selected.");
                return Page();
            }

            // Create new user
            var user = new ApplicationUser
            {
                LoginId = Input.LoginId,
                UserName = Input.Email,
                Email = Input.Email,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                ContactId = Input.ContactId,
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                // Add user to selected role
                await _userManager.AddToRoleAsync(user, Input.Role);

                // Sign in the user
                await _signInManager.SignInAsync(user, isPersistent: false);

                // Redirect based on role
                if (Input.Role == UserRoles.Admin)
                {
                    return RedirectToPage("/Dashboard", new { area = "Admin" });
                }
                else
                {
                    return RedirectToPage("/Dashboard", new { area = "Portal" });
                }
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return Page();
    }

    private async Task LoadContactsAsync()
    {
        AvailableContacts = await _context.Contacts
            .Where(c => c.IsActive && c.User == null)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}
