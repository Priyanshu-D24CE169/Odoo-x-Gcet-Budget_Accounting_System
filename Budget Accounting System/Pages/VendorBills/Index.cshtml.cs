using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.VendorBills;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<VendorBill> VendorBills { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string? SearchString { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterPaymentStatus { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.VendorBills
            .Include(v => v.Vendor)
            .Include(v => v.Lines)
            .Include(v => v.PurchaseOrder)
            .AsQueryable();

        if (!string.IsNullOrEmpty(SearchString))
        {
            query = query.Where(v => v.BillNumber.Contains(SearchString) || 
                                    (v.Reference != null && v.Reference.Contains(SearchString)));
        }

        if (!string.IsNullOrEmpty(FilterStatus))
        {
            if (Enum.TryParse<BillStatus>(FilterStatus, out var status))
            {
                query = query.Where(v => v.Status == status);
            }
        }

        var bills = await query
            .OrderByDescending(v => v.BillDate)
            .ToListAsync();

        // Filter by payment status
        if (!string.IsNullOrEmpty(FilterPaymentStatus))
        {
            bills = FilterPaymentStatus switch
            {
                "Paid" => bills.Where(b => b.TotalAmount - b.PaidAmount == 0).ToList(),
                "Partial" => bills.Where(b => b.PaidAmount > 0 && b.PaidAmount < b.TotalAmount).ToList(),
                "NotPaid" => bills.Where(b => b.PaidAmount == 0).ToList(),
                _ => bills
            };
        }

        VendorBills = bills;
    }
}
