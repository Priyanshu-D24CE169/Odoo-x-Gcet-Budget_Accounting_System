using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.Services;

namespace ShivFurnitureERP.Areas.Portal.Controllers;

[Area("Portal")]
[Authorize(Policy = "PortalOnly")]
public class PurchaseOrdersController : Controller
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PurchaseOrdersController> _logger;

    public PurchaseOrdersController(
        IPurchaseOrderService purchaseOrderService,
        UserManager<ApplicationUser> userManager,
        ILogger<PurchaseOrdersController> logger)
    {
        _purchaseOrderService = purchaseOrderService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var contactId = await GetContactIdAsync();
        if (contactId is null)
        {
            return View(new List<PurchaseOrder>());
        }

        var allOrders = await _purchaseOrderService.GetOrdersAsync(null, null, cancellationToken);
        var vendorOrders = allOrders
            .Where(po => po.VendorId == contactId.Value)
            .OrderByDescending(po => po.PODate)
            .ToList();

        return View(vendorOrders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var contactId = await GetContactIdAsync();
        if (contactId is null)
        {
            return NotFound();
        }

        var order = await _purchaseOrderService.GetByIdAsync(id, cancellationToken);
        if (order is null || order.VendorId != contactId.Value)
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
