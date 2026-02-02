using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.Services;
using ShivFurnitureERP.ViewModels.SalesOrders;

namespace ShivFurnitureERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class SalesOrdersController : Controller
{
    private readonly ISalesOrderService _salesOrderService;
    private readonly ApplicationDbContext _dbContext;

    public SalesOrdersController(ISalesOrderService salesOrderService, ApplicationDbContext dbContext)
    {
        _salesOrderService = salesOrderService;
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(string? search, SalesOrderStatus? status, CancellationToken cancellationToken)
    {
        var orders = await _salesOrderService.GetOrdersAsync(search, status, cancellationToken);
        var model = orders.Select(order => new SalesOrderListItemViewModel
        {
            SalesOrderId = order.SalesOrderId,
            SONumber = order.SONumber,
            CustomerName = order.Customer?.Name ?? "-",
            SODate = order.SODate,
            Reference = order.Reference,
            Status = order.Status
        }).ToList();

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = await BuildFormAsync(new SalesOrderFormViewModel(), cancellationToken);
        EnsureLine(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalesOrderFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await BuildFormAsync(model, cancellationToken);
            EnsureLine(model);
            return View(model);
        }

        var order = MapToEntity(model);
        await _salesOrderService.CreateAsync(order, cancellationToken);
        TempData["StatusMessage"] = "Sales order created.";
        return RedirectToAction(nameof(Edit), new { id = order.SalesOrderId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var order = await _salesOrderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var model = MapToViewModel(order);
        await BuildFormAsync(model, cancellationToken);
        EnsureLine(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SalesOrderFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.SalesOrderId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await BuildFormAsync(model, cancellationToken);
            EnsureLine(model);
            return View(model);
        }

        try
        {
            var entity = MapToEntity(model);
            await _salesOrderService.UpdateAsync(entity, cancellationToken);
            TempData["StatusMessage"] = "Sales order updated.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await BuildFormAsync(model, cancellationToken);
            EnsureLine(model);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, CancellationToken cancellationToken)
    {
        await _salesOrderService.ConfirmAsync(id, cancellationToken);
        TempData["StatusMessage"] = "Sales order confirmed.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        await _salesOrderService.CancelAsync(id, cancellationToken);
        TempData["StatusMessage"] = "Sales order cancelled.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var order = await _salesOrderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var model = MapToViewModel(order);
        await BuildFormAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInvoice(int id, CancellationToken cancellationToken)
    {
        var order = await _salesOrderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            TempData["StatusMessage"] = "Sales order not found.";
            return RedirectToAction(nameof(Index));
        }

        if (order.Status != SalesOrderStatus.Confirmed)
        {
            TempData["StatusMessage"] = "Confirm the sales order before creating an invoice.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var invoiceExists = await _dbContext.CustomerInvoices.AnyAsync(
            invoice => invoice.SalesOrderId == id,
            cancellationToken);

        if (invoiceExists)
        {
            TempData["StatusMessage"] = "An invoice already exists for this sales order.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        return RedirectToAction("Create", "CustomerInvoices", new { area = "Admin", salesOrderId = id });
    }

    private async Task<SalesOrderFormViewModel> BuildFormAsync(SalesOrderFormViewModel model, CancellationToken cancellationToken)
    {
        var portalRoleId = await _dbContext.Roles
            .Where(r => r.Name == "PortalUser")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var customers = new List<SelectListItem>();
        if (!string.IsNullOrEmpty(portalRoleId))
        {
            customers = await (from user in _dbContext.Users
                               join userRole in _dbContext.UserRoles on user.Id equals userRole.UserId
                               join role in _dbContext.Roles on userRole.RoleId equals role.Id
                               join contact in _dbContext.Contacts on user.ContactId equals contact.ContactId
                               where userRole.RoleId == portalRoleId
                                     && !contact.IsArchived
                               orderby contact.Name
                               select new SelectListItem
                               {
                                   Text = $"{(role.Name == "PortalUser" ? "Portal" : role.Name)} — {(!string.IsNullOrWhiteSpace(contact.Name)
                                       ? contact.Name
                                       : (!string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.LoginId))}",
                                   Value = contact.ContactId.ToString()
                               })
                               .ToListAsync(cancellationToken);
        }

        if (customers.Count == 0)
        {
            customers = await _dbContext.Contacts
                .Where(c => !c.IsArchived && (c.Type == ContactType.Customer || c.Type == ContactType.Both))
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Text = $"Contact — {c.Name}", Value = c.ContactId.ToString() })
                .ToListAsync(cancellationToken);
        }

        var products = await _dbContext.Products
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem { Text = p.Name, Value = p.ProductId.ToString() })
            .ToListAsync(cancellationToken);

        var incomeAccountIds = _dbContext.AnalyticalBudgets
            .Where(budget => budget.BudgetType == BudgetType.Income)
            .Select(budget => budget.AnalyticalAccountId)
            .Distinct();

        var analyticalAccounts = await _dbContext.AnalyticalAccounts
            .Where(account => !account.IsArchived && incomeAccountIds.Contains(account.AnalyticalAccountId))
            .OrderBy(account => account.Name)
            .Select(account => new SelectListItem { Text = account.Name, Value = account.AnalyticalAccountId.ToString() })
            .ToListAsync(cancellationToken);

        model.Customers = customers;
        model.Products = products;
        model.AnalyticalAccounts = analyticalAccounts;
        model.StatusOptions = Enum.GetValues<SalesOrderStatus>()
            .Select(status => new SelectListItem
            {
                Text = status switch
                {
                    SalesOrderStatus.Draft => "Draft",
                    SalesOrderStatus.Confirmed => "Confirm",
                    SalesOrderStatus.Cancelled => "Cancelled",
                    _ => status.ToString()
                },
                Value = status.ToString("d"),
                Selected = status == model.Status
            })
            .ToList();
        return model;
    }

    private static void EnsureLine(SalesOrderFormViewModel model)
    {
        if (model.Lines.Count == 0)
        {
            model.Lines.Add(new SalesOrderLineViewModel());
        }
    }

    private static SalesOrder MapToEntity(SalesOrderFormViewModel model)
    {
        var order = new SalesOrder
        {
            SalesOrderId = model.SalesOrderId ?? 0,
            CustomerId = model.CustomerId ?? 0,
            SODate = model.SODate,
            Reference = model.Reference,
            Status = model.Status
        };

        foreach (var line in model.Lines)
        {
            if (!line.ProductId.HasValue || line.Quantity <= 0)
            {
                continue;
            }

            order.Lines.Add(new SalesOrderLine
            {
                SalesOrderLineId = line.SalesOrderLineId ?? 0,
                ProductId = line.ProductId.Value,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                AnalyticalAccountId = line.AnalyticalAccountId
            });
        }

        return order;
    }

    private static SalesOrderFormViewModel MapToViewModel(SalesOrder order)
    {
        return new SalesOrderFormViewModel
        {
            SalesOrderId = order.SalesOrderId,
            SONumber = order.SONumber,
            CustomerId = order.CustomerId,
            SODate = order.SODate,
            Reference = order.Reference,
            Status = order.Status,
            Lines = order.Lines.Select(line => new SalesOrderLineViewModel
            {
                SalesOrderLineId = line.SalesOrderLineId,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                AnalyticalAccountId = line.AnalyticalAccountId
            }).ToList()
        };
    }
}
