using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Dashboard Statistics
    public int TotalContacts { get; set; }
    public int TotalProducts { get; set; }
    public int TotalAnalyticalAccounts { get; set; }
    public int ActiveBudgets { get; set; }
    
    public int TotalPurchaseOrders { get; set; }
    public int PendingPOs { get; set; }
    public int TotalVendorBills { get; set; }
    public decimal UnpaidVendorBills { get; set; }
    
    public int TotalSalesOrders { get; set; }
    public int PendingSOs { get; set; }
    public int TotalCustomerInvoices { get; set; }
    public decimal UnpaidCustomerInvoices { get; set; }
    
    public int TotalPayments { get; set; }
    public decimal TotalPaymentsAmount { get; set; }

    public async Task OnGetAsync()
    {
        // Master Data Statistics
        TotalContacts = await _context.Contacts.CountAsync(c => c.State == ContactState.Confirmed);
        TotalProducts = await _context.Products.CountAsync(p => p.State == ProductState.Confirmed);
        TotalAnalyticalAccounts = await _context.AnalyticalAccounts.CountAsync(a => a.IsActive);
        ActiveBudgets = await _context.Budgets.CountAsync(b => b.State == BudgetState.Confirmed);

        // Purchase Statistics
        TotalPurchaseOrders = await _context.PurchaseOrders.CountAsync();
        PendingPOs = await _context.PurchaseOrders.CountAsync(p => p.Status == POStatus.Draft);
        TotalVendorBills = await _context.VendorBills.CountAsync();
        
        var vendorBills = await _context.VendorBills.ToListAsync();
        UnpaidVendorBills = vendorBills.Sum(b => b.TotalAmount - b.PaidAmount);

        // Sales Statistics
        TotalSalesOrders = await _context.SalesOrders.CountAsync();
        PendingSOs = await _context.SalesOrders.CountAsync(s => s.Status == SOStatus.Draft);
        TotalCustomerInvoices = await _context.CustomerInvoices.CountAsync();
        
        var customerInvoices = await _context.CustomerInvoices.ToListAsync();
        UnpaidCustomerInvoices = customerInvoices.Sum(i => i.TotalAmount - i.PaidAmount);

        // Payment Statistics
        TotalPayments = await _context.Payments.CountAsync();
        TotalPaymentsAmount = await _context.Payments.SumAsync(p => p.Amount);
    }
}

