using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.Services;
using ShivFurnitureERP.ViewModels.PurchaseOrders;

namespace ShivFurnitureERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class PurchaseOrdersController : Controller
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IBudgetWarningService _budgetWarningService;
    private readonly IVendorBillService _vendorBillService;

    public PurchaseOrdersController(
        IPurchaseOrderService purchaseOrderService,
        ApplicationDbContext dbContext,
        IBudgetWarningService budgetWarningService,
        IVendorBillService vendorBillService)
    {
        _purchaseOrderService = purchaseOrderService;
        _dbContext = dbContext;
        _budgetWarningService = budgetWarningService;
        _vendorBillService = vendorBillService;
    }

    public async Task<IActionResult> Index(string? search, PurchaseOrderStatus? status, CancellationToken cancellationToken)
    {
        var orders = await _purchaseOrderService.GetOrdersAsync(search, status, cancellationToken);
        var model = orders.Select(order => new PurchaseOrderListItemViewModel
        {
            PurchaseOrderId = order.PurchaseOrderId,
            PONumber = order.PONumber,
            VendorName = order.Vendor?.Name ?? "-",
            PODate = order.PODate,
            Reference = order.Reference,
            Status = order.Status,
            TotalAmount = order.Lines.Sum(l => l.Total)
        }).ToList();

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = await BuildFormViewModelAsync(new PurchaseOrderFormViewModel(), cancellationToken);
        EnsureLinePlaceholders(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseOrderFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync(model, cancellationToken);
            EnsureLinePlaceholders(model);
            await ApplyBudgetWarningsAsync(model, cancellationToken);
            return View(model);
        }

        var order = MapToEntity(model);
        await _purchaseOrderService.CreateAsync(order, cancellationToken);

        TempData["StatusMessage"] = $"Purchase Order {order.PONumber} created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var order = await _purchaseOrderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var model = await BuildFormViewModelAsync(MapToViewModel(order), cancellationToken);
        EnsureLinePlaceholders(model);
        await ApplyBudgetWarningsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PurchaseOrderFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.PurchaseOrderId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync(model, cancellationToken);
            EnsureLinePlaceholders(model);
            await ApplyBudgetWarningsAsync(model, cancellationToken);
            return View(model);
        }

        var order = MapToEntity(model);
        await _purchaseOrderService.UpdateAsync(order, cancellationToken);

        TempData["StatusMessage"] = $"Purchase Order {order.PONumber} updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, CancellationToken cancellationToken)
    {
        await _purchaseOrderService.ConfirmAsync(id, cancellationToken);
        TempData["StatusMessage"] = "Purchase Order confirmed.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        await _purchaseOrderService.CancelAsync(id, cancellationToken);
        TempData["StatusMessage"] = "Purchase Order cancelled.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var order = await _purchaseOrderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var model = await BuildFormViewModelAsync(MapToViewModel(order), cancellationToken);
        await ApplyBudgetWarningsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreateBill(int id, CancellationToken cancellationToken)
    {
        try
        {
            var bill = await _vendorBillService.CreateDraftFromPurchaseOrderAsync(id, cancellationToken);
            TempData["StatusMessage"] = $"Vendor Bill {bill.BillNumber} ready.";
            return RedirectToAction("Edit", "VendorBills", new { area = "Admin", id = bill.VendorBillId });
        }
        catch (Exception ex)
        {
            TempData["StatusMessage"] = ex.Message;
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    private async Task<PurchaseOrderFormViewModel> BuildFormViewModelAsync(PurchaseOrderFormViewModel model, CancellationToken cancellationToken)
    {
        await PopulateSelectListsAsync(model, cancellationToken);
        if (model.Lines.Count == 0)
        {
            model.Lines.Add(new PurchaseOrderLineViewModel());
        }

        return model;
    }

    private async Task PopulateSelectListsAsync(PurchaseOrderFormViewModel model, CancellationToken cancellationToken)
    {
        var vendors = await _dbContext.Contacts
            .Where(c => !c.IsArchived && (c.Type == ContactType.Vendor || c.Type == ContactType.Both))
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem { Text = c.Name, Value = c.ContactId.ToString() })
            .ToListAsync(cancellationToken);

        var products = await _dbContext.Products
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem { Text = p.Name, Value = p.ProductId.ToString() })
            .ToListAsync(cancellationToken);

        var expenseAccountIds = _dbContext.AnalyticalBudgets
            .Where(budget => budget.BudgetType == BudgetType.Expense)
            .Select(budget => budget.AnalyticalAccountId)
            .Distinct();

        var analyticalAccounts = await _dbContext.AnalyticalAccounts
            .Where(account => !account.IsArchived && expenseAccountIds.Contains(account.AnalyticalAccountId))
            .OrderBy(account => account.Name)
            .Select(account => new SelectListItem { Text = account.Name, Value = account.AnalyticalAccountId.ToString() })
            .ToListAsync(cancellationToken);

        model.Vendors = vendors;
        model.Products = products;
        model.AnalyticalAccounts = analyticalAccounts;
    }

    private static void EnsureLinePlaceholders(PurchaseOrderFormViewModel model)
    {
        const int minimumLines = 1;
        while (model.Lines.Count < minimumLines)
        {
            model.Lines.Add(new PurchaseOrderLineViewModel());
        }
    }

    private static PurchaseOrder MapToEntity(PurchaseOrderFormViewModel model)
    {
        var order = new PurchaseOrder
        {
            PurchaseOrderId = model.PurchaseOrderId ?? 0,
            PONumber = model.PONumber ?? string.Empty,
            VendorId = model.VendorId ?? 0,
            Reference = model.Reference,
            PODate = model.PODate,
            Status = model.Status
        };

        foreach (var lineModel in model.Lines)
        {
            if (!lineModel.ProductId.HasValue || lineModel.Quantity <= 0)
            {
                continue;
            }

            order.Lines.Add(new PurchaseOrderLine
            {
                PurchaseOrderLineId = lineModel.PurchaseOrderLineId ?? 0,
                ProductId = lineModel.ProductId,
                Quantity = lineModel.Quantity,
                UnitPrice = lineModel.UnitPrice,
                AnalyticalAccountId = lineModel.AnalyticalAccountId,
                Total = Math.Round(lineModel.Quantity * lineModel.UnitPrice, 2, MidpointRounding.AwayFromZero)
            });
        }

        return order;
    }

    private static PurchaseOrderFormViewModel MapToViewModel(PurchaseOrder order)
    {
        return new PurchaseOrderFormViewModel
        {
            PurchaseOrderId = order.PurchaseOrderId,
            PONumber = order.PONumber,
            VendorId = order.VendorId,
            Reference = order.Reference,
            PODate = order.PODate,
            Status = order.Status,
            Lines = order.Lines.Select(line => new PurchaseOrderLineViewModel
            {
                PurchaseOrderLineId = line.PurchaseOrderLineId,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                AnalyticalAccountId = line.AnalyticalAccountId
            }).ToList()
        };
    }

    private async Task ApplyBudgetWarningsAsync(PurchaseOrderFormViewModel model, CancellationToken cancellationToken)
    {
        var order = MapToEntity(model);
        var warnings = await _budgetWarningService.EvaluateAsync(order, cancellationToken);

        for (var index = 0; index < model.Lines.Count; index++)
        {
            if (warnings.TryGetValue(index, out var warning))
            {
                model.Lines[index].HasBudgetWarning = warning.IsExceeded;
                model.Lines[index].BudgetWarningMessage = warning.Message;
            }
            else
            {
                model.Lines[index].HasBudgetWarning = false;
                model.Lines[index].BudgetWarningMessage = null;
            }
        }
    }
}
