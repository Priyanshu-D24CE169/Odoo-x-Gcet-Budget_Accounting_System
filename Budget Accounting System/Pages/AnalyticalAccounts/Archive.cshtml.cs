using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.AnalyticalAccounts;

public class ArchiveModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ArchiveModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public AnalyticalAccount AnalyticalAccount { get; set; } = default!;
    public int BudgetCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var account = await _context.AnalyticalAccounts.FirstOrDefaultAsync(m => m.Id == id);
        if (account == null)
        {
            return NotFound();
        }

        AnalyticalAccount = account;
        // Count confirmed budgets using this analytical account
        BudgetCount = await _context.BudgetLines
            .Include(bl => bl.Budget)
            .CountAsync(bl => bl.AnalyticalAccountId == id && bl.Budget.State == BudgetState.Confirmed);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var account = await _context.AnalyticalAccounts.FindAsync(AnalyticalAccount.Id);
        if (account == null)
        {
            return NotFound();
        }

        account.IsActive = false;
        account.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Analytical Account '{account.Name}' has been archived.";
        return RedirectToPage("./Index");
    }
}
