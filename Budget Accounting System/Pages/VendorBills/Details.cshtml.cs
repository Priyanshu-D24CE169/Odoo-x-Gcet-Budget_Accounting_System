using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using Budget_Accounting_System.Services;

namespace Budget_Accounting_System.Pages.VendorBills;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IBudgetService _budgetService;

    public DetailsModel(ApplicationDbContext context, IBudgetService budgetService)
    {
        _context = context;
        _budgetService = budgetService;
    }

    public VendorBill VendorBill { get; set; } = default!;
    public Dictionary<int, string> BudgetWarnings { get; set; } = new();
    public Dictionary<string, BudgetInfo> BudgetInfo { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
    public decimal PaidCash { get; set; }
    public decimal PaidBank { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var bill = await _context.VendorBills
            .Include(v => v.Vendor)
            .Include(v => v.Lines)
                .ThenInclude(l => l.Product)
            .Include(v => v.Lines)
                .ThenInclude(l => l.AnalyticalAccount)
            .Include(v => v.PurchaseOrder)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (bill == null)
        {
            return NotFound();
        }

        VendorBill = bill;

        // Get payments
        Payments = await _context.Payments
            .Where(p => p.VendorBillId == id)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync();

        PaidCash = Payments.Where(p => p.Method == PaymentMethod.Cash).Sum(p => p.Amount);
        PaidBank = Payments.Where(p => p.Method == PaymentMethod.BankTransfer).Sum(p => p.Amount);

        // Check budget warnings
        foreach (var line in VendorBill.Lines)
        {
            if (line.AnalyticalAccountId.HasValue)
            {
                var warning = await CheckBudgetWarning(line);
                if (!string.IsNullOrEmpty(warning))
                {
                    BudgetWarnings[line.Id] = warning;
                }

                await LoadBudgetInfo(line);
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync(int id)
    {
        var bill = await _context.VendorBills.FindAsync(id);
        if (bill == null)
        {
            return NotFound();
        }

        bill.Status = BillStatus.Posted;
        bill.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Vendor Bill {bill.BillNumber} has been confirmed.";
        return RedirectToPage("./Details", new { id });
    }

    private async Task<string?> CheckBudgetWarning(VendorBillLine line)
    {
        if (!line.AnalyticalAccountId.HasValue)
            return null;

        // Simplified budget check
        var budgets = await _context.BudgetLines
            .Include(bl => bl.Budget)
            .Where(bl => bl.AnalyticalAccountId == line.AnalyticalAccountId.Value &&
                        bl.Type == BudgetLineType.Expense &&
                        bl.Budget.State == BudgetState.Confirmed &&
                        bl.Budget.StartDate <= VendorBill.BillDate &&
                        bl.Budget.EndDate >= VendorBill.BillDate)
            .ToListAsync();

        foreach (var budgetLine in budgets)
        {
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

    private async Task LoadBudgetInfo(VendorBillLine line)
    {
        if (!line.AnalyticalAccountId.HasValue || line.AnalyticalAccount == null)
            return;

        var budgets = await _context.BudgetLines
            .Include(bl => bl.Budget)
            .Where(bl => bl.AnalyticalAccountId == line.AnalyticalAccountId.Value &&
                        bl.Type == BudgetLineType.Expense &&
                        bl.Budget.State == BudgetState.Confirmed &&
                        bl.Budget.StartDate <= VendorBill.BillDate &&
                        bl.Budget.EndDate >= VendorBill.BillDate)
            .ToListAsync();

        foreach (var budgetLine in budgets)
        {
            var actualAmount = await _context.VendorBillLines
                .Include(l => l.VendorBill)
                .Where(l => l.AnalyticalAccountId == budgetLine.AnalyticalAccountId &&
                           l.VendorBill.Status == BillStatus.Posted &&
                           l.VendorBill.BillDate >= budgetLine.Budget.StartDate &&
                           l.VendorBill.BillDate <= budgetLine.Budget.EndDate)
                .SumAsync(l => l.LineTotal);

            var key = $"{line.AnalyticalAccount.Code} - {line.AnalyticalAccount.Name}";

            if (!BudgetInfo.ContainsKey(key))
            {
                BudgetInfo[key] = new BudgetInfo
                {
                    BudgetName = budgetLine.Budget.Name,
                    PlannedAmount = budgetLine.BudgetedAmount,
                    SpentAmount = actualAmount,
                    Remaining = budgetLine.BudgetedAmount - actualAmount
                };
            }
        }
    }
}

public class BudgetInfo
{
    public string BudgetName { get; set; } = string.Empty;
    public decimal PlannedAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal ThisBillAmount { get; set; }
    public decimal Remaining { get; set; }
}
