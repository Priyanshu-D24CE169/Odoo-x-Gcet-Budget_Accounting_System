using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.SalesOrders;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<SalesOrder> SalesOrders { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string? SearchString { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterStatus { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.Lines)
            .Include(s => s.CustomerInvoices)
            .AsQueryable();

        if (!string.IsNullOrEmpty(SearchString))
        {
            query = query.Where(s => s.SONumber.Contains(SearchString) || 
                                    (s.Reference != null && s.Reference.Contains(SearchString)));
        }

        if (!string.IsNullOrEmpty(FilterStatus))
        {
            if (Enum.TryParse<SOStatus>(FilterStatus, out var status))
            {
                query = query.Where(s => s.Status == status);
            }
        }

        SalesOrders = await query
            .OrderByDescending(s => s.SODate)
            .ToListAsync();
    }
}
