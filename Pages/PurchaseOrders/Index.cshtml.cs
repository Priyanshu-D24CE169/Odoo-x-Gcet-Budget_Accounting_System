using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.PurchaseOrders;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IList<PurchaseOrder> PurchaseOrders { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string? SearchString { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterStatus { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            _logger.LogInformation("Loading Purchase Orders. Search: {Search}, Filter: {Filter}", SearchString, FilterStatus);

            var query = _context.PurchaseOrders
                .Include(p => p.Vendor)
                .Include(p => p.Lines)
                .Include(p => p.VendorBills)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(p => p.PONumber.Contains(SearchString) || 
                                        (p.Reference != null && p.Reference.Contains(SearchString)));
            }

            if (!string.IsNullOrEmpty(FilterStatus))
            {
                if (Enum.TryParse<POStatus>(FilterStatus, out var status))
                {
                    query = query.Where(p => p.Status == status);
                }
            }

            PurchaseOrders = await query
                .OrderByDescending(p => p.PODate)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} Purchase Orders", PurchaseOrders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Purchase Orders");
            PurchaseOrders = new List<PurchaseOrder>();
        }
    }
}

