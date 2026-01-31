using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public interface IAnalyticalBudgetService
{
    Task<IReadOnlyList<AnalyticalBudget>> GetBudgetsAsync(CancellationToken cancellationToken = default);
    Task<AnalyticalBudget?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AnalyticalBudget> CreateAsync(AnalyticalBudget budget, CancellationToken cancellationToken = default);
    Task<AnalyticalBudget> CreateRevisionAsync(int sourceBudgetId, AnalyticalBudget revision, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BudgetPerformanceResult>> GetBudgetPerformanceAsync(CancellationToken cancellationToken = default);
    Task<BudgetPerformanceResult?> GetBudgetPerformanceAsync(int budgetId, bool includeTransactions, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(int budgetId, BudgetStatus status, CancellationToken cancellationToken = default);
}
