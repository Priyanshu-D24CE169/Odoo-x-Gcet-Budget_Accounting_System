using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public class BudgetWarningService : IBudgetWarningService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<BudgetWarningService> _logger;

    public BudgetWarningService(ApplicationDbContext dbContext, ILogger<BudgetWarningService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task<IReadOnlyDictionary<int, BudgetWarningResult>> EvaluateAsync(PurchaseOrder order, CancellationToken cancellationToken = default)
    {
        var lines = order.Lines
            .Select((line, index) => new BudgetLineEntry(index, line.AnalyticalAccountId, line.Total))
            .ToList();

        return EvaluateInternalAsync(order.PODate, lines, BudgetSpendSource.PurchaseOrder, order.PurchaseOrderId, cancellationToken);
    }

    public Task<IReadOnlyDictionary<int, BudgetWarningResult>> EvaluateAsync(VendorBill bill, CancellationToken cancellationToken = default)
    {
        var lines = bill.Lines
            .Select((line, index) => new BudgetLineEntry(index, line.AnalyticalAccountId, line.Total))
            .ToList();

        return EvaluateInternalAsync(bill.BillDate, lines, BudgetSpendSource.VendorBill, bill.VendorBillId, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<int, BudgetWarningResult>> EvaluateInternalAsync(
        DateTime documentDate,
        IReadOnlyList<BudgetLineEntry> lines,
        BudgetSpendSource source,
        int documentId,
        CancellationToken cancellationToken)
    {
        var warnings = new Dictionary<int, BudgetWarningResult>();
        var indexedLines = lines.Where(line => line.AnalyticalAccountId.HasValue).ToList();
        if (indexedLines.Count == 0)
        {
            return warnings;
        }

        var accountGroups = indexedLines
            .GroupBy(line => line.AnalyticalAccountId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        var accountIds = accountGroups.Keys.ToArray();
        var budgets = await _dbContext.AnalyticalBudgets
            .Where(b => accountIds.Contains(b.AnalyticalAccountId) && documentDate >= b.PeriodStart && documentDate <= b.PeriodEnd)
            .ToListAsync(cancellationToken);

        if (budgets.Count == 0)
        {
            return warnings;
        }

        var confirmedSpend = await GetConfirmedSpendAsync(accountIds, cancellationToken);

        foreach (var (accountId, entries) in accountGroups)
        {
            var budget = budgets.FirstOrDefault(b => b.AnalyticalAccountId == accountId);
            if (budget is null)
            {
                continue;
            }

            var existingSpend = confirmedSpend
                .Where(spend => spend.AnalyticalAccountId == accountId && spend.DocumentDate >= budget.PeriodStart && spend.DocumentDate <= budget.PeriodEnd)
                .Where(spend => documentId == 0 || spend.Source != source || spend.DocumentId != documentId)
                .Sum(spend => spend.Total);

            var projected = existingSpend + entries.Sum(entry => entry.Total);

            if (projected > budget.LimitAmount)
            {
                var message = $"Budget limit {budget.LimitAmount:C} exceeded. Projected {projected:C}.";
                foreach (var entry in entries)
                {
                    warnings[entry.Index] = new BudgetWarningResult(true, budget.LimitAmount, projected, message);
                }
            }
        }

        if (warnings.Count > 0)
        {
            _logger.LogWarning("Budget warnings detected for {DocumentType} {DocumentId}.", source, documentId);
        }

        return warnings;
    }

    private async Task<List<BudgetSpendEntry>> GetConfirmedSpendAsync(int[] accountIds, CancellationToken cancellationToken)
    {
        var purchaseSpends = await _dbContext.PurchaseOrderLines
            .Where(line => line.AnalyticalAccountId.HasValue && accountIds.Contains(line.AnalyticalAccountId.Value))
            .Where(line => line.PurchaseOrder != null && line.PurchaseOrder.Status == PurchaseOrderStatus.Confirmed)
            .Select(line => new BudgetSpendEntry(
                line.AnalyticalAccountId!.Value,
                line.Total,
                line.PurchaseOrder!.PODate,
                BudgetSpendSource.PurchaseOrder,
                line.PurchaseOrderId))
            .ToListAsync(cancellationToken);

        var billSpends = await _dbContext.VendorBillLines
            .Where(line => line.AnalyticalAccountId.HasValue && accountIds.Contains(line.AnalyticalAccountId.Value))
            .Where(line => line.VendorBill != null && line.VendorBill.Status == VendorBillStatus.Confirmed)
            .Select(line => new BudgetSpendEntry(
                line.AnalyticalAccountId!.Value,
                line.Total,
                line.VendorBill!.BillDate,
                BudgetSpendSource.VendorBill,
                line.VendorBillId))
            .ToListAsync(cancellationToken);

        purchaseSpends.AddRange(billSpends);
        return purchaseSpends;
    }

    private record BudgetLineEntry(int Index, int? AnalyticalAccountId, decimal Total);

    private record BudgetSpendEntry(int AnalyticalAccountId, decimal Total, DateTime DocumentDate, BudgetSpendSource Source, int DocumentId);

    private enum BudgetSpendSource
    {
        PurchaseOrder,
        VendorBill
    }
}
