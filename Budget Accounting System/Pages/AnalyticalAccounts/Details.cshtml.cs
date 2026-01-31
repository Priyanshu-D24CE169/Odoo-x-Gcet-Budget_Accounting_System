using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.AnalyticalAccounts;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public AnalyticalAccount AnalyticalAccount { get; set; } = default!;
    public List<AnalyticalAccount> Children { get; set; } = default!;
    public int BudgetCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var account = await _context.AnalyticalAccounts
            .Include(a => a.Parent)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (account == null)
        {
            return NotFound();
        }

        AnalyticalAccount = account;

        Children = await _context.AnalyticalAccounts
            .Where(a => a.ParentId == id)
            .OrderBy(a => a.Code)
            .ToListAsync();

        // Count budget lines that use this analytical account
        BudgetCount = await _context.BudgetLines.CountAsync(bl => bl.AnalyticalAccountId == id);

        return Page();
    }
}
