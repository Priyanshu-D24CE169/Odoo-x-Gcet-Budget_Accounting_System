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

    public DetailsModel(ApplicationDbContext context, IBudgetService budgetService)
    {
        _context = context;
        _budgetService = budgetService;
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
        var po = await _context.PurchaseOrders.FindAsync(id);
        if (po == null)
        {
            return NotFound();
        }

        po.Status = POStatus.Confirmed;
        po.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Purchase Order {po.PONumber} has been confirmed.";
        return RedirectToPage("./Details", new { id });
    }

    private async Task<string?> CheckBudgetWarning(PurchaseOrderLine line)
    {
        if (!line.AnalyticalAccountId.HasValue)
            return null;

        // Find budgets for this analytical account that overlap with current period
        var budgets = await _context.Budgets
            .Where(b => b.AnalyticalAccountId == line.AnalyticalAccountId.Value &&
                       b.IsActive &&
                       b.Type == BudgetType.Expense &&
                       b.StartDate <= PurchaseOrder.PODate &&
                       b.EndDate >= PurchaseOrder.PODate)
            .ToListAsync();

        foreach (var budget in budgets)
        {
            var analysis = await _budgetService.GetBudgetAnalysis(budget.Id);
            var remainingBudget = budget.PlannedAmount - analysis.ActualAmount;

            if (line.LineTotal > remainingBudget)
            {
                return $"Exceeds budget by {(line.LineTotal - remainingBudget):C}. Budget: {budget.Name}";
            }
        }

        return null;
    }
}
