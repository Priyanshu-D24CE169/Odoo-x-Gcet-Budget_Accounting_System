using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using Budget_Accounting_System.Services;
using System.ComponentModel.DataAnnotations;

namespace Budget_Accounting_System.Pages.Budgets;

public class ReviseModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IBudgetService _budgetService;

    public ReviseModel(ApplicationDbContext context, IBudgetService budgetService)
    {
        _context = context;
        _budgetService = budgetService;
    }

    public Budget OriginalBudget { get; set; } = default!;

    [BindProperty]
    public int OriginalBudgetId { get; set; }

    [BindProperty]
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "New amount must be greater than zero")]
    public decimal NewAmount { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Please provide a reason for the revision")]
    public string RevisionReason { get; set; } = string.Empty;

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

        if (!budget.IsActive)
        {
            TempData["ErrorMessage"] = "Cannot revise an archived budget.";
            return RedirectToPage("./Details", new { id });
        }

        OriginalBudget = budget;
        OriginalBudgetId = budget.Id;
        NewAmount = budget.PlannedAmount;

        // Get current achievement
        var analysis = await _budgetService.GetBudgetAnalysis(budget.Id);
        AchievedAmount = analysis.ActualAmount;
        AchievementPercentage = analysis.AchievementPercentage;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var originalBudget = await _context.Budgets
            .Include(b => b.AnalyticalAccount)
            .FirstOrDefaultAsync(b => b.Id == OriginalBudgetId);

        if (originalBudget == null)
        {
            return NotFound();
        }

        OriginalBudget = originalBudget;

        if (!ModelState.IsValid)
        {
            var analysis = await _budgetService.GetBudgetAnalysis(originalBudget.Id);
            AchievedAmount = analysis.ActualAmount;
            AchievementPercentage = analysis.AchievementPercentage;
            return Page();
        }

        if (NewAmount == originalBudget.PlannedAmount)
        {
            ModelState.AddModelError("NewAmount", "New amount must be different from the current amount.");
            var analysis = await _budgetService.GetBudgetAnalysis(originalBudget.Id);
            AchievedAmount = analysis.ActualAmount;
            AchievementPercentage = analysis.AchievementPercentage;
            return Page();
        }

        // Create revision record
        var revision = new BudgetRevision
        {
            BudgetId = originalBudget.Id,
            OldAmount = originalBudget.PlannedAmount,
            NewAmount = NewAmount,
            Reason = RevisionReason,
            RevisionDate = DateTime.UtcNow,
            RevisedBy = User.Identity?.Name ?? "System"
        };

        _context.BudgetRevisions.Add(revision);

        // Update budget with new amount
        originalBudget.PlannedAmount = NewAmount;
        originalBudget.ModifiedDate = DateTime.UtcNow;

        // Update budget name to include revision date
        if (!originalBudget.Name.Contains("Rev"))
        {
            originalBudget.Name = $"{originalBudget.Name} (Rev {DateTime.Now:DD MM YYYY})";
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Budget '{originalBudget.Name}' has been revised successfully. New amount: {NewAmount:C}";
        return RedirectToPage("./Details", new { id = originalBudget.Id });
    }
}
