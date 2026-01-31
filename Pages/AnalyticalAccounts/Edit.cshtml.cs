using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.AnalyticalAccounts;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public AnalyticalAccount AnalyticalAccount { get; set; } = default!;

    public List<AnalyticalAccount> ParentAccounts { get; set; } = default!;

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

        // Exclude self and descendants from parent options
        ParentAccounts = await _context.AnalyticalAccounts
            .Where(a => a.IsActive && a.Id != id && a.ParentId != id)
            .OrderBy(a => a.Code)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ParentAccounts = await _context.AnalyticalAccounts
                .Where(a => a.IsActive && a.Id != AnalyticalAccount.Id)
                .OrderBy(a => a.Code)
                .ToListAsync();
            return Page();
        }

        AnalyticalAccount.ModifiedDate = DateTime.UtcNow;
        _context.Attach(AnalyticalAccount).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!AnalyticalAccountExists(AnalyticalAccount.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        TempData["SuccessMessage"] = $"Analytical Account '{AnalyticalAccount.Name}' has been updated successfully.";
        return RedirectToPage("./Index");
    }

    private bool AnalyticalAccountExists(int id)
    {
        return _context.AnalyticalAccounts.Any(e => e.Id == id);
    }
}
