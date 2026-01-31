using System;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.AnalyticalBudgets;

public class BudgetPerformanceListItemViewModel
{
    public int AnalyticalBudgetId { get; set; }
    public string BudgetName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public BudgetType BudgetType { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal LimitAmount { get; set; }
    public decimal AchievedAmount { get; set; }
    public decimal RemainingBalance { get; set; }
    public decimal AchievedPercent { get; set; }
    public bool IsReadOnly { get; set; }
    public int? OriginalBudgetId { get; set; }
    public bool HasRevisions { get; set; }
    public BudgetStatus Status { get; set; }
    public bool ShouldArchive { get; set; }
}
