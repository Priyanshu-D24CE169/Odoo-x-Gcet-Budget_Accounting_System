using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Budget_Accounting_System.Services;

public interface IBudgetService
{
    Task<BudgetAnalysisResult> GetBudgetAnalysis(int budgetId);
    Task<List<BudgetAnalysisResult>> GetBudgetAnalysisByPeriod(DateTime startDate, DateTime endDate);
    Task<BudgetAnalysisResult> GetBudgetAnalysisByAnalyticalAccount(int analyticalAccountId, DateTime startDate, DateTime endDate);
}

public class BudgetService : IBudgetService
{
    private readonly ApplicationDbContext _context;

    public BudgetService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BudgetAnalysisResult> GetBudgetAnalysis(int budgetId)
    {
        var budget = await _context.Budgets
            .Include(b => b.AnalyticalAccount)
            .Include(b => b.Revisions)
            .FirstOrDefaultAsync(b => b.Id == budgetId);

        if (budget == null)
            return new BudgetAnalysisResult();

        var actualAmount = await CalculateActualAmount(budget.AnalyticalAccountId, budget.StartDate, budget.EndDate, budget.Type);

        return new BudgetAnalysisResult
        {
            BudgetId = budget.Id,
            BudgetName = budget.Name,
            AnalyticalAccountName = budget.AnalyticalAccount.Name,
            BudgetType = budget.Type,
            PlannedAmount = budget.PlannedAmount,
            ActualAmount = actualAmount,
            Variance = budget.PlannedAmount - actualAmount,
            AchievementPercentage = budget.PlannedAmount > 0 ? (actualAmount / budget.PlannedAmount) * 100 : 0,
            RemainingBalance = budget.PlannedAmount - actualAmount,
            StartDate = budget.StartDate,
            EndDate = budget.EndDate,
            RevisionCount = budget.Revisions.Count
        };
    }

    public async Task<List<BudgetAnalysisResult>> GetBudgetAnalysisByPeriod(DateTime startDate, DateTime endDate)
    {
        var budgets = await _context.Budgets
            .Include(b => b.AnalyticalAccount)
            .Include(b => b.Revisions)
            .Where(b => b.IsActive && b.StartDate <= endDate && b.EndDate >= startDate)
            .ToListAsync();

        var results = new List<BudgetAnalysisResult>();

        foreach (var budget in budgets)
        {
            var actualAmount = await CalculateActualAmount(budget.AnalyticalAccountId, budget.StartDate, budget.EndDate, budget.Type);

            results.Add(new BudgetAnalysisResult
            {
                BudgetId = budget.Id,
                BudgetName = budget.Name,
                AnalyticalAccountName = budget.AnalyticalAccount.Name,
                BudgetType = budget.Type,
                PlannedAmount = budget.PlannedAmount,
                ActualAmount = actualAmount,
                Variance = budget.PlannedAmount - actualAmount,
                AchievementPercentage = budget.PlannedAmount > 0 ? (actualAmount / budget.PlannedAmount) * 100 : 0,
                RemainingBalance = budget.PlannedAmount - actualAmount,
                StartDate = budget.StartDate,
                EndDate = budget.EndDate,
                RevisionCount = budget.Revisions.Count
            });
        }

        return results;
    }

    public async Task<BudgetAnalysisResult> GetBudgetAnalysisByAnalyticalAccount(int analyticalAccountId, DateTime startDate, DateTime endDate)
    {
        var account = await _context.AnalyticalAccounts.FindAsync(analyticalAccountId);
        if (account == null)
            return new BudgetAnalysisResult();

        var budgets = await _context.Budgets
            .Where(b => b.AnalyticalAccountId == analyticalAccountId && 
                       b.IsActive && 
                       b.StartDate <= endDate && 
                       b.EndDate >= startDate)
            .ToListAsync();

        var totalPlanned = budgets.Sum(b => b.PlannedAmount);
        var incomeActual = await CalculateActualAmount(analyticalAccountId, startDate, endDate, BudgetType.Income);
        var expenseActual = await CalculateActualAmount(analyticalAccountId, startDate, endDate, BudgetType.Expense);

        return new BudgetAnalysisResult
        {
            AnalyticalAccountName = account.Name,
            PlannedAmount = totalPlanned,
            ActualAmount = incomeActual + expenseActual,
            Variance = totalPlanned - (incomeActual + expenseActual),
            AchievementPercentage = totalPlanned > 0 ? ((incomeActual + expenseActual) / totalPlanned) * 100 : 0,
            RemainingBalance = totalPlanned - (incomeActual + expenseActual),
            StartDate = startDate,
            EndDate = endDate
        };
    }

    private async Task<decimal> CalculateActualAmount(int analyticalAccountId, DateTime startDate, DateTime endDate, BudgetType budgetType)
    {
        decimal total = 0;

        if (budgetType == BudgetType.Income)
        {
            var invoiceLines = await _context.CustomerInvoiceLines
                .Include(l => l.CustomerInvoice)
                .Where(l => l.AnalyticalAccountId == analyticalAccountId &&
                           l.CustomerInvoice.Status == InvoiceStatus.Posted &&
                           l.CustomerInvoice.InvoiceDate >= startDate &&
                           l.CustomerInvoice.InvoiceDate <= endDate)
                .ToListAsync();

            total = invoiceLines.Sum(l => l.LineTotal);
        }
        else if (budgetType == BudgetType.Expense)
        {
            var billLines = await _context.VendorBillLines
                .Include(l => l.VendorBill)
                .Where(l => l.AnalyticalAccountId == analyticalAccountId &&
                           l.VendorBill.Status == BillStatus.Posted &&
                           l.VendorBill.BillDate >= startDate &&
                           l.VendorBill.BillDate <= endDate)
                .ToListAsync();

            total = billLines.Sum(l => l.LineTotal);
        }

        return total;
    }
}

public class BudgetAnalysisResult
{
    public int BudgetId { get; set; }
    public string BudgetName { get; set; } = string.Empty;
    public string AnalyticalAccountName { get; set; } = string.Empty;
    public BudgetType BudgetType { get; set; }
    public decimal PlannedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Variance { get; set; }
    public decimal AchievementPercentage { get; set; }
    public decimal RemainingBalance { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int RevisionCount { get; set; }
}
