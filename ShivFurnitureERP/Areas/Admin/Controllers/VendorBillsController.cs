using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.Services;
using ShivFurnitureERP.ViewModels.BillPayments;
using ShivFurnitureERP.ViewModels.VendorBills;

namespace ShivFurnitureERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class VendorBillsController : Controller
{
    private readonly IVendorBillService _vendorBillService;
    private readonly IBillPaymentService _billPaymentService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IBudgetWarningService _budgetWarningService;

    public VendorBillsController(
        IVendorBillService vendorBillService,
        IBillPaymentService billPaymentService,
        ApplicationDbContext dbContext,
        IBudgetWarningService budgetWarningService)
    {
        _vendorBillService = vendorBillService;
        _billPaymentService = billPaymentService;
        _dbContext = dbContext;
        _budgetWarningService = budgetWarningService;
    }

    public async Task<IActionResult> Index(string? search, VendorBillStatus? status, VendorBillPaymentStatus? paymentStatus, CancellationToken cancellationToken)
    {
        var bills = await _vendorBillService.GetBillsAsync(search, status, paymentStatus, cancellationToken);
        var model = bills.Select(bill => new VendorBillListItemViewModel
        {
            VendorBillId = bill.VendorBillId,
            BillNumber = bill.BillNumber,
            VendorName = bill.Vendor?.Name ?? "-",
            BillDate = bill.BillDate,
            DueDate = bill.DueDate,
            Status = bill.Status,
            PaymentStatus = bill.PaymentStatus,
            TotalAmount = bill.TotalAmount,
            AmountPaid = bill.AmountPaid
        }).ToList();

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["PaymentStatus"] = paymentStatus;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var bill = await _vendorBillService.GetByIdAsync(id, cancellationToken);
        if (bill is null)
        {
            return NotFound();
        }

        var model = await BuildFormViewModelAsync(MapToViewModel(bill), cancellationToken);
        EnsureLinePlaceholders(model);
        await ApplyBudgetWarningsAsync(model, cancellationToken);
        await PopulatePaymentsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VendorBillFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.VendorBillId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync(model, cancellationToken);
            EnsureLinePlaceholders(model);
            await ApplyBudgetWarningsAsync(model, cancellationToken);
            await PopulatePaymentsAsync(model, cancellationToken);
            return View(model);
        }

        var billEntity = MapToEntity(model);
        var budgetWarnings = await _budgetWarningService.EvaluateAsync(billEntity, cancellationToken);
        if (budgetWarnings.Values.Any(warning => warning.IsExceeded))
        {
            ModelState.AddModelError(string.Empty, "Budget limits are exceeded for one or more analytical accounts. Adjust the bill or choose a different account before saving.");
            await PopulateSelectListsAsync(model, cancellationToken);
            EnsureLinePlaceholders(model);
            ApplyBudgetWarnings(model, billEntity, budgetWarnings);
            await PopulatePaymentsAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            await _vendorBillService.UpdateAsync(billEntity, cancellationToken);
            TempData["StatusMessage"] = "Vendor Bill updated.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateSelectListsAsync(model, cancellationToken);
            EnsureLinePlaceholders(model);
            ApplyBudgetWarnings(model, billEntity, budgetWarnings);
            await PopulatePaymentsAsync(model, cancellationToken);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, CancellationToken cancellationToken)
    {
        await _vendorBillService.ConfirmAsync(id, cancellationToken);
        TempData["StatusMessage"] = "Vendor Bill confirmed.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        await _vendorBillService.CancelAsync(id, cancellationToken);
        TempData["StatusMessage"] = "Vendor Bill cancelled.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var bill = await _vendorBillService.GetByIdAsync(id, cancellationToken);
        if (bill is null)
        {
            return NotFound();
        }

        var model = await BuildFormViewModelAsync(MapToViewModel(bill), cancellationToken);
        await ApplyBudgetWarningsAsync(model, cancellationToken);
        await PopulatePaymentsAsync(model, cancellationToken);
        return View(model);
    }

    private async Task<VendorBillFormViewModel> BuildFormViewModelAsync(VendorBillFormViewModel model, CancellationToken cancellationToken)
    {
        await PopulateSelectListsAsync(model, cancellationToken);
        if (model.Lines.Count == 0)
        {
            model.Lines.Add(new VendorBillLineViewModel());
        }

        return model;
    }

    private async Task PopulateSelectListsAsync(VendorBillFormViewModel model, CancellationToken cancellationToken)
    {
        var vendors = await _dbContext.Contacts
            .Where(c => !c.IsArchived && (c.Type == ContactType.Vendor || c.Type == ContactType.Both))
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem { Text = c.Name, Value = c.ContactId.ToString() })
            .ToListAsync(cancellationToken);

        if (model.VendorId.HasValue && vendors.All(v => v.Value != model.VendorId.Value.ToString()))
        {
            var vendor = await _dbContext.Contacts
                .Where(c => c.ContactId == model.VendorId.Value)
                .Select(c => new SelectListItem { Text = c.Name + " (archived)", Value = c.ContactId.ToString() })
                .FirstOrDefaultAsync(cancellationToken);
            if (vendor is not null)
            {
                vendors.Insert(0, vendor);
            }
        }

        var products = await _dbContext.Products
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem { Text = p.Name, Value = p.ProductId.ToString() })
            .ToListAsync(cancellationToken);

        var expenseAccountIds = _dbContext.AnalyticalBudgets
            .Where(b => b.BudgetType == BudgetType.Expense)
            .Select(b => b.AnalyticalAccountId)
            .Distinct();

        var analyticalAccounts = await _dbContext.AnalyticalAccounts
            .Where(a => !a.IsArchived && expenseAccountIds.Contains(a.AnalyticalAccountId))
            .OrderBy(a => a.Name)
            .Select(a => new SelectListItem { Text = a.Name, Value = a.AnalyticalAccountId.ToString() })
            .ToListAsync(cancellationToken);

        model.Vendors = vendors;
        model.Products = products;
        model.AnalyticalAccounts = analyticalAccounts;
    }

    private static void EnsureLinePlaceholders(VendorBillFormViewModel model)
    {
        if (model.Lines.Count == 0)
        {
            model.Lines.Add(new VendorBillLineViewModel());
        }
    }

    private static VendorBill MapToEntity(VendorBillFormViewModel model)
    {
        var bill = new VendorBill
        {
            VendorBillId = model.VendorBillId ?? 0,
            BillNumber = model.BillNumber ?? string.Empty,
            VendorId = model.VendorId ?? 0,
            BillDate = model.BillDate,
            DueDate = model.DueDate,
            Status = model.Status,
            PaymentStatus = model.PaymentStatus,
            AmountPaid = model.AmountPaid,
            PurchaseOrderId = model.PurchaseOrderId
        };

        foreach (var lineModel in model.Lines)
        {
            if (!lineModel.ProductId.HasValue || lineModel.Quantity <= 0)
            {
                continue;
            }

            bill.Lines.Add(new VendorBillLine
            {
                VendorBillLineId = lineModel.VendorBillLineId ?? 0,
                ProductId = lineModel.ProductId,
                Quantity = lineModel.Quantity,
                UnitPrice = lineModel.UnitPrice,
                Total = Math.Round(lineModel.Quantity * lineModel.UnitPrice, 2, MidpointRounding.AwayFromZero),
                AnalyticalAccountId = lineModel.AnalyticalAccountId
            });
        }

        bill.TotalAmount = bill.Lines.Sum(line => line.Total);
        return bill;
    }

    private static VendorBillFormViewModel MapToViewModel(VendorBill bill)
    {
        return new VendorBillFormViewModel
        {
            VendorBillId = bill.VendorBillId,
            BillNumber = bill.BillNumber,
            VendorId = bill.VendorId,
            BillDate = bill.BillDate,
            DueDate = bill.DueDate,
            Status = bill.Status,
            PaymentStatus = bill.PaymentStatus,
            AmountPaid = bill.AmountPaid,
            TotalAmount = bill.TotalAmount,
            PurchaseOrderId = bill.PurchaseOrderId,
            Lines = bill.Lines.Select(line => new VendorBillLineViewModel
            {
                VendorBillLineId = line.VendorBillLineId,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                AnalyticalAccountId = line.AnalyticalAccountId,
                HasBudgetWarning = false
            }).ToList(),
            Payments = new List<BillPaymentListItemViewModel>()
        };
    }

    private async Task ApplyBudgetWarningsAsync(VendorBillFormViewModel model, CancellationToken cancellationToken)
    {
        var bill = MapToEntity(model);
        var warnings = await _budgetWarningService.EvaluateAsync(bill, cancellationToken);
        ApplyBudgetWarnings(model, bill, warnings);
    }

    private static void ApplyBudgetWarnings(
        VendorBillFormViewModel model,
        VendorBill bill,
        IReadOnlyDictionary<int, BudgetWarningResult> warnings)
    {
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

        model.TotalAmount = bill.TotalAmount;
    }

    private async Task PopulatePaymentsAsync(VendorBillFormViewModel model, CancellationToken cancellationToken)
    {
        if (!model.VendorBillId.HasValue)
        {
            model.Payments = new List<BillPaymentListItemViewModel>();
            return;
        }

        var payments = await _billPaymentService.GetPaymentsForBillAsync(model.VendorBillId.Value, cancellationToken);
        model.Payments = payments.Select(payment => new BillPaymentListItemViewModel
        {
            BillPaymentId = payment.BillPaymentId,
            PaymentNumber = payment.PaymentNumber,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            PaymentMode = payment.PaymentMode,
            Note = payment.Note
        }).ToList();
    }
}
