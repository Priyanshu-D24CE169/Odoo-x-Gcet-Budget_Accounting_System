using Microsoft.AspNetCore.Mvc.Rendering;

namespace ShivFurnitureERP.ViewModels;

public class DashboardViewModel
{
    public DateTime FilterStart { get; set; }
    public DateTime FilterEnd { get; set; }
    public int? SelectedAccountId { get; set; }
    public IReadOnlyList<SelectListItem> AccountOptions { get; set; } = Array.Empty<SelectListItem>();

    public decimal TotalBudgetedAmount { get; set; }
    public decimal TotalActualAmount { get; set; }
    public decimal BudgetUtilizationPercent { get; set; }
    public decimal RemainingBudget => Math.Max(TotalBudgetedAmount - TotalActualAmount, 0m);

    public IncomeExpenseSummary IncomeExpense { get; set; } = new();
    public IReadOnlyList<CostCenterPerformanceViewModel> CostCenters { get; set; } = Array.Empty<CostCenterPerformanceViewModel>();
    public PaymentStatusSummary PaymentStatus { get; set; } = new();
    public IReadOnlyList<BudgetAlertViewModel> Alerts { get; set; } = Array.Empty<BudgetAlertViewModel>();
}

public class IncomeExpenseSummary
{
    public decimal BudgetedIncome { get; set; }
    public decimal ActualIncome { get; set; }
    public decimal BudgetedExpense { get; set; }
    public decimal ActualExpense { get; set; }

    public decimal IncomeUtilizationPercent => BudgetedIncome <= 0 ? 0 : Math.Round(Math.Min(100m, ActualIncome / BudgetedIncome * 100m), 2, MidpointRounding.AwayFromZero);
    public decimal ExpenseUtilizationPercent => BudgetedExpense <= 0 ? 0 : Math.Round(Math.Min(100m, ActualExpense / BudgetedExpense * 100m), 2, MidpointRounding.AwayFromZero);
    public decimal RemainingIncome => Math.Max(BudgetedIncome - ActualIncome, 0m);
    public decimal RemainingExpense => Math.Max(BudgetedExpense - ActualExpense, 0m);
}

public class CostCenterPerformanceViewModel
{
    public int AnalyticalAccountId { get; set; }
    public string AnalyticalAccountName { get; set; } = string.Empty;
    public decimal BudgetedAmount { get; set; }
    public decimal ActualIncome { get; set; }
    public decimal ActualExpense { get; set; }
    public decimal ActualAmount => ActualIncome + ActualExpense;
    public decimal UtilizationPercent => BudgetedAmount <= 0 ? 0 : Math.Round(Math.Min(100m, ActualAmount / BudgetedAmount * 100m), 2, MidpointRounding.AwayFromZero);
    public decimal RemainingAmount => Math.Max(BudgetedAmount - ActualAmount, 0m);
}

public class PaymentStatusSummary
{
    public IReadOnlyList<PaymentStatusItem> IncomeStatuses { get; set; } = Array.Empty<PaymentStatusItem>();
    public IReadOnlyList<PaymentStatusItem> ExpenseStatuses { get; set; } = Array.Empty<PaymentStatusItem>();
}

public class PaymentStatusItem
{
    public string Label { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal OutstandingAmount { get; set; }
}

public class BudgetAlertViewModel
{
    public string AccountName { get; set; } = string.Empty;
    public decimal BudgetedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal UtilizationPercent { get; set; }
    public bool IsExceeded { get; set; }
}
