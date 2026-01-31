using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.Budgets;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Budget Budget { get; set; } = default!;

    public List<AnalyticalAccount> AnalyticalAccounts { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var budget = await _context.Budgets.FirstOrDefaultAsync(m => m.Id == id);
        if (budget == null)
        {
            return NotFound();
        }

        Budget = budget;

        AnalyticalAccounts = await _context.AnalyticalAccounts
            .Where(a => a.IsActive)
            .OrderBy(a => a.Code)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            AnalyticalAccounts = await _context.AnalyticalAccounts
                .Where(a => a.IsActive)
                .OrderBy(a => a.Code)
                .ToListAsync();
            return Page();
        }

        // Validate date range
        if (Budget.EndDate <= Budget.StartDate)
        {
            ModelState.AddModelError("Budget.EndDate", "End date must be after start date.");
            AnalyticalAccounts = await _context.AnalyticalAccounts
                .Where(a => a.IsActive)
                .OrderBy(a => a.Code)
                .ToListAsync();
            return Page();
        }

        Budget.ModifiedDate = DateTime.UtcNow;
        _context.Attach(Budget).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!BudgetExists(Budget.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        TempData["SuccessMessage"] = $"Budget '{Budget.Name}' has been updated successfully.";
        return RedirectToPage("./Index");
    }

    private bool BudgetExists(int id)
    {
        return _context.Budgets.Any(e => e.Id == id);
    }
}
