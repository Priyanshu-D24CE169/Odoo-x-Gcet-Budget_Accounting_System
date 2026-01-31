using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Gcet.Models;
using Gcet.Services;
using Gcet.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gcet.Controllers
{
    /// <summary>
    /// Administrative endpoints for managing users and enforcing role-based security.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IUserService userService, IEmailService emailService, ILogger<AdminController> logger)
        {
            _userService = userService;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var stats = await _userService.GetDashboardMetricsAsync();
            var model = new AdminDashboardViewModel
            {
                TotalUsers = stats.TotalUsers,
                ActiveUsers = stats.ActiveUsers,
                InactiveUsers = stats.InactiveUsers,
                LockedUsers = stats.LockedUsers
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> UsersList(string? search, int page = 1)
        {
            const int pageSize = 10;
            page = page < 1 ? 1 : page;

            var (users, totalCount) = await _userService.SearchAsync(search, page, pageSize);
            var model = new UsersListViewModel
            {
                Search = search,
                PageNumber = page,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Users = users.Select(u => new UserManagementViewModel
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    FailedLoginAttempts = u.FailedLoginAttempts,
                    IsLockedOut = u.IsLockedOut,
                    LockoutEnd = u.LockoutEnd
                }).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View(new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _userService.UsernameExistsAsync(model.Username))
            {
                ModelState.AddModelError(string.Empty, "Username already exists.");
                return View(model);
            }

            if (await _userService.EmailExistsAsync(model.Email))
            {
                ModelState.AddModelError(string.Empty, "Email already exists.");
                return View(model);
            }

            var temporaryPassword = GenerateTemporaryPassword();
            var temporaryUserId = GenerateTemporaryUserId();
            var user = new User
            {
                Username = model.Username.Trim(),
                Email = model.Email.Trim(),
                Role = model.Role,
                IsActive = true
            };

            await _userService.CreateAsync(user, temporaryPassword, markPasswordChanged: false);
            await _emailService.SendLoginDetailsAsync(user.Email, user.Username, user.Username, temporaryUserId, temporaryPassword);
            _logger.LogInformation("Admin {Admin} created user {User}", User.Identity?.Name, user.Username);

            TempData["SuccessMessage"] = "User created and credentials emailed.";
            return RedirectToAction(nameof(UsersList));
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _userService.UpdateUserRoleAsync(model.Id, model.Role);
            await _userService.ToggleUserStatusAsync(model.Id, model.IsActive);
            _logger.LogInformation("Admin {Admin} updated user {User}", User.Identity?.Name, model.Username);

            TempData["SuccessMessage"] = "User updated successfully.";
            return RedirectToAction(nameof(UsersList));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            await _userService.SoftDeleteUserAsync(id);
            _logger.LogInformation("Admin {Admin} soft-deleted user {UserId}", User.Identity?.Name, id);
            TempData["SuccessMessage"] = "User disabled.";
            return RedirectToAction(nameof(UsersList));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, bool isActive)
        {
            await _userService.ToggleUserStatusAsync(id, isActive);
            TempData["SuccessMessage"] = "User status updated.";
            return RedirectToAction(nameof(UsersList));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int id, string role)
        {
            await _userService.UpdateUserRoleAsync(id, role);
            TempData["SuccessMessage"] = "User role updated.";
            return RedirectToAction(nameof(UsersList));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(int id)
        {
            await _userService.UnlockUserAsync(id);
            TempData["SuccessMessage"] = "User unlocked.";
            return RedirectToAction(nameof(UsersList));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var tempPassword = await _userService.ResetPasswordAsync(id);
            TempData["SuccessMessage"] = $"Temporary password: {tempPassword}. User must change at next login.";
            return RedirectToAction(nameof(UsersList));
        }

        private static string GenerateTemporaryPassword()
        {
            const string allowed = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789@$!%*?&";
            var randomBytes = new byte[16];
            RandomNumberGenerator.Fill(randomBytes);
            var chars = new char[randomBytes.Length];
            for (var i = 0; i < randomBytes.Length; i++)
            {
                chars[i] = allowed[randomBytes[i] % allowed.Length];
            }

            return new string(chars);
        }

        private static string GenerateTemporaryUserId()
        {
            return $"TMP-{Guid.NewGuid():N}".Substring(0, 12).ToUpperInvariant();
        }
    }
}
