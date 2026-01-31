using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Gcet.Models;
using Gcet.Services;
using Gcet.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gcet.Controllers
{
    /// <summary>
    /// Handles registration, login, and logout flows with hardened error handling.
    /// </summary>
    [AllowAnonymous]
    public class AuthenticationController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(IUserService userService, ILogger<AuthenticationController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _userService.UsernameExistsAsync(model.Username))
            {
                ModelState.AddModelError(string.Empty, "Registration failed.");
                _logger.LogWarning("Attempt to register duplicate username: {Username}", model.Username);
                return View(model);
            }

            if (await _userService.EmailExistsAsync(model.Email))
            {
                ModelState.AddModelError(string.Empty, "Registration failed.");
                _logger.LogWarning("Attempt to register duplicate email: {Email}", model.Email);
                return View(model);
            }

            var user = new User
            {
                Username = model.Username.Trim(),
                Email = model.Email.Trim(),
                Role = "User",
                IsActive = true
            };

            await _userService.CreateAsync(user, model.Password);

            TempData["SuccessMessage"] = "Account created. Please sign in.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userService.GetByUsernameOrEmailAsync(model.UsernameOrEmail);
            if (user == null)
            {
                await Task.Delay(200); // Blunt timing attacks.
                ModelState.AddModelError(string.Empty, "Invalid credentials.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Invalid credentials.");
                return View(model);
            }

            if (user.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account temporarily locked. Try again later.");
                return View(model);
            }

            if (!await _userService.ValidatePasswordAsync(user, model.Password))
            {
                await _userService.RecordFailedLoginAsync(user);
                ModelState.AddModelError(string.Empty, "Invalid credentials.");
                return View(model);
            }

            await _userService.RecordSuccessfulLoginAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            if (user.PasswordChangedAt == null)
            {
                claims.Add(new Claim("MustChangePassword", "true"));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                AllowRefresh = true,
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            if (claims.Any(c => c.Type == "MustChangePassword"))
            {
                return RedirectToAction("ChangePassword", "Account");
            }

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                ? RedirectToAction("Dashboard", "Admin")
                : RedirectToAction("Dashboard", "User");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
