using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using Budget_Accounting_System.Services;

namespace Budget_Accounting_System.Pages.PurchaseOrders;

[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IBudgetService _budgetService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(ApplicationDbContext context, IBudgetService budgetService, ILogger<DetailsModel> logger)
    {
        _context = context;
        _budgetService = budgetService;
        _logger = logger;
    }

    public PurchaseOrder PurchaseOrder { get; set; } = default!;
    public Dictionary<int, string> BudgetWarnings { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var po = await _context.PurchaseOrders
            .Include(p => p.Vendor)
            .Include(p => p.Lines)
                .ThenInclude(l => l.Product)
            .Include(p => p.Lines)
                .ThenInclude(l => l.AnalyticalAccount)
            .Include(p => p.VendorBills)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (po == null)
        {
            return NotFound();
        }

        PurchaseOrder = po;

        // Check budget warnings for each line
        foreach (var line in PurchaseOrder.Lines)
        {
            if (line.AnalyticalAccountId.HasValue)
            {
                var warning = await CheckBudgetWarning(line);
                if (!string.IsNullOrEmpty(warning))
                {
                    BudgetWarnings[line.Id] = warning;
                }
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync(int id)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id);
            
        if (po == null)
        {
            return NotFound();
        }

        po.Status = POStatus.Confirmed;
        po.ModifiedDate = DateTime.UtcNow;
        
        // Automatically create Vendor Bill from this PO
        var billCreated = await CreateVendorBillFromPOAsync(po);
        
        await _context.SaveChangesAsync();

        if (billCreated)
        {
            TempData["SuccessMessage"] = $"Purchase Order {po.PONumber} has been confirmed and Vendor Bill has been automatically created!";
        }
        else
        {
            TempData["SuccessMessage"] = $"Purchase Order {po.PONumber} has been confirmed.";
        }
        
        return RedirectToPage("./Details", new { id });
    }

    private async Task<bool> CreateVendorBillFromPOAsync(PurchaseOrder po)
    {
        try
        {
            // Generate bill number
            var lastBill = await _context.VendorBills
                .OrderByDescending(b => b.Id)
                .FirstOrDefaultAsync();

            var nextNumber = lastBill != null 
                ? int.Parse(lastBill.BillNumber.Split('/')[^1]) + 1 
                : 1;

            var billNumber = $"BILL/{DateTime.Now.Year}/{nextNumber:D4}";

            // Create vendor bill
            var vendorBill = new VendorBill
            {
                VendorId = po.VendorId,
                PurchaseOrderId = po.Id,
                BillNumber = billNumber,
                BillDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(30),
                Reference = $"Auto-created from {po.PONumber}",
                Notes = $"Automatically generated from Purchase Order {po.PONumber}",
                Status = BillStatus.Draft,
                TotalAmount = 0,
                PaidAmount = 0,
                PaymentStatus = PaymentStatus.NotPaid,
                CreatedDate = DateTime.UtcNow
            };

            // Copy all lines from PO
            foreach (var poLine in po.Lines)
            {
                var billLine = new VendorBillLine
                {
                    ProductId = poLine.ProductId,
                    Quantity = poLine.Quantity,
                    UnitPrice = poLine.UnitPrice,
                    LineTotal = poLine.LineTotal,
                    AnalyticalAccountId = poLine.AnalyticalAccountId
                };

                vendorBill.Lines.Add(billLine);
            }

            // Calculate total
            vendorBill.TotalAmount = vendorBill.Lines.Sum(l => l.LineTotal);

            _context.VendorBills.Add(vendorBill);
            
            _logger.LogInformation("Auto-created Vendor Bill {BillNumber} from PO {PONumber}", 
                billNumber, po.PONumber);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-creating vendor bill from PO {PONumber}", po.PONumber);
            return false;
        }
    }

    private async Task<string?> CheckBudgetWarning(PurchaseOrderLine line)
    {
        if (!line.AnalyticalAccountId.HasValue)
            return null;

        // Simplified budget check - find any confirmed budgets for this analytical account
        var budgets = await _context.BudgetLines
            .Include(bl => bl.Budget)
            .Where(bl => bl.AnalyticalAccountId == line.AnalyticalAccountId.Value &&
                        bl.Type == BudgetLineType.Expense &&
                        bl.Budget.State == BudgetState.Confirmed &&
                        bl.Budget.StartDate <= PurchaseOrder.PODate &&
                        bl.Budget.EndDate >= PurchaseOrder.PODate)
            .ToListAsync();

        foreach (var budgetLine in budgets)
        {
            // Compute actual for this budget line
            var actualAmount = await _context.VendorBillLines
                .Include(l => l.VendorBill)
                .Where(l => l.AnalyticalAccountId == budgetLine.AnalyticalAccountId &&
                           l.VendorBill.Status == BillStatus.Posted &&
                           l.VendorBill.BillDate >= budgetLine.Budget.StartDate &&
                           l.VendorBill.BillDate <= budgetLine.Budget.EndDate)
                .SumAsync(l => l.LineTotal);

            var remainingBudget = budgetLine.BudgetedAmount - actualAmount;

            if (line.LineTotal > remainingBudget)
            {
                return $"Exceeds budget by ?{(line.LineTotal - remainingBudget):N2}. Budget: {budgetLine.Budget.Name}";
            }
        }

        return null;
    }
}
