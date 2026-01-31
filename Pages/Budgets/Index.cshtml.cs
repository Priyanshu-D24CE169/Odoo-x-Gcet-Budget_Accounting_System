using Microsoft.AspNetCore.Mvc;
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

    public IList<Budget> Budgets { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string? FilterPeriod { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterType { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.Budgets
            .Include(b => b.AnalyticalAccount)
            .Include(b => b.Revisions)
            .AsQueryable();

        if (!string.IsNullOrEmpty(FilterPeriod))
        {
            if (DateTime.TryParse(FilterPeriod + "-01", out var filterDate))
            {
                var startOfMonth = new DateTime(filterDate.Year, filterDate.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                
                query = query.Where(b => 
                    (b.StartDate <= endOfMonth && b.EndDate >= startOfMonth));
            }
        }

        if (!string.IsNullOrEmpty(FilterType))
        {
            if (Enum.TryParse<BudgetType>(FilterType, out var type))
            {
                query = query.Where(b => b.Type == type);
            }
        }

        Budgets = await query
            .OrderByDescending(b => b.IsActive)
            .ThenByDescending(b => b.StartDate)
            .ToListAsync();
    }
}
