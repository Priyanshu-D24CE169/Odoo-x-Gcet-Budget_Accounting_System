using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using System.Security.Claims;

namespace Budget_Accounting_System.Areas.Portal.Pages;

[Authorize(Roles = "PortalUser")]
[Area("Portal")]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Contact? ContactInfo { get; set; }
    public ApplicationUser CurrentUser { get; set; } = default!;
    public List<CustomerInvoice> MyInvoices { get; set; } = new();
    public List<VendorBill> MyBills { get; set; } = new();
    public decimal TotalInvoicesAmount { get; set; }
    public decimal TotalBillsAmount { get; set; }
    public decimal UnpaidInvoices { get; set; }
    public decimal UnpaidBills { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        CurrentUser = await _userManager.FindByIdAsync(userId!);

        if (CurrentUser?.ContactId == null)
        {
            TempData["ErrorMessage"] = "Your account is not linked to a contact. Please contact support.";
            return Page();
        }

        ContactInfo = await _context.Contacts.FindAsync(CurrentUser.ContactId);
        if (ContactInfo == null)
        {
            return NotFound();
        }

        // Load invoices (if customer)
        if (ContactInfo.Type == ContactType.Customer || ContactInfo.Type == ContactType.Both)
        {
            MyInvoices = await _context.CustomerInvoices
                .Include(i => i.Lines)
                .Where(i => i.CustomerId == CurrentUser.ContactId)
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
                .Where(b => b.VendorId == CurrentUser.ContactId)
                .OrderByDescending(b => b.BillDate)
                .Take(10)
                .ToListAsync();

            TotalBillsAmount = MyBills.Sum(b => b.TotalAmount);
            UnpaidBills = MyBills.Sum(b => b.TotalAmount - b.PaidAmount);
        }

        return Page();
    }
}
