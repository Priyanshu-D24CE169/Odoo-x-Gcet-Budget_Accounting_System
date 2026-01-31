using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Budget_Accounting_System.Services;

public interface IBudgetService
{
    Task<List<BudgetAnalysisResult>> GetBudgetAnalysisByPeriod(DateTime startDate, DateTime endDate);
}

public class BudgetService : IBudgetService
{
    private readonly ApplicationDbContext _context;
    private readonly IBudgetActualService _budgetActualService;

    public BudgetService(ApplicationDbContext context, IBudgetActualService budgetActualService)
    {
        _context = context;
        _budgetActualService = budgetActualService;
    }

    public async Task<List<BudgetAnalysisResult>> GetBudgetAnalysisByPeriod(DateTime startDate, DateTime endDate)
    {
        var budgets = await _context.Budgets
            .Include(b => b.Lines)
                .ThenInclude(l => l.AnalyticalAccount)
            .Where(b => b.State == BudgetState.Confirmed && 
                       b.StartDate <= endDate && 
                       b.EndDate >= startDate)
            .ToListAsync();

        var results = new List<BudgetAnalysisResult>();

        foreach (var budget in budgets)
        {
            // Compute actuals for all lines
            await _budgetActualService.ComputeAllActualsAsync(budget);

            var totalPlanned = budget.Lines.Sum(l => l.BudgetedAmount);
            var totalActual = budget.Lines.Sum(l => l.AchievedAmount);

            results.Add(new BudgetAnalysisResult
            {
                BudgetId = budget.Id,
                BudgetName = budget.Name,
                PlannedAmount = totalPlanned,
                ActualAmount = totalActual,
                Variance = totalPlanned - totalActual,
                AchievementPercentage = totalPlanned > 0 ? (totalActual / totalPlanned) * 100 : 0,
                RemainingBalance = totalPlanned - totalActual,
                StartDate = budget.StartDate,
                EndDate = budget.EndDate
            });
        }

        return results;
    }
}

public class BudgetAnalysisResult
{
    public int BudgetId { get; set; }
    public string BudgetName { get; set; } = string.Empty;
    public string AnalyticalAccountName { get; set; } = string.Empty;
    public decimal PlannedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Variance { get; set; }
    public decimal AchievementPercentage { get; set; }
    public decimal RemainingBalance { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int RevisionCount { get; set; }
    public BudgetType BudgetType { get; set; }
}

