using System;
using ShivFurnitureERP.Services;

namespace ShivFurnitureERP.ViewModels.AnalyticalBudgets;

public class BudgetTransactionViewModel
{
    public string DocumentType { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Counterparty { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int DocumentId { get; set; }
    public BudgetTransactionType Type { get; set; }
}
