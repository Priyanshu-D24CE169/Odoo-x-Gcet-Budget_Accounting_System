using System.Linq;
using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.ViewModels;

namespace ShivFurnitureERP.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ApplicationDbContext dbContext, ILogger<DashboardService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(
        DateTime? start = null,
        DateTime? end = null,
        int? analyticalAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var defaultStart = new DateTime(today.Year, 1, 1);
        var filterStart = (start?.Date ?? defaultStart);
        var filterEnd = (end?.Date ?? today);

        if (filterEnd < filterStart)
        {
            (filterStart, filterEnd) = (filterEnd, filterStart);
        }

        _logger.LogInformation("Building management dashboard between {Start} and {End} for account {Account}", filterStart, filterEnd, analyticalAccountId);

        var accountEntities = await _dbContext.AnalyticalAccounts
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        var accountOptions = accountEntities
            .Where(a => !a.IsArchived)
            .Select(a => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = a.AnalyticalAccountId.ToString(),
                Text = a.Name,
                Selected = analyticalAccountId.HasValue && analyticalAccountId.Value == a.AnalyticalAccountId
            })
            .ToList();

        accountOptions.Insert(0, new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
        {
            Text = "All Cost Centers",
            Value = string.Empty,
            Selected = !analyticalAccountId.HasValue
        });

        var accountNameLookup = accountEntities.ToDictionary(a => a.AnalyticalAccountId, a => a.Name);

        var budgetsQuery = _dbContext.AnalyticalBudgets
            .AsNoTracking()
            .Include(b => b.AnalyticalAccount)
            .Where(b => b.Status == BudgetStatus.Confirmed)
            .Where(b => b.PeriodStart <= filterEnd && b.PeriodEnd >= filterStart);

        if (analyticalAccountId.HasValue)
        {
            budgetsQuery = budgetsQuery.Where(b => b.AnalyticalAccountId == analyticalAccountId.Value);
        }

        var budgets = await budgetsQuery.ToListAsync(cancellationToken);

        var incomeBudgetsTotal = budgets.Where(b => b.BudgetType == BudgetType.Income).Sum(b => b.LimitAmount);
        var expenseBudgetsTotal = budgets.Where(b => b.BudgetType == BudgetType.Expense).Sum(b => b.LimitAmount);

        var incomeActualGroups = await _dbContext.CustomerInvoiceLines
            .AsNoTracking()
            .Where(line => line.AnalyticalAccountId.HasValue)
            .Where(line => line.CustomerInvoice != null
                           && line.CustomerInvoice.Status == CustomerInvoiceStatus.Confirmed
                           && line.CustomerInvoice.InvoiceDate >= filterStart
                           && line.CustomerInvoice.InvoiceDate <= filterEnd)
            .Where(line => !analyticalAccountId.HasValue || line.AnalyticalAccountId == analyticalAccountId.Value)
            .GroupBy(line => line.AnalyticalAccountId!.Value)
            .Select(group => new ActualAggregate(group.Key, group.Sum(line => line.Total)))
            .ToListAsync(cancellationToken);

        var expenseActualGroups = await _dbContext.VendorBillLines
            .AsNoTracking()
            .Where(line => line.AnalyticalAccountId.HasValue)
            .Where(line => line.VendorBill != null
                           && line.VendorBill.Status == VendorBillStatus.Confirmed
                           && line.VendorBill.BillDate >= filterStart
                           && line.VendorBill.BillDate <= filterEnd)
            .Where(line => !analyticalAccountId.HasValue || line.AnalyticalAccountId == analyticalAccountId.Value)
            .GroupBy(line => line.AnalyticalAccountId!.Value)
            .Select(group => new ActualAggregate(group.Key, group.Sum(line => line.Total)))
            .ToListAsync(cancellationToken);

        var incomeActualTotal = incomeActualGroups.Sum(x => x.Total);
        var expenseActualTotal = expenseActualGroups.Sum(x => x.Total);

        var budgetTotalsByAccount = budgets
            .GroupBy(b => b.AnalyticalAccountId)
            .ToDictionary(g => g.Key, g => g.Sum(b => b.LimitAmount));

        var incomeBudgetByAccount = budgets
            .Where(b => b.BudgetType == BudgetType.Income)
            .GroupBy(b => b.AnalyticalAccountId)
            .ToDictionary(g => g.Key, g => g.Sum(b => b.LimitAmount));

        var expenseBudgetByAccount = budgets
            .Where(b => b.BudgetType == BudgetType.Expense)
            .GroupBy(b => b.AnalyticalAccountId)
            .ToDictionary(g => g.Key, g => g.Sum(b => b.LimitAmount));

        var incomeActualByAccount = incomeActualGroups.ToDictionary(x => x.AccountId, x => x.Total);
        var expenseActualByAccount = expenseActualGroups.ToDictionary(x => x.AccountId, x => x.Total);

        var accountIds = new HashSet<int>(budgetTotalsByAccount.Keys);
        foreach (var key in incomeActualByAccount.Keys)
        {
            accountIds.Add(key);
        }
        foreach (var key in expenseActualByAccount.Keys)
        {
            accountIds.Add(key);
        }
        if (analyticalAccountId.HasValue)
        {
            accountIds.Add(analyticalAccountId.Value);
        }

        var costCenters = accountIds
            .Select(id => BuildCostCenter(id,
                accountNameLookup.TryGetValue(id, out var name) ? name : $"Account #{id}",
                budgetTotalsByAccount.TryGetValue(id, out var budgetTotal) ? budgetTotal : 0m,
                incomeBudgetByAccount.TryGetValue(id, out var incomeBudget) ? incomeBudget : 0m,
                expenseBudgetByAccount.TryGetValue(id, out var expenseBudget) ? expenseBudget : 0m,
                incomeActualByAccount.TryGetValue(id, out var incomeActual) ? incomeActual : 0m,
                expenseActualByAccount.TryGetValue(id, out var expenseActual) ? expenseActual : 0m))
            .OrderByDescending(c => c.UtilizationPercent)
            .ToList();

        var alerts = costCenters
            .Where(c => c.BudgetedAmount > 0 && c.UtilizationPercent >= 85m)
            .Select(c => new BudgetAlertViewModel
            {
                AccountName = c.AnalyticalAccountName,
                BudgetedAmount = c.BudgetedAmount,
                ActualAmount = c.ActualAmount,
                UtilizationPercent = c.UtilizationPercent,
                IsExceeded = c.ActualAmount > c.BudgetedAmount
            })
            .OrderByDescending(a => a.UtilizationPercent)
            .Take(6)
            .ToList();

        var incomePaymentAggregates = await _dbContext.CustomerInvoices
            .AsNoTracking()
            .Where(ci => ci.Status == CustomerInvoiceStatus.Confirmed)
            .Where(ci => ci.InvoiceDate >= filterStart && ci.InvoiceDate <= filterEnd)
            .Where(ci => !analyticalAccountId.HasValue || ci.Lines.Any(line => line.AnalyticalAccountId == analyticalAccountId.Value))
            .GroupBy(ci => ci.PaymentStatus)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count(),
                TotalAmount = group.Sum(ci => ci.TotalAmount),
                AmountPaid = group.Sum(ci => ci.AmountPaid)
            })
            .ToListAsync(cancellationToken);

        var expensePaymentAggregates = await _dbContext.VendorBills
            .AsNoTracking()
            .Where(vb => vb.Status == VendorBillStatus.Confirmed)
            .Where(vb => vb.BillDate >= filterStart && vb.BillDate <= filterEnd)
            .Where(vb => !analyticalAccountId.HasValue || vb.Lines.Any(line => line.AnalyticalAccountId == analyticalAccountId.Value))
            .GroupBy(vb => vb.PaymentStatus)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count(),
                TotalAmount = group.Sum(vb => vb.TotalAmount),
                AmountPaid = group.Sum(vb => vb.AmountPaid)
            })
            .ToListAsync(cancellationToken);

        var incomePaymentStatuses = incomePaymentAggregates
            .Select(a => new PaymentStatusItem
            {
                Label = a.Status.ToString(),
                DocumentCount = a.Count,
                TotalAmount = a.TotalAmount,
                AmountPaid = a.AmountPaid,
                OutstandingAmount = Math.Max(a.TotalAmount - a.AmountPaid, 0m)
            })
            .OrderByDescending(item => item.TotalAmount)
            .ToList();

        var expensePaymentStatuses = expensePaymentAggregates
            .Select(a => new PaymentStatusItem
            {
                Label = a.Status.ToString(),
                DocumentCount = a.Count,
                TotalAmount = a.TotalAmount,
                AmountPaid = a.AmountPaid,
                OutstandingAmount = Math.Max(a.TotalAmount - a.AmountPaid, 0m)
            })
            .OrderByDescending(item => item.TotalAmount)
            .ToList();

        var totalBudgeted = budgetTotalsByAccount.Values.Sum();
        var totalActual = incomeActualTotal + expenseActualTotal;
        var utilizationPercent = totalBudgeted <= 0
            ? 0
            : Math.Round(Math.Min(100m, totalActual / totalBudgeted * 100m), 2, MidpointRounding.AwayFromZero);

        return new DashboardViewModel
        {
            FilterStart = filterStart,
            FilterEnd = filterEnd,
            SelectedAccountId = analyticalAccountId,
            AccountOptions = accountOptions,
            TotalBudgetedAmount = totalBudgeted,
            TotalActualAmount = totalActual,
            BudgetUtilizationPercent = utilizationPercent,
            IncomeExpense = new IncomeExpenseSummary
            {
                BudgetedIncome = incomeBudgetsTotal,
                ActualIncome = incomeActualTotal,
                BudgetedExpense = expenseBudgetsTotal,
                ActualExpense = expenseActualTotal
            },
            CostCenters = costCenters,
            PaymentStatus = new PaymentStatusSummary
            {
                IncomeStatuses = incomePaymentStatuses,
                ExpenseStatuses = expensePaymentStatuses
            },
            Alerts = alerts
        };
    }

    private static CostCenterPerformanceViewModel BuildCostCenter(
        int accountId,
        string accountName,
        decimal totalBudget,
        decimal incomeBudget,
        decimal expenseBudget,
        decimal incomeActual,
        decimal expenseActual)
    {
        return new CostCenterPerformanceViewModel
        {
            AnalyticalAccountId = accountId,
            AnalyticalAccountName = accountName,
            BudgetedAmount = totalBudget > 0 ? totalBudget : incomeBudget + expenseBudget,
            ActualIncome = incomeActual,
            ActualExpense = expenseActual
        };
    }

    private sealed record ActualAggregate(int AccountId, decimal Total);
}
