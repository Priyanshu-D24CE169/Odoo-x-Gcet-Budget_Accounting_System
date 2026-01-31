using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Services;

public interface IBudgetActualService
{
    Task<decimal> ComputeAchievedAmountAsync(BudgetLine budgetLine, Budget budget);
    Task ComputeAllActualsAsync(Budget budget);
    Task UpdateBudgetOnVendorBillPaymentAsync(int vendorBillId);
    Task UpdateBudgetOnCustomerInvoicePaymentAsync(int customerInvoiceId);
    Task<List<Budget>> GetAffectedBudgetsForVendorBillAsync(int vendorBillId);
    Task<List<Budget>> GetAffectedBudgetsForCustomerInvoiceAsync(int customerInvoiceId);
}

public class BudgetActualService : IBudgetActualService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BudgetActualService> _logger;

    public BudgetActualService(ApplicationDbContext context, ILogger<BudgetActualService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<decimal> ComputeAchievedAmountAsync(BudgetLine budgetLine, Budget budget)
    {
        try
        {
            if (budget.State != BudgetState.Confirmed)
            {
                return 0; // Only compute for confirmed budgets
            }

            decimal achievedAmount = 0;

            if (budgetLine.Type == BudgetLineType.Income)
            {
                // Search Customer Invoices (PAID amounts only)
                achievedAmount = await _context.CustomerInvoiceLines
                    .Include(l => l.CustomerInvoice)
                    .Where(line => line.AnalyticalAccountId == budgetLine.AnalyticalAccountId &&
                                  line.CustomerInvoice.Status == InvoiceStatus.Posted &&
                                  line.CustomerInvoice.InvoiceDate >= budget.StartDate &&
                                  line.CustomerInvoice.InvoiceDate <= budget.EndDate)
                    .Select(line => new 
                    { 
                        LineTotal = line.LineTotal,
                        PaidRatio = line.CustomerInvoice.TotalAmount > 0 
                            ? line.CustomerInvoice.PaidAmount / line.CustomerInvoice.TotalAmount 
                            : 0
                    })
                    .SumAsync(x => x.LineTotal * x.PaidRatio);
            }
            else // Expense
            {
                // Search Vendor Bills (PAID amounts only)
                achievedAmount = await _context.VendorBillLines
                    .Include(l => l.VendorBill)
                    .Where(line => line.AnalyticalAccountId == budgetLine.AnalyticalAccountId &&
                                  line.VendorBill.Status == BillStatus.Posted &&
                                  line.VendorBill.BillDate >= budget.StartDate &&
                                  line.VendorBill.BillDate <= budget.EndDate)
                    .Select(line => new 
                    { 
                        LineTotal = line.LineTotal,
                        PaidRatio = line.VendorBill.TotalAmount > 0 
                            ? line.VendorBill.PaidAmount / line.VendorBill.TotalAmount 
                            : 0
                    })
                    .SumAsync(x => x.LineTotal * x.PaidRatio);
            }

            return achievedAmount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing achieved amount for budget line {LineId}", budgetLine.Id);
            return 0;
        }
    }

    public async Task ComputeAllActualsAsync(Budget budget)
    {
        if (budget.State != BudgetState.Confirmed)
        {
            return; // Only compute for confirmed budgets
        }

        var lines = await _context.BudgetLines
            .Where(l => l.BudgetId == budget.Id)
            .ToListAsync();

        foreach (var line in lines)
        {
            line.AchievedAmount = await ComputeAchievedAmountAsync(line, budget);
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Updated actual amounts for budget {BudgetName} with {LineCount} lines", 
            budget.Name, lines.Count);
    }

    public async Task UpdateBudgetOnVendorBillPaymentAsync(int vendorBillId)
    {
        try
        {
            var vendorBill = await _context.VendorBills
                .Include(b => b.Lines)
                .FirstOrDefaultAsync(b => b.Id == vendorBillId);

            if (vendorBill == null)
            {
                _logger.LogWarning("Vendor bill {BillId} not found", vendorBillId);
                return;
            }

            // Get all budgets affected by this vendor bill
            var affectedBudgets = await GetAffectedBudgetsForVendorBillAsync(vendorBillId);

            foreach (var budget in affectedBudgets)
            {
                await ComputeAllActualsAsync(budget);
            }

            _logger.LogInformation(
                "Updated {BudgetCount} budgets after vendor bill {BillNumber} payment. Paid: ?{PaidAmount}, Total: ?{TotalAmount}",
                affectedBudgets.Count, vendorBill.BillNumber, vendorBill.PaidAmount, vendorBill.TotalAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating budgets for vendor bill {BillId}", vendorBillId);
        }
    }

    public async Task UpdateBudgetOnCustomerInvoicePaymentAsync(int customerInvoiceId)
    {
        try
        {
            var invoice = await _context.CustomerInvoices
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == customerInvoiceId);

            if (invoice == null)
            {
                _logger.LogWarning("Customer invoice {InvoiceId} not found", customerInvoiceId);
                return;
            }

            // Get all budgets affected by this invoice
            var affectedBudgets = await GetAffectedBudgetsForCustomerInvoiceAsync(customerInvoiceId);

            foreach (var budget in affectedBudgets)
            {
                await ComputeAllActualsAsync(budget);
            }

            _logger.LogInformation(
                "Updated {BudgetCount} budgets after customer invoice {InvoiceNumber} payment. Received: ?{PaidAmount}, Total: ?{TotalAmount}",
                affectedBudgets.Count, invoice.InvoiceNumber, invoice.PaidAmount, invoice.TotalAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating budgets for customer invoice {InvoiceId}", customerInvoiceId);
        }
    }

    public async Task<List<Budget>> GetAffectedBudgetsForVendorBillAsync(int vendorBillId)
    {
        var billDate = await _context.VendorBills
            .Where(b => b.Id == vendorBillId)
            .Select(b => b.BillDate)
            .FirstOrDefaultAsync();

        var analyticalAccountIds = await _context.VendorBillLines
            .Where(l => l.VendorBillId == vendorBillId && l.AnalyticalAccountId.HasValue)
            .Select(l => l.AnalyticalAccountId!.Value)
            .Distinct()
            .ToListAsync();

        if (!analyticalAccountIds.Any())
        {
            return new List<Budget>();
        }

        var affectedBudgets = await _context.Budgets
            .Where(b => b.State == BudgetState.Confirmed &&
                       b.StartDate <= billDate &&
                       b.EndDate >= billDate &&
                       b.Lines.Any(l => analyticalAccountIds.Contains(l.AnalyticalAccountId)))
            .Distinct()
            .ToListAsync();

        return affectedBudgets;
    }

    public async Task<List<Budget>> GetAffectedBudgetsForCustomerInvoiceAsync(int customerInvoiceId)
    {
        var invoiceDate = await _context.CustomerInvoices
            .Where(i => i.Id == customerInvoiceId)
            .Select(i => i.InvoiceDate)
            .FirstOrDefaultAsync();

        var analyticalAccountIds = await _context.CustomerInvoiceLines
            .Where(l => l.CustomerInvoiceId == customerInvoiceId && l.AnalyticalAccountId.HasValue)
            .Select(l => l.AnalyticalAccountId!.Value)
            .Distinct()
            .ToListAsync();

        if (!analyticalAccountIds.Any())
        {
            return new List<Budget>();
        }

        var affectedBudgets = await _context.Budgets
            .Where(b => b.State == BudgetState.Confirmed &&
                       b.StartDate <= invoiceDate &&
                       b.EndDate >= invoiceDate &&
                       b.Lines.Any(l => analyticalAccountIds.Contains(l.AnalyticalAccountId)))
            .Distinct()
            .ToListAsync();

        return affectedBudgets;
    }
}

