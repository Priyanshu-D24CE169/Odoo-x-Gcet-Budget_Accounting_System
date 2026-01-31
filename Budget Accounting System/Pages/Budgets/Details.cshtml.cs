using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using Budget_Accounting_System.Services;

namespace Budget_Accounting_System.Pages.Budgets;

[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IBudgetActualService _budgetActualService;

    public DetailsModel(ApplicationDbContext context, IBudgetActualService budgetActualService)
    {
        _context = context;
        _budgetActualService = budgetActualService;
    }

    public Budget Budget { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var budget = await _context.Budgets
            .Include(b => b.Lines)
                .ThenInclude(l => l.AnalyticalAccount)
            .Include(b => b.RevisedFrom)
            .Include(b => b.RevisedWith)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (budget == null)
        {
            return NotFound();
        }

        Budget = budget;

        // Compute actuals if confirmed
        if (Budget.State == BudgetState.Confirmed)
        {
            await _budgetActualService.ComputeAllActualsAsync(Budget);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync(int id)
    {
        var budget = await _context.Budgets.FindAsync(id);
        if (budget == null)
        {
            return NotFound();
        }

        budget.State = BudgetState.Confirmed;
        budget.ConfirmedDate = DateTime.UtcNow;
        budget.ConfirmedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Budget '{budget.Name}' has been confirmed.";
        return RedirectToPage("./Details", new { id });
    }
}
