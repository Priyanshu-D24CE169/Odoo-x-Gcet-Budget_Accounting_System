using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.Budgets;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Budget Budget { get; set; } = default!;

    public List<AnalyticalAccount> AnalyticalAccounts { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        Budget = new Budget
        {
            IsActive = true,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(1)
        };

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

        Budget.CreatedDate = DateTime.UtcNow;
        _context.Budgets.Add(Budget);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Budget '{Budget.Name}' has been created successfully.";
        return RedirectToPage("./Index");
    }
}
