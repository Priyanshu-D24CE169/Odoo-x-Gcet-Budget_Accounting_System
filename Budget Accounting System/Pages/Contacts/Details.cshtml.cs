using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.Contacts;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Contact Contact { get; set; } = default!;
    public int PurchaseOrderCount { get; set; }
    public int SalesOrderCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var contact = await _context.Contacts.FirstOrDefaultAsync(m => m.Id == id);
        if (contact == null)
        {
            return NotFound();
        }

        Contact = contact;

        // Get transaction counts
        PurchaseOrderCount = await _context.PurchaseOrders.CountAsync(po => po.VendorId == id);
        SalesOrderCount = await _context.SalesOrders.CountAsync(so => so.CustomerId == id);

        return Page();
    }
}
