using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.Budgets;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Budget> Budgets { get; set; } = new();
    public string? FilterState { get; set; }

    public async Task OnGetAsync(string? filterState)
    {
        FilterState = filterState;

        var query = _context.Budgets
            .Include(b => b.Lines)
                .ThenInclude(l => l.AnalyticalAccount)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filterState))
        {
            if (Enum.TryParse<BudgetState>(filterState, out var state))
            {
                query = query.Where(b => b.State == state);
            }
        }

        Budgets = await query
            .OrderByDescending(b => b.CreatedDate)
            .ToListAsync();
    }
}
