using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.AnalyticalAccounts;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public AnalyticalAccount AnalyticalAccount { get; set; } = default!;

    public List<AnalyticalAccount> ParentAccounts { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        AnalyticalAccount = new AnalyticalAccount
        {
            IsActive = true
        };

        ParentAccounts = await _context.AnalyticalAccounts
            .Where(a => a.IsActive)
            .OrderBy(a => a.Code)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ParentAccounts = await _context.AnalyticalAccounts
                .Where(a => a.IsActive)
                .OrderBy(a => a.Code)
                .ToListAsync();
            return Page();
        }

        // Check if code already exists
        if (await _context.AnalyticalAccounts.AnyAsync(a => a.Code == AnalyticalAccount.Code))
        {
            ModelState.AddModelError("AnalyticalAccount.Code", "This code already exists.");
            ParentAccounts = await _context.AnalyticalAccounts
                .Where(a => a.IsActive)
                .OrderBy(a => a.Code)
                .ToListAsync();
            return Page();
        }

        AnalyticalAccount.CreatedDate = DateTime.UtcNow;
        _context.AnalyticalAccounts.Add(AnalyticalAccount);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Analytical Account '{AnalyticalAccount.Name}' has been created successfully.";
        return RedirectToPage("./Index");
    }
}
