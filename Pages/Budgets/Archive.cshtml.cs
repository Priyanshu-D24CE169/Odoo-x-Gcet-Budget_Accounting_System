using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using Budget_Accounting_System.Services;

namespace Budget_Accounting_System.Pages.Budgets;

public class ArchiveModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IBudgetService _budgetService;

    public ArchiveModel(ApplicationDbContext context, IBudgetService budgetService)
    {
        _context = context;
        _budgetService = budgetService;
    }

    [BindProperty]
    public Budget Budget { get; set; } = default!;
    public decimal AchievedAmount { get; set; }
    public decimal AchievementPercentage { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var budget = await _context.Budgets
            .Include(b => b.AnalyticalAccount)
            .Include(b => b.Revisions)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (budget == null)
        {
            return NotFound();
        }

        Budget = budget;

        // Get achievement data
        var analysis = await _budgetService.GetBudgetAnalysis(budget.Id);
        AchievedAmount = analysis.ActualAmount;
        AchievementPercentage = analysis.AchievementPercentage;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var budget = await _context.Budgets.FindAsync(Budget.Id);
        if (budget == null)
        {
            return NotFound();
        }

        budget.IsActive = false;
        budget.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Budget '{budget.Name}' has been archived.";
        return RedirectToPage("./Index");
    }
}
