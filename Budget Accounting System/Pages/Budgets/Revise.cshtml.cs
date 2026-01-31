using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using Budget_Accounting_System.Services;

namespace Budget_Accounting_System.Pages.Budgets;

[Authorize(Roles = "Admin")]
public class ReviseModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IBudgetService _budgetService;

    public ReviseModel(ApplicationDbContext context, IBudgetService budgetService)
    {
        _context = context;
        _budgetService = budgetService;
    }

    public Budget Budget { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        var budget = await _context.Budgets
            .Include(b => b.Lines)
                .ThenInclude(l => l.AnalyticalAccount)
            .Include(b => b.RevisedWith)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (budget == null)
            return NotFound();

        if (budget.State != BudgetState.Confirmed)
        {
            TempData["ErrorMessage"] = $"Can only revise Confirmed budgets. Current state: {budget.State}";
            return RedirectToPage("./Details", new { id });
        }

        if (budget.HasRevision)
        {
            TempData["ErrorMessage"] = "This budget has already been revised.";
            return RedirectToPage("./Details", new { id });
        }

        Budget = budget;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
            return NotFound();

        var userName = User.Identity?.Name ?? "System";
        var (success, message, revisedBudget) = await _budgetService.ReviseBudgetAsync(id.Value, userName);

        if (success && revisedBudget != null)
        {
            TempData["SuccessMessage"] = message + " You can now edit the budget lines.";
            return RedirectToPage("./Edit", new { id = revisedBudget.Id });
        }
        else
        {
            TempData["ErrorMessage"] = message;
            return RedirectToPage("./Details", new { id });
        }
    }
}
