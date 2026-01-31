using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Areas.Admin.Pages;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DashboardModel> _logger;

    public DashboardModel(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        ILogger<DashboardModel> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    // Dashboard Statistics
    public int TotalContacts { get; set; }
    public int TotalProducts { get; set; }
    public int TotalAnalyticalAccounts { get; set; }
    public int ActiveBudgets { get; set; }
    
    public int TotalPurchaseOrders { get; set; }
    public int PendingPOs { get; set; }
    public int ConfirmedPOs { get; set; }
    public int TotalVendorBills { get; set; }
    public decimal TotalVendorBillsAmount { get; set; }
    public decimal UnpaidVendorBills { get; set; }
    
    public int TotalSalesOrders { get; set; }
    public int PendingSOs { get; set; }
    public int ConfirmedSOs { get; set; }
    public int TotalCustomerInvoices { get; set; }
    public decimal TotalCustomerInvoicesAmount { get; set; }
    public decimal UnpaidCustomerInvoices { get; set; }
    
    public int TotalPayments { get; set; }
    public decimal TotalPaymentsAmount { get; set; }

    public ApplicationUser? CurrentUser { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                CurrentUser = await _userManager.FindByIdAsync(userId);
            }

            // Master Data Statistics
            TotalContacts = await _context.Contacts.CountAsync(c => c.IsActive);
            TotalProducts = await _context.Products.CountAsync(p => p.IsActive);
            TotalAnalyticalAccounts = await _context.AnalyticalAccounts.CountAsync(a => a.IsActive);
            
            // Try to get budget count - might fail if database not migrated yet
            try
            {
                ActiveBudgets = await _context.Budgets.CountAsync(b => b.State == BudgetState.Confirmed);
            }
            catch (Exception)
            {
                // Fallback - database might not have State column yet
                ActiveBudgets = await _context.Budgets.CountAsync();
                _logger.LogWarning("Using fallback budget count - database may need migration");
            }

            _logger.LogInformation("Master Data - Contacts: {Contacts}, Products: {Products}, AnalyticalAccounts: {Accounts}, Budgets: {Budgets}",
                TotalContacts, TotalProducts, TotalAnalyticalAccounts, ActiveBudgets);

            // Purchase Statistics
            TotalPurchaseOrders = await _context.PurchaseOrders.CountAsync();
            PendingPOs = await _context.PurchaseOrders.CountAsync(p => p.Status == POStatus.Draft);
            ConfirmedPOs = await _context.PurchaseOrders.CountAsync(p => p.Status == POStatus.Confirmed);
            
            _logger.LogInformation("Purchase Orders - Total: {Total}, Pending: {Pending}, Confirmed: {Confirmed}",
                TotalPurchaseOrders, PendingPOs, ConfirmedPOs);

            // Vendor Bills Statistics
            TotalVendorBills = await _context.VendorBills.CountAsync();
            
            if (TotalVendorBills > 0)
            {
                var vendorBills = await _context.VendorBills
                    .Select(b => new { b.TotalAmount, b.PaidAmount })
                    .ToListAsync();
                
                TotalVendorBillsAmount = vendorBills.Sum(b => b.TotalAmount);
                UnpaidVendorBills = vendorBills.Sum(b => b.TotalAmount - b.PaidAmount);
            }

            _logger.LogInformation("Vendor Bills - Total: {Total}, Amount: {Amount}, Unpaid: {Unpaid}",
                TotalVendorBills, TotalVendorBillsAmount, UnpaidVendorBills);

            // Sales Statistics
            TotalSalesOrders = await _context.SalesOrders.CountAsync();
            PendingSOs = await _context.SalesOrders.CountAsync(s => s.Status == SOStatus.Draft);
            ConfirmedSOs = await _context.SalesOrders.CountAsync(s => s.Status == SOStatus.Confirmed);

            _logger.LogInformation("Sales Orders - Total: {Total}, Pending: {Pending}, Confirmed: {Confirmed}",
                TotalSalesOrders, PendingSOs, ConfirmedSOs);

            // Customer Invoices Statistics
            TotalCustomerInvoices = await _context.CustomerInvoices.CountAsync();
            
            if (TotalCustomerInvoices > 0)
            {
                var customerInvoices = await _context.CustomerInvoices
                    .Select(i => new { i.TotalAmount, i.PaidAmount })
                    .ToListAsync();
                
                TotalCustomerInvoicesAmount = customerInvoices.Sum(i => i.TotalAmount);
                UnpaidCustomerInvoices = customerInvoices.Sum(i => i.TotalAmount - i.PaidAmount);
            }

            _logger.LogInformation("Customer Invoices - Total: {Total}, Amount: {Amount}, Unpaid: {Unpaid}",
                TotalCustomerInvoices, TotalCustomerInvoicesAmount, UnpaidCustomerInvoices);

            // Payment Statistics
            TotalPayments = await _context.Payments.CountAsync();
            
            if (TotalPayments > 0)
            {
                TotalPaymentsAmount = await _context.Payments.SumAsync(p => p.Amount);
            }

            _logger.LogInformation("Payments - Total: {Total}, Amount: {Amount}",
                TotalPayments, TotalPaymentsAmount);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data");
            
            // Show friendly error message to user
            TempData["ErrorMessage"] = "Error loading dashboard data. The database may need to be migrated. Please run: IMPLEMENT-NEW-BUDGET-MODULE.bat";
            
            return Page();
        }
    }
}
