using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.AnalyticalAccounts;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<AnalyticalAccount> AnalyticalAccounts { get; set; } = default!;

    public async Task OnGetAsync()
    {
        AnalyticalAccounts = await _context.AnalyticalAccounts
            .Include(a => a.Parent)
            .OrderBy(a => a.Code)
            .ToListAsync();
    }
}
