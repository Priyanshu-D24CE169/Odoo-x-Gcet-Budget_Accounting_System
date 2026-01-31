using System.Collections.Generic;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.AnalyticalBudgets;

public class BudgetDetailsViewModel : BudgetPerformanceListItemViewModel
{
    public decimal BalanceForChart { get; set; }
    public string AnalyticalAccountName { get; set; } = string.Empty;
    public IList<BudgetTransactionViewModel> Transactions { get; set; } = new List<BudgetTransactionViewModel>();
    public int? LatestRevisionId { get; set; }
    public bool ShowPerformance => Status == BudgetStatus.Confirmed;
}
