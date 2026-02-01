using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.Services;

namespace ShivFurnitureERP.Areas.Portal.Controllers;

[Area("Portal")]
[Authorize(Policy = "PortalOnly")]
public class SalesOrdersController : Controller
{
    private readonly ISalesOrderService _salesOrderService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SalesOrdersController> _logger;

    public SalesOrdersController(
        ISalesOrderService salesOrderService,
        UserManager<ApplicationUser> userManager,
        ILogger<SalesOrdersController> logger)
    {
        _salesOrderService = salesOrderService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var contactId = await GetContactIdAsync();
        if (contactId is null)
        {
            return View(new List<SalesOrder>());
        }

        var allOrders = await _salesOrderService.GetOrdersAsync(null, null, cancellationToken);
        var customerOrders = allOrders
            .Where(so => so.CustomerId == contactId.Value)
            .OrderByDescending(so => so.SODate)
            .ToList();

        return View(customerOrders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var contactId = await GetContactIdAsync();
        if (contactId is null)
        {
            return NotFound();
        }

        var order = await _salesOrderService.GetByIdAsync(id, cancellationToken);
        if (order is null || order.CustomerId != contactId.Value)
        {
            return NotFound();
        }

        return View(order);
    }

    private async Task<int?> GetContactIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.ContactId;
    }
}
