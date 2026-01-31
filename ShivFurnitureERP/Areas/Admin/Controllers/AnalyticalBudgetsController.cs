using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.Services;
using ShivFurnitureERP.ViewModels.AnalyticalBudgets;

namespace ShivFurnitureERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class AnalyticalBudgetsController : Controller
{
    private readonly IAnalyticalBudgetService _budgetService;
    private readonly IAnalyticalAccountService _accountService;

    public AnalyticalBudgetsController(
        IAnalyticalBudgetService budgetService,
        IAnalyticalAccountService accountService)
    {
        _budgetService = budgetService;
        _accountService = accountService;
    }

    public async Task<IActionResult> Index(BudgetListMode mode = BudgetListMode.Confirm, CancellationToken cancellationToken = default)
    {
        var performance = await _budgetService.GetBudgetPerformanceAsync(cancellationToken);
        var model = performance.Select(result => new BudgetPerformanceListItemViewModel
        {
            AnalyticalBudgetId = result.Budget.AnalyticalBudgetId,
            BudgetName = result.Budget.BudgetName,
            AccountName = result.Budget.AnalyticalAccount?.Name ?? $"Account #{result.Budget.AnalyticalAccountId}",
            BudgetType = result.Budget.BudgetType,
            PeriodStart = result.Budget.PeriodStart,
            PeriodEnd = result.Budget.PeriodEnd,
            LimitAmount = result.Budget.LimitAmount,
            AchievedAmount = result.AchievedAmount,
            RemainingBalance = result.RemainingBalance,
            AchievedPercent = result.AchievedPercent,
            IsReadOnly = result.Budget.IsReadOnly,
            OriginalBudgetId = result.Budget.OriginalBudgetId,
            HasRevisions = result.Budget.Revisions?.Any() == true,
            Status = result.Budget.Status,
            ShouldArchive = result.MeetsIncomeTarget
        }).ToList();

        var filtered = mode switch
        {
            BudgetListMode.Draft => model.Where(b => b.Status == BudgetStatus.Draft).ToList(),
            BudgetListMode.Confirm => model.Where(b => b.Status == BudgetStatus.Confirmed).ToList(),
            BudgetListMode.Revise => model.Where(b => b.Status == BudgetStatus.Revised).ToList(),
            BudgetListMode.Archived => model.Where(b => b.Status == BudgetStatus.Archived).ToList(),
            _ => model
        };

        ViewData["Mode"] = mode;
        return View(filtered);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var result = await _budgetService.GetBudgetPerformanceAsync(id, includeTransactions: true, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        var revisions = result.Budget.Revisions ?? Array.Empty<AnalyticalBudget>();

        var model = new BudgetDetailsViewModel
        {
            AnalyticalBudgetId = result.Budget.AnalyticalBudgetId,
            BudgetName = result.Budget.BudgetName,
            AccountName = result.Budget.AnalyticalAccount?.Name ?? $"Account #{result.Budget.AnalyticalAccountId}",
            BudgetType = result.Budget.BudgetType,
            AnalyticalAccountName = result.Budget.AnalyticalAccount?.Name ?? string.Empty,
            PeriodStart = result.Budget.PeriodStart,
            PeriodEnd = result.Budget.PeriodEnd,
            LimitAmount = result.Budget.LimitAmount,
            AchievedAmount = result.AchievedAmount,
            RemainingBalance = result.RemainingBalance,
            AchievedPercent = result.AchievedPercent,
            BalanceForChart = result.BalanceForChart,
            IsReadOnly = result.Budget.IsReadOnly,
            OriginalBudgetId = result.Budget.OriginalBudgetId,
            HasRevisions = revisions.Any(),
            Status = result.Budget.Status,
            ShouldArchive = result.MeetsIncomeTarget,
            LatestRevisionId = revisions
                .OrderByDescending(r => r.AnalyticalBudgetId)
                .Select(r => (int?)r.AnalyticalBudgetId)
                .FirstOrDefault(),
            Transactions = result.Transactions.Select(transaction =>
            {
                var (documentType, controller) = transaction.Type switch
                {
                    BudgetTransactionType.CustomerInvoice => ("Customer Invoice", "CustomerInvoices"),
                    BudgetTransactionType.VendorBill => ("Vendor Bill", "VendorBills"),
                    BudgetTransactionType.PurchaseOrder => ("Purchase Order", "PurchaseOrders"),
                    BudgetTransactionType.SalesOrder => ("Sales Order", "SalesOrders"),
                    _ => ("Document", "Dashboard")
                };

                return new BudgetTransactionViewModel
                {
                    DocumentType = documentType,
                    Reference = transaction.Reference,
                    Date = transaction.Date,
                    Amount = transaction.Amount,
                    Counterparty = transaction.Counterparty,
                    Controller = controller,
                    Action = "Details",
                    DocumentId = transaction.DocumentId,
                    Type = transaction.Type
                };
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new AnalyticalBudgetFormViewModel
        {
            BudgetName = $"Budget {DateTime.UtcNow:MMMM yyyy}",
            PeriodStart = DateTime.UtcNow.Date,
            PeriodEnd = DateTime.UtcNow.Date.AddMonths(1).AddDays(-1),
            BudgetType = BudgetType.Income,
            Accounts = await BuildAccountSelectListAsync(null, cancellationToken)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AnalyticalBudgetFormViewModel model, CancellationToken cancellationToken)
    {
        ValidatePeriod(model);

        if (!ModelState.IsValid)
        {
            model.Accounts = await BuildAccountSelectListAsync(model.AnalyticalAccountId, cancellationToken);
            return View(model);
        }

        var entity = new AnalyticalBudget
        {
            BudgetName = model.BudgetName.Trim(),
            AnalyticalAccountId = model.AnalyticalAccountId,
            BudgetType = model.BudgetType,
            PeriodStart = model.PeriodStart,
            PeriodEnd = model.PeriodEnd,
            LimitAmount = model.LimitAmount
        };

        await _budgetService.CreateAsync(entity, cancellationToken);
        TempData["StatusMessage"] = "Budget created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> CreateRevision(int id, CancellationToken cancellationToken)
    {
        var budget = await _budgetService.GetByIdAsync(id, cancellationToken);
        if (budget is null)
        {
            return NotFound();
        }

        if (budget.Status != BudgetStatus.Confirmed)
        {
            TempData["StatusMessage"] = "Only confirmed budgets can be revised.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new AnalyticalBudgetFormViewModel
        {
            AnalyticalBudgetId = budget.AnalyticalBudgetId,
            BudgetName = budget.BudgetName,
            AnalyticalAccountId = budget.AnalyticalAccountId,
            AnalyticalAccountName = budget.AnalyticalAccount?.Name ?? $"Account #{budget.AnalyticalAccountId}",
            PeriodStart = budget.PeriodStart,
            PeriodEnd = budget.PeriodEnd,
            LimitAmount = budget.LimitAmount,
            BudgetType = budget.BudgetType,
            OriginalBudgetId = budget.OriginalBudgetId ?? budget.AnalyticalBudgetId,
            IsRevision = true
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRevision(int id, AnalyticalBudgetFormViewModel model, CancellationToken cancellationToken)
    {
        ValidatePeriod(model);

        if (!ModelState.IsValid)
        {
            model.IsRevision = true;
            return View(model);
        }

        var revision = new AnalyticalBudget
        {
            BudgetName = model.BudgetName,
            AnalyticalAccountId = model.AnalyticalAccountId,
            BudgetType = model.BudgetType,
            PeriodStart = model.PeriodStart,
            PeriodEnd = model.PeriodEnd,
            LimitAmount = model.LimitAmount
        };

        try
        {
            var created = await _budgetService.CreateRevisionAsync(id, revision, cancellationToken);
            TempData["StatusMessage"] = "Budget revision created successfully.";
            return RedirectToAction(nameof(Details), new { id = created.AnalyticalBudgetId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.IsRevision = true;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, BudgetStatus status, CancellationToken cancellationToken)
    {
        try
        {
            await _budgetService.UpdateStatusAsync(id, status, cancellationToken);
            TempData["StatusMessage"] = status switch
            {
                BudgetStatus.Confirmed => "Budget confirmed successfully.",
                BudgetStatus.Archived => "Budget archived successfully.",
                _ => "Budget status updated."
            };
        }
        catch (InvalidOperationException ex)
        {
            TempData["StatusMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private void ValidatePeriod(AnalyticalBudgetFormViewModel model)
    {
        if (model.PeriodEnd < model.PeriodStart)
        {
            ModelState.AddModelError(nameof(model.PeriodEnd), "Period end must be greater than or equal to the start date.");
        }
    }

    private async Task<IEnumerable<SelectListItem>> BuildAccountSelectListAsync(int? selectedAccountId, CancellationToken cancellationToken)
    {
        var accounts = await _accountService.GetAccountsAsync(null, includeArchived: false, cancellationToken);
        return accounts.Select(account => new SelectListItem
        {
            Value = account.AnalyticalAccountId.ToString(),
            Text = account.Name,
            Selected = selectedAccountId.HasValue && selectedAccountId.Value == account.AnalyticalAccountId
        }).ToList();
    }
}
