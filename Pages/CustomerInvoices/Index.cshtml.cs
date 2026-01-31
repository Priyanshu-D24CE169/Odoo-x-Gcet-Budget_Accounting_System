using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.CustomerInvoices;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<CustomerInvoice> CustomerInvoices { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string? SearchString { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterPaymentStatus { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.CustomerInvoices
            .Include(c => c.Customer)
            .Include(c => c.Lines)
            .Include(c => c.SalesOrder)
            .AsQueryable();

        if (!string.IsNullOrEmpty(SearchString))
        {
            query = query.Where(c => c.InvoiceNumber.Contains(SearchString) || 
                                    (c.Reference != null && c.Reference.Contains(SearchString)));
        }

        if (!string.IsNullOrEmpty(FilterStatus))
        {
            if (Enum.TryParse<InvoiceStatus>(FilterStatus, out var status))
            {
                query = query.Where(c => c.Status == status);
            }
        }

        var invoices = await query
            .OrderByDescending(c => c.InvoiceDate)
            .ToListAsync();

        // Filter by payment status
        if (!string.IsNullOrEmpty(FilterPaymentStatus))
        {
            invoices = FilterPaymentStatus switch
            {
                "Paid" => invoices.Where(i => i.TotalAmount - i.PaidAmount == 0).ToList(),
                "Partial" => invoices.Where(i => i.PaidAmount > 0 && i.PaidAmount < i.TotalAmount).ToList(),
                "NotPaid" => invoices.Where(i => i.PaidAmount == 0).ToList(),
                _ => invoices
            };
        }

        CustomerInvoices = invoices;
    }
}
