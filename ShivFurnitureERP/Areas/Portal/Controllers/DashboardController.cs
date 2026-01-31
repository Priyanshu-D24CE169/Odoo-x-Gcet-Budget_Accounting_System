using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ShivFurnitureERP.Areas.Portal.Controllers;

[Area("Portal")]
[Authorize(Policy = "PortalOnly")]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
