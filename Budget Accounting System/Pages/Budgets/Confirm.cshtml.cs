using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using Budget_Accounting_System.Services;

namespace Budget_Accounting_System.Pages.Budgets;

[Authorize(Roles = "Admin")]
public class ConfirmModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IBudgetService _budgetService;

    public ConfirmModel(ApplicationDbContext context, IBudgetService budgetService)
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
            .FirstOrDefaultAsync(m => m.Id == id);

        if (budget == null)
            return NotFound();

        if (budget.State != BudgetState.Draft)
        {
            TempData["ErrorMessage"] = $"Cannot confirm budget in {budget.State} state.";
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
        var (success, message) = await _budgetService.ConfirmBudgetAsync(id.Value, userName);

        if (success)
        {
            TempData["SuccessMessage"] = message;
            return RedirectToPage("./Details", new { id });
        }
        else
        {
            TempData["ErrorMessage"] = message;
            return RedirectToPage("./Details", new { id });
        }
    }
}
