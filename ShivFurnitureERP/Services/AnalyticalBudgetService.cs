using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public class AnalyticalBudgetService : IAnalyticalBudgetService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AnalyticalBudgetService> _logger;

    public AnalyticalBudgetService(ApplicationDbContext dbContext, ILogger<AnalyticalBudgetService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AnalyticalBudget>> GetBudgetsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.AnalyticalBudgets
            .Include(b => b.AnalyticalAccount)
            .Include(b => b.OriginalBudget)
            .Include(b => b.Revisions)
            .AsNoTracking()
            .OrderByDescending(b => b.PeriodStart)
            .ToListAsync(cancellationToken);
    }

    public Task<AnalyticalBudget?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.AnalyticalBudgets
            .Include(b => b.AnalyticalAccount)
            .Include(b => b.OriginalBudget)
            .Include(b => b.Revisions)
            .FirstOrDefaultAsync(b => b.AnalyticalBudgetId == id, cancellationToken);
    }

    public async Task<AnalyticalBudget> CreateAsync(AnalyticalBudget budget, CancellationToken cancellationToken = default)
    {
        budget.CreatedOn = DateTime.UtcNow;
        _dbContext.AnalyticalBudgets.Add(budget);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return budget;
    }

    public async Task<AnalyticalBudget> CreateRevisionAsync(int sourceBudgetId, AnalyticalBudget revision, CancellationToken cancellationToken = default)
    {
        var sourceBudget = await _dbContext.AnalyticalBudgets
            .FirstOrDefaultAsync(b => b.AnalyticalBudgetId == sourceBudgetId, cancellationToken)
            ?? throw new InvalidOperationException($"Budget {sourceBudgetId} not found.");

        if (sourceBudget.Status != BudgetStatus.Confirmed)
        {
            throw new InvalidOperationException("Only confirmed budgets can be revised.");
        }

        var originalId = sourceBudget.OriginalBudgetId ?? sourceBudget.AnalyticalBudgetId;

        revision.AnalyticalAccountId = sourceBudget.AnalyticalAccountId;
        revision.BudgetName = sourceBudget.BudgetName;
        revision.BudgetType = sourceBudget.BudgetType;
        revision.OriginalBudgetId = originalId;
        revision.CreatedOn = DateTime.UtcNow;
        revision.IsReadOnly = false;
        revision.Status = BudgetStatus.Draft;

        sourceBudget.IsReadOnly = true;
        sourceBudget.Status = BudgetStatus.Revised;

        _dbContext.AnalyticalBudgets.Add(revision);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Budget {BudgetId} revised. New budget {RevisionId} created.", sourceBudgetId, revision.AnalyticalBudgetId);
        return revision;
    }

    public async Task UpdateStatusAsync(int budgetId, BudgetStatus status, CancellationToken cancellationToken = default)
    {
        var budget = await _dbContext.AnalyticalBudgets
            .FirstOrDefaultAsync(b => b.AnalyticalBudgetId == budgetId, cancellationToken)
            ?? throw new InvalidOperationException($"Budget {budgetId} not found.");

        if (budget.Status == status)
        {
            return;
        }

        switch (status)
        {
            case BudgetStatus.Confirmed:
                if (budget.Status != BudgetStatus.Draft)
                {
                    throw new InvalidOperationException("Only draft budgets can be confirmed.");
                }
                budget.Status = BudgetStatus.Confirmed;
                budget.IsReadOnly = false;
                break;
            case BudgetStatus.Archived:
                budget.Status = BudgetStatus.Archived;
                budget.IsReadOnly = true;
                break;
            default:
                throw new InvalidOperationException("Unsupported status transition.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BudgetPerformanceResult>> GetBudgetPerformanceAsync(CancellationToken cancellationToken = default)
    {
        var budgets = await GetBudgetsAsync(cancellationToken);
        var results = new List<BudgetPerformanceResult>(budgets.Count);

        foreach (var budget in budgets)
        {
            var achieved = await CalculateAchievedAmountAsync(budget, cancellationToken);
            results.Add(BuildResult(budget, achieved, Array.Empty<BudgetTransaction>()));
        }

        return results;
    }

    public async Task<BudgetPerformanceResult?> GetBudgetPerformanceAsync(int budgetId, bool includeTransactions, CancellationToken cancellationToken = default)
    {
        var budget = await _dbContext.AnalyticalBudgets
            .Include(b => b.AnalyticalAccount)
            .Include(b => b.OriginalBudget)
            .Include(b => b.Revisions)
            .FirstOrDefaultAsync(b => b.AnalyticalBudgetId == budgetId, cancellationToken);

        if (budget is null)
        {
            return null;
        }

        if (!includeTransactions)
        {
            var achieved = await CalculateAchievedAmountAsync(budget, cancellationToken);
            return BuildResult(budget, achieved, Array.Empty<BudgetTransaction>());
        }

        var transactions = await LoadTransactionsAsync(budget, cancellationToken);
        var achievedWithDetails = transactions.Sum(t => t.Amount);
        return BuildResult(budget, achievedWithDetails, transactions);
    }

    private BudgetPerformanceResult BuildResult(AnalyticalBudget budget, decimal achieved, IReadOnlyList<BudgetTransaction> transactions)
    {
        var remaining = Math.Round(budget.LimitAmount - achieved, 2, MidpointRounding.AwayFromZero);
        var percent = budget.LimitAmount == 0
            ? 0
            : Math.Round(achieved / budget.LimitAmount * 100, 2, MidpointRounding.AwayFromZero);

        return new BudgetPerformanceResult
        {
            Budget = budget,
            AchievedAmount = achieved,
            RemainingBalance = remaining,
            AchievedPercent = percent,
            BalanceForChart = remaining > 0 ? remaining : 0,
            MeetsIncomeTarget = budget.BudgetType == BudgetType.Income && achieved >= budget.LimitAmount,
            Transactions = transactions
        };
    }

    private async Task<decimal> CalculateAchievedAmountAsync(AnalyticalBudget budget, CancellationToken cancellationToken)
    {
        if (budget.BudgetType == BudgetType.Income)
        {
            var salesTotal = await _dbContext.SalesOrderLines
                .Where(line => line.AnalyticalAccountId == budget.AnalyticalAccountId)
                .Where(line => line.SalesOrder != null && line.SalesOrder.Status == SalesOrderStatus.Confirmed)
                .Where(line => line.SalesOrder!.SODate >= budget.PeriodStart && line.SalesOrder.SODate <= budget.PeriodEnd)
                .SumAsync(line => (decimal?)line.Total, cancellationToken) ?? 0m;

            return Math.Round(salesTotal, 2, MidpointRounding.AwayFromZero);
        }

        var billTotal = await _dbContext.VendorBillLines
            .Where(line => line.AnalyticalAccountId == budget.AnalyticalAccountId)
            .Where(line => line.VendorBill != null && line.VendorBill.Status == VendorBillStatus.Confirmed)
            .Where(line => line.VendorBill!.BillDate >= budget.PeriodStart && line.VendorBill.BillDate <= budget.PeriodEnd)
            .SumAsync(line => (decimal?)line.Total, cancellationToken) ?? 0m;

        var purchaseOrderTotal = await _dbContext.PurchaseOrderLines
            .Where(line => line.AnalyticalAccountId == budget.AnalyticalAccountId)
            .Where(line => line.PurchaseOrder != null && line.PurchaseOrder.Status != PurchaseOrderStatus.Cancelled)
            .Where(line => line.PurchaseOrder!.PODate >= budget.PeriodStart && line.PurchaseOrder.PODate <= budget.PeriodEnd)
            .SumAsync(line => (decimal?)line.Total, cancellationToken) ?? 0m;

        return Math.Round(billTotal + purchaseOrderTotal, 2, MidpointRounding.AwayFromZero);
    }

    private async Task<IReadOnlyList<BudgetTransaction>> LoadTransactionsAsync(AnalyticalBudget budget, CancellationToken cancellationToken)
    {
        if (budget.BudgetType == BudgetType.Income)
        {
            var salesTransactions = await _dbContext.SalesOrderLines
                .Where(line => line.AnalyticalAccountId == budget.AnalyticalAccountId)
                .Where(line => line.SalesOrder != null && line.SalesOrder.Status == SalesOrderStatus.Confirmed)
                .Where(line => line.SalesOrder!.SODate >= budget.PeriodStart && line.SalesOrder.SODate <= budget.PeriodEnd)
                .GroupBy(line => new
                {
                    line.SalesOrderId,
                    line.SalesOrder!.SONumber,
                    line.SalesOrder!.SODate,
                    CustomerName = line.SalesOrder!.Customer != null
                        ? line.SalesOrder!.Customer!.Name
                        : null,
                    line.SalesOrder!.CustomerId
                })
                .Select(group => new BudgetTransaction(
                    BudgetTransactionType.SalesOrder,
                    group.Key.SalesOrderId,
                    group.Key.SONumber,
                    group.Key.SODate,
                    group.Sum(x => x.Total),
                    group.Key.CustomerName ?? $"Customer #{group.Key.CustomerId}"))
                .ToListAsync(cancellationToken);

            return salesTransactions
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Reference)
                .ToList();
        }

        var vendorTransactionsData = await _dbContext.VendorBillLines
            .Where(line => line.AnalyticalAccountId == budget.AnalyticalAccountId)
            .Where(line => line.VendorBill != null && line.VendorBill.Status == VendorBillStatus.Confirmed)
            .Where(line => line.VendorBill!.BillDate >= budget.PeriodStart && line.VendorBill.BillDate <= budget.PeriodEnd)
            .GroupBy(line => new
            {
                line.VendorBillId,
                line.VendorBill!.BillNumber,
                line.VendorBill!.BillDate,
                VendorName = line.VendorBill!.Vendor != null
                    ? line.VendorBill!.Vendor!.Name
                    : null,
                line.VendorBill!.VendorId
            })
            .Select(group => new
            {
                group.Key.VendorBillId,
                group.Key.BillNumber,
                group.Key.BillDate,
                group.Key.VendorName,
                group.Key.VendorId,
                Amount = group.Sum(x => x.Total)
            })
            .ToListAsync(cancellationToken);

        var vendorTransactions = vendorTransactionsData
            .Select(item => new BudgetTransaction(
                BudgetTransactionType.VendorBill,
                item.VendorBillId,
                item.BillNumber,
                item.BillDate,
                item.Amount,
                item.VendorName ?? $"Vendor #{item.VendorId}"))
            .ToList();

        var purchaseOrderTransactionsData = await _dbContext.PurchaseOrderLines
            .Where(line => line.AnalyticalAccountId == budget.AnalyticalAccountId)
            .Where(line => line.PurchaseOrder != null && line.PurchaseOrder.Status != PurchaseOrderStatus.Cancelled)
            .Where(line => line.PurchaseOrder!.PODate >= budget.PeriodStart && line.PurchaseOrder.PODate <= budget.PeriodEnd)
            .GroupBy(line => new
            {
                line.PurchaseOrderId,
                line.PurchaseOrder!.PONumber,
                line.PurchaseOrder!.PODate,
                VendorName = line.PurchaseOrder!.Vendor != null
                    ? line.PurchaseOrder!.Vendor!.Name
                    : null,
                line.PurchaseOrder!.VendorId
            })
            .Select(group => new
            {
                group.Key.PurchaseOrderId,
                group.Key.PONumber,
                group.Key.PODate,
                group.Key.VendorName,
                group.Key.VendorId,
                Amount = group.Sum(x => x.Total)
            })
            .ToListAsync(cancellationToken);

        var purchaseOrderTransactions = purchaseOrderTransactionsData
            .Select(item => new BudgetTransaction(
                BudgetTransactionType.PurchaseOrder,
                item.PurchaseOrderId,
                item.PONumber,
                item.PODate,
                item.Amount,
                item.VendorName ?? $"Vendor #{item.VendorId}"))
            .ToList();

        var combined = vendorTransactions
            .Concat(purchaseOrderTransactions)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Reference)
            .ToList();

        return combined;
    }
}
