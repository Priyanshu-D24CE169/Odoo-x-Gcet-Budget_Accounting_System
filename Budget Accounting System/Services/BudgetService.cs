using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Budget_Accounting_System.Services;

public interface IBudgetService
{
    Task<List<BudgetAnalysisResult>> GetBudgetAnalysisByPeriod(DateTime startDate, DateTime endDate);
    Task<(bool Success, string Message)> ConfirmBudgetAsync(int budgetId, string userName);
    Task<(bool Success, string Message, Budget? RevisedBudget)> ReviseBudgetAsync(int budgetId, string userName);
    Task<(bool Success, string Message)> ArchiveBudgetAsync(int budgetId, string userName);
    Task<(bool Success, string Message)> CancelBudgetAsync(int budgetId, string userName);
    Task<List<Budget>> GetActiveBudgetsForPeriodAsync(DateTime date);
}

public class BudgetService : IBudgetService
{
    private readonly ApplicationDbContext _context;
    private readonly IBudgetActualService _budgetActualService;
    private readonly ILogger<BudgetService> _logger;

    public BudgetService(
        ApplicationDbContext context, 
        IBudgetActualService budgetActualService,
        ILogger<BudgetService> logger)
    {
        _context = context;
        _budgetActualService = budgetActualService;
        _logger = logger;
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

    public async Task<(bool Success, string Message)> ConfirmBudgetAsync(int budgetId, string userName)
    {
        try
        {
            var budget = await _context.Budgets
                .Include(b => b.Lines)
                .FirstOrDefaultAsync(b => b.Id == budgetId);

            if (budget == null)
                return (false, "Budget not found.");

            if (budget.State != BudgetState.Draft)
                return (false, $"Cannot confirm budget in {budget.State} state.");

            if (!budget.Lines.Any())
                return (false, "Cannot confirm budget without budget lines.");

            budget.State = BudgetState.Confirmed;
            budget.ConfirmedDate = DateTime.UtcNow;
            budget.ConfirmedBy = userName;
            budget.ModifiedDate = DateTime.UtcNow;
            budget.ModifiedBy = userName;

            await _context.SaveChangesAsync();
            await _budgetActualService.ComputeAllActualsAsync(budget);

            _logger.LogInformation("Budget {BudgetName} (ID: {Id}) confirmed by {User}",
                budget.Name, budget.Id, userName);

            return (true, $"Budget '{budget.Name}' confirmed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming budget {Id}", budgetId);
            return (false, "An error occurred while confirming the budget.");
        }
    }

    public async Task<(bool Success, string Message, Budget? RevisedBudget)> ReviseBudgetAsync(int budgetId, string userName)
    {
        try
        {
            var originalBudget = await _context.Budgets
                .Include(b => b.Lines)
                .FirstOrDefaultAsync(b => b.Id == budgetId);

            if (originalBudget == null)
                return (false, "Budget not found.", null);

            if (originalBudget.State != BudgetState.Confirmed)
                return (false, $"Can only revise Confirmed budgets.", null);

            if (originalBudget.HasRevision)
                return (false, "This budget has already been revised.", null);

            // Create revised budget
            var revisedBudget = new Budget
            {
                Name = originalBudget.Name,
                StartDate = originalBudget.StartDate,
                EndDate = originalBudget.EndDate,
                Notes = $"Revision of budget created on {originalBudget.CreatedDate:dd MMM yyyy}",
                State = BudgetState.Draft,
                RevisedFromId = originalBudget.Id,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = userName,
                RevisedDate = DateTime.UtcNow,
                RevisedBy = userName
            };

            // Copy all budget lines
            foreach (var line in originalBudget.Lines)
            {
                revisedBudget.Lines.Add(new BudgetLine
                {
                    AnalyticalAccountId = line.AnalyticalAccountId,
                    Type = line.Type,
                    BudgetedAmount = line.BudgetedAmount,
                    Description = line.Description
                });
            }

            _context.Budgets.Add(revisedBudget);
            await _context.SaveChangesAsync();

            // Update original budget
            originalBudget.State = BudgetState.Revised;
            originalBudget.RevisedWithId = revisedBudget.Id;
            originalBudget.ModifiedDate = DateTime.UtcNow;
            originalBudget.ModifiedBy = userName;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Budget {Name} (ID: {Id}) revised by {User}. New ID: {NewId}",
                originalBudget.Name, originalBudget.Id, userName, revisedBudget.Id);

            return (true, $"Budget '{originalBudget.Name}' has been revised.", revisedBudget);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revising budget {Id}", budgetId);
            return (false, "An error occurred while revising the budget.", null);
        }
    }

    public async Task<(bool Success, string Message)> ArchiveBudgetAsync(int budgetId, string userName)
    {
        try
        {
            var budget = await _context.Budgets.FindAsync(budgetId);

            if (budget == null)
                return (false, "Budget not found.");

            if (budget.State != BudgetState.Confirmed && budget.State != BudgetState.Revised)
                return (false, $"Can only archive Confirmed or Revised budgets.");

            budget.State = BudgetState.Archived;
            budget.ArchivedDate = DateTime.UtcNow;
            budget.ArchivedBy = userName;
            budget.ModifiedDate = DateTime.UtcNow;
            budget.ModifiedBy = userName;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Budget {Name} (ID: {Id}) archived by {User}",
                budget.Name, budget.Id, userName);

            return (true, $"Budget '{budget.Name}' archived successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving budget {Id}", budgetId);
            return (false, "An error occurred while archiving the budget.");
        }
    }

    public async Task<(bool Success, string Message)> CancelBudgetAsync(int budgetId, string userName)
    {
        try
        {
            var budget = await _context.Budgets.FindAsync(budgetId);

            if (budget == null)
                return (false, "Budget not found.");

            if (budget.State != BudgetState.Draft)
                return (false, $"Can only cancel Draft budgets.");

            budget.State = BudgetState.Cancelled;
            budget.CancelledDate = DateTime.UtcNow;
            budget.CancelledBy = userName;
            budget.ModifiedDate = DateTime.UtcNow;
            budget.ModifiedBy = userName;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Budget {Name} (ID: {Id}) cancelled by {User}",
                budget.Name, budget.Id, userName);

            return (true, $"Budget '{budget.Name}' cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling budget {Id}", budgetId);
            return (false, "An error occurred while cancelling the budget.");
        }
    }

    public async Task<List<Budget>> GetActiveBudgetsForPeriodAsync(DateTime date)
    {
        return await _context.Budgets
            .Include(b => b.Lines)
                .ThenInclude(l => l.AnalyticalAccount)
            .Where(b => b.State == BudgetState.Confirmed &&
                       b.StartDate <= date &&
                       b.EndDate >= date)
            .OrderBy(b => b.StartDate)
            .ToListAsync();
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


