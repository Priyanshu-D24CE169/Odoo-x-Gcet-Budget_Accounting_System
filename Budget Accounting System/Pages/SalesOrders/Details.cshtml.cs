using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.SalesOrders;

[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(ApplicationDbContext context, ILogger<DetailsModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public SalesOrder SalesOrder { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var so = await _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.Lines)
                .ThenInclude(l => l.Product)
            .Include(s => s.Lines)
                .ThenInclude(l => l.AnalyticalAccount)
            .Include(s => s.CustomerInvoices)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (so == null)
        {
            return NotFound();
        }

        SalesOrder = so;
        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync(int id)
    {
        var so = await _context.SalesOrders
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id);
            
        if (so == null)
        {
            return NotFound();
        }

        so.Status = SOStatus.Confirmed;
        so.ModifiedDate = DateTime.UtcNow;
        
        // Automatically create Customer Invoice from this SO
        var invoiceCreated = await CreateCustomerInvoiceFromSOAsync(so);
        
        await _context.SaveChangesAsync();

        if (invoiceCreated)
        {
            TempData["SuccessMessage"] = $"Sales Order {so.SONumber} has been confirmed and Customer Invoice has been automatically created!";
        }
        else
        {
            TempData["SuccessMessage"] = $"Sales Order {so.SONumber} has been confirmed.";
        }
        
        return RedirectToPage("./Details", new { id });
    }

    private async Task<bool> CreateCustomerInvoiceFromSOAsync(SalesOrder so)
    {
        try
        {
            // Generate invoice number
            var lastInvoice = await _context.CustomerInvoices
                .OrderByDescending(i => i.Id)
                .FirstOrDefaultAsync();

            var nextNumber = lastInvoice != null 
                ? int.Parse(lastInvoice.InvoiceNumber.Split('/')[^1]) + 1 
                : 1;

            var invoiceNumber = $"INV/{DateTime.Now.Year}/{nextNumber:D4}";

            // Create customer invoice
            var customerInvoice = new CustomerInvoice
            {
                CustomerId = so.CustomerId,
                SalesOrderId = so.Id,
                InvoiceNumber = invoiceNumber,
                InvoiceDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(30),
                Reference = $"Auto-created from {so.SONumber}",
                Notes = $"Automatically generated from Sales Order {so.SONumber}",
                Status = InvoiceStatus.Draft,
                TotalAmount = 0,
                PaidAmount = 0,
                PaymentStatus = PaymentStatus.NotPaid,
                CreatedDate = DateTime.UtcNow
            };

            // Copy all lines from SO
            foreach (var soLine in so.Lines)
            {
                var invoiceLine = new CustomerInvoiceLine
                {
                    ProductId = soLine.ProductId,
                    Quantity = soLine.Quantity,
                    UnitPrice = soLine.UnitPrice,
                    LineTotal = soLine.LineTotal,
                    AnalyticalAccountId = soLine.AnalyticalAccountId
                };

                customerInvoice.Lines.Add(invoiceLine);
            }

            // Calculate total
            customerInvoice.TotalAmount = customerInvoice.Lines.Sum(l => l.LineTotal);

            _context.CustomerInvoices.Add(customerInvoice);
            
            _logger.LogInformation("Auto-created Customer Invoice {InvoiceNumber} from SO {SONumber}", 
                invoiceNumber, so.SONumber);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-creating customer invoice from SO {SONumber}", so.SONumber);
            return false;
        }
    }
}
