using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using System.Security.Claims;

namespace Budget_Accounting_System.Pages.Portal;

[Authorize(Roles = "Contact")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Contact? ContactInfo { get; set; }
    public List<CustomerInvoice> MyInvoices { get; set; } = new();
    public List<VendorBill> MyBills { get; set; } = new();
    public decimal TotalInvoicesAmount { get; set; }
    public decimal TotalBillsAmount { get; set; }
    public decimal UnpaidInvoices { get; set; }
    public decimal UnpaidBills { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var contactIdClaim = User.FindFirstValue("ContactId");
        if (string.IsNullOrEmpty(contactIdClaim) || !int.TryParse(contactIdClaim, out int contactId))
        {
            return RedirectToPage("/Account/AccessDenied");
        }

        ContactInfo = await _context.Contacts.FindAsync(contactId);
        if (ContactInfo == null)
        {
            return NotFound();
        }

        // Load invoices (if customer)
        if (ContactInfo.Type == ContactType.Customer || ContactInfo.Type == ContactType.Both)
        {
            MyInvoices = await _context.CustomerInvoices
                .Include(i => i.Lines)
                .Where(i => i.CustomerId == contactId)
                .OrderByDescending(i => i.InvoiceDate)
                .Take(10)
                .ToListAsync();

            TotalInvoicesAmount = MyInvoices.Sum(i => i.TotalAmount);
            UnpaidInvoices = MyInvoices.Sum(i => i.TotalAmount - i.PaidAmount);
        }

        // Load bills (if vendor)
        if (ContactInfo.Type == ContactType.Vendor || ContactInfo.Type == ContactType.Both)
        {
            MyBills = await _context.VendorBills
                .Include(b => b.Lines)
                .Where(b => b.VendorId == contactId)
                .OrderByDescending(b => b.BillDate)
                .Take(10)
                .ToListAsync();

            TotalBillsAmount = MyBills.Sum(b => b.TotalAmount);
            UnpaidBills = MyBills.Sum(b => b.TotalAmount - b.PaidAmount);
        }

        return Page();
    }
}
