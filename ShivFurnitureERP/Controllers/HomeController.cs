using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.Services;

namespace ShivFurnitureERP.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IDashboardService dashboardService, ILogger<HomeController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var dashboard = await _dashboardService.GetDashboardAsync(null, null, null, cancellationToken);
            return View(dashboard);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View("~/Views/Shared/AccessDenied.cshtml");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var errorViewModel = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
            return View(errorViewModel);
        }
    }
}
