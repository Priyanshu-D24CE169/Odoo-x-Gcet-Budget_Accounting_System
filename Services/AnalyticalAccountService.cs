using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Budget_Accounting_System.Services;

public interface IAnalyticalAccountService
{
    Task<int?> GetAnalyticalAccountForTransaction(int productId, int? contactId, string? productCategory);
    Task ApplyAutoAnalyticalRules(int transactionLineId, string transactionType);
}

public class AnalyticalAccountService : IAnalyticalAccountService
{
    private readonly ApplicationDbContext _context;

    public AnalyticalAccountService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int?> GetAnalyticalAccountForTransaction(int productId, int? contactId, string? productCategory)
    {
        var models = await _context.AutoAnalyticalModels
            .Include(m => m.Rules)
            .ThenInclude(r => r.AnalyticalAccount)
            .Where(m => m.IsActive)
            .OrderBy(m => m.Priority)
            .ToListAsync();

        foreach (var model in models)
        {
            foreach (var rule in model.Rules.Where(r => r.IsActive))
            {
                switch (rule.Condition)
                {
                    case RuleCondition.SpecificProduct:
                        if (rule.ProductId == productId)
                            return rule.AnalyticalAccountId;
                        break;

                    case RuleCondition.ProductCategory:
                        if (!string.IsNullOrEmpty(productCategory) && 
                            rule.ProductCategory?.Equals(productCategory, StringComparison.OrdinalIgnoreCase) == true)
                            return rule.AnalyticalAccountId;
                        break;

                    case RuleCondition.Customer:
                    case RuleCondition.Vendor:
                        if (contactId.HasValue && rule.ContactId == contactId)
                            return rule.AnalyticalAccountId;
                        break;
                }
            }
        }

        return null;
    }

    public async Task ApplyAutoAnalyticalRules(int transactionLineId, string transactionType)
    {
        switch (transactionType.ToLower())
        {
            case "purchaseorder":
                var poLine = await _context.PurchaseOrderLines
                    .Include(l => l.Product)
                    .Include(l => l.PurchaseOrder)
                    .FirstOrDefaultAsync(l => l.Id == transactionLineId);

                if (poLine != null && !poLine.AnalyticalAccountId.HasValue)
                {
                    var accountId = await GetAnalyticalAccountForTransaction(
                        poLine.ProductId, 
                        poLine.PurchaseOrder.VendorId, 
                        poLine.Product.Category);

                    if (accountId.HasValue)
                    {
                        poLine.AnalyticalAccountId = accountId;
                        await _context.SaveChangesAsync();
                    }
                }
                break;

            case "vendorbill":
                var billLine = await _context.VendorBillLines
                    .Include(l => l.Product)
                    .Include(l => l.VendorBill)
                    .FirstOrDefaultAsync(l => l.Id == transactionLineId);

                if (billLine != null && !billLine.AnalyticalAccountId.HasValue)
                {
                    var accountId = await GetAnalyticalAccountForTransaction(
                        billLine.ProductId, 
                        billLine.VendorBill.VendorId, 
                        billLine.Product.Category);

                    if (accountId.HasValue)
                    {
                        billLine.AnalyticalAccountId = accountId;
                        await _context.SaveChangesAsync();
                    }
                }
                break;

            case "salesorder":
                var soLine = await _context.SalesOrderLines
                    .Include(l => l.Product)
                    .Include(l => l.SalesOrder)
                    .FirstOrDefaultAsync(l => l.Id == transactionLineId);

                if (soLine != null && !soLine.AnalyticalAccountId.HasValue)
                {
                    var accountId = await GetAnalyticalAccountForTransaction(
                        soLine.ProductId, 
                        soLine.SalesOrder.CustomerId, 
                        soLine.Product.Category);

                    if (accountId.HasValue)
                    {
                        soLine.AnalyticalAccountId = accountId;
                        await _context.SaveChangesAsync();
                    }
                }
                break;

            case "customerinvoice":
                var invLine = await _context.CustomerInvoiceLines
                    .Include(l => l.Product)
                    .Include(l => l.CustomerInvoice)
                    .FirstOrDefaultAsync(l => l.Id == transactionLineId);

                if (invLine != null && !invLine.AnalyticalAccountId.HasValue)
                {
                    var accountId = await GetAnalyticalAccountForTransaction(
                        invLine.ProductId, 
                        invLine.CustomerInvoice.CustomerId, 
                        invLine.Product.Category);

                    if (accountId.HasValue)
                    {
                        invLine.AnalyticalAccountId = accountId;
                        await _context.SaveChangesAsync();
                    }
                }
                break;
        }
    }
}
