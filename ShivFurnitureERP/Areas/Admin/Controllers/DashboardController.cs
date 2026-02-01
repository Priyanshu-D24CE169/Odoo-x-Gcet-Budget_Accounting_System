using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShivFurnitureERP.Services;
using ShivFurnitureERP.ViewModels;

namespace ShivFurnitureERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateTime? start, DateTime? end, int? accountId, CancellationToken cancellationToken)
    {
        DashboardViewModel model = await _dashboardService.GetDashboardAsync(start, end, accountId, cancellationToken);
        return View(model);
    }
}
