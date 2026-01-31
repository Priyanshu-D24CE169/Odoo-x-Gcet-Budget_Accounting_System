using System.Security.Claims;
using System.Threading.Tasks;
using Gcet.Services;
using Gcet.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gcet.Controllers
{
    /// <summary>
    /// Handles authenticated user actions such as password changes.
    /// </summary>
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            var model = new ChangePasswordViewModel
            {
                RequiresPasswordChange = User.HasClaim(c => c.Type == "MustChangePassword")
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.RequiresPasswordChange = User.HasClaim(c => c.Type == "MustChangePassword");
                return View(model);
            }

            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Authentication");
            }

            if (!await _userService.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword))
            {
                ModelState.AddModelError(string.Empty, "Password change failed.");
                return View(model);
            }

            // Force re-login to refresh claims and session state.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "Password updated. Please sign in again.";
            return RedirectToAction("Login", "Authentication");
        }

        private async Task<Models.User?> GetCurrentUserAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await _userService.GetByIdAsync(int.Parse(userId));
        }
    }
}
