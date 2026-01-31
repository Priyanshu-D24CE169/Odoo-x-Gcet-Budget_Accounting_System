using System;
using System.Collections.Generic;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public class BudgetPerformanceResult
{
    public required AnalyticalBudget Budget { get; init; }
    public decimal AchievedAmount { get; init; }
    public decimal RemainingBalance { get; init; }
    public decimal AchievedPercent { get; init; }
    public decimal BalanceForChart { get; init; }
    public bool MeetsIncomeTarget { get; init; }
    public IReadOnlyList<BudgetTransaction> Transactions { get; init; } = new List<BudgetTransaction>();
}

public record BudgetTransaction(
    BudgetTransactionType Type,
    int DocumentId,
    string Reference,
    DateTime Date,
    decimal Amount,
    string Counterparty);

public enum BudgetTransactionType
{
    CustomerInvoice,
    VendorBill,
    PurchaseOrder,
    SalesOrder
}
