using System.Security.Claims;
using System.Threading.Tasks;
using Gcet.Services;
using Gcet.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gcet.Controllers
{
    /// <summary>
    /// End-user dashboard protected by the User role.
    /// </summary>
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Authentication");
            }

            var user = await _userService.GetByIdAsync(int.Parse(userId));
            if (user == null)
            {
                return RedirectToAction("Login", "Authentication");
            }

            var model = new UserDashboardViewModel
            {
                Username = user.Username,
                LastLoginAt = user.LastLoginAt
            };

            return View(model);
        }
    }
}
