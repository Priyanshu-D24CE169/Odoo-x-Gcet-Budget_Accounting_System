using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public class VendorBillService : IVendorBillService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<VendorBillService> _logger;

    public VendorBillService(ApplicationDbContext dbContext, ILogger<VendorBillService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<VendorBill>> GetBillsAsync(string? search, VendorBillStatus? status, VendorBillPaymentStatus? paymentStatus, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.VendorBills
            .Include(bill => bill.Vendor)
            .Include(bill => bill.PurchaseOrder)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(bill => bill.BillNumber.Contains(term) || (bill.Vendor != null && bill.Vendor.Name.Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(bill => bill.Status == status.Value);
        }

        if (paymentStatus.HasValue)
        {
            query = query.Where(bill => bill.PaymentStatus == paymentStatus.Value);
        }

        return await query
            .OrderByDescending(bill => bill.VendorBillId)
            .ToListAsync(cancellationToken);
    }

    public Task<VendorBill?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.VendorBills
            .Include(bill => bill.Vendor)
            .Include(bill => bill.Lines)
                .ThenInclude(line => line.Product)
            .Include(bill => bill.Lines)
                .ThenInclude(line => line.AnalyticalAccount)
            .AsNoTracking()
            .FirstOrDefaultAsync(bill => bill.VendorBillId == id, cancellationToken);
    }

    public async Task<VendorBill> CreateDraftFromPurchaseOrderAsync(int purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var purchaseOrder = await _dbContext.PurchaseOrders
            .Include(po => po.Lines)
            .FirstOrDefaultAsync(po => po.PurchaseOrderId == purchaseOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Purchase Order {purchaseOrderId} not found.");

        if (purchaseOrder.Status != PurchaseOrderStatus.Confirmed)
        {
            throw new InvalidOperationException("Vendor bills can only be created from confirmed purchase orders.");
        }

        var existingBill = await _dbContext.VendorBills
            .FirstOrDefaultAsync(bill => bill.PurchaseOrderId == purchaseOrderId && bill.Status != VendorBillStatus.Cancelled, cancellationToken);

        if (existingBill is not null)
        {
            return existingBill;
        }

        var bill = new VendorBill
        {
            BillNumber = await GenerateNextBillNumberAsync(cancellationToken),
            VendorId = purchaseOrder.VendorId,
            PurchaseOrderId = purchaseOrder.PurchaseOrderId,
            BillDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            Status = VendorBillStatus.Draft,
            PaymentStatus = VendorBillPaymentStatus.NotPaid
        };

        foreach (var poLine in purchaseOrder.Lines)
        {
            if (!poLine.ProductId.HasValue || poLine.Quantity <= 0)
            {
                continue;
            }

            bill.Lines.Add(new VendorBillLine
            {
                ProductId = poLine.ProductId,
                Quantity = poLine.Quantity,
                UnitPrice = poLine.UnitPrice,
                Total = poLine.Total,
                AnalyticalAccountId = poLine.AnalyticalAccountId
            });
        }

        UpdateTotals(bill);

        _dbContext.VendorBills.Add(bill);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return bill;
    }

    public async Task UpdateAsync(VendorBill updatedBill, CancellationToken cancellationToken = default)
    {
        var existingBill = await _dbContext.VendorBills
            .Include(bill => bill.Lines)
            .FirstOrDefaultAsync(bill => bill.VendorBillId == updatedBill.VendorBillId, cancellationToken);

        if (existingBill is null)
        {
            throw new InvalidOperationException($"Vendor Bill {updatedBill.VendorBillId} not found.");
        }

        if (existingBill.Status != VendorBillStatus.Draft)
        {
            throw new InvalidOperationException("Only draft vendor bills can be edited.");
        }

        existingBill.BillDate = updatedBill.BillDate;
        existingBill.DueDate = updatedBill.DueDate;
        existingBill.VendorId = updatedBill.VendorId;

        _dbContext.VendorBillLines.RemoveRange(existingBill.Lines);
        existingBill.Lines.Clear();

        foreach (var line in updatedBill.Lines)
        {
            line.VendorBillId = existingBill.VendorBillId;
            existingBill.Lines.Add(line);
        }

        UpdateTotals(existingBill);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ConfirmAsync(int billId, CancellationToken cancellationToken = default)
    {
        var bill = await _dbContext.VendorBills.FirstOrDefaultAsync(b => b.VendorBillId == billId, cancellationToken)
            ?? throw new InvalidOperationException($"Vendor Bill {billId} not found.");

        if (bill.Status == VendorBillStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled vendor bills cannot be confirmed.");
        }

        if (bill.Status == VendorBillStatus.Confirmed)
        {
            return;
        }

        bill.Status = VendorBillStatus.Confirmed;
        bill.ConfirmedOn = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(int billId, CancellationToken cancellationToken = default)
    {
        var bill = await _dbContext.VendorBills.FirstOrDefaultAsync(b => b.VendorBillId == billId, cancellationToken)
            ?? throw new InvalidOperationException($"Vendor Bill {billId} not found.");

        if (bill.Status == VendorBillStatus.Cancelled)
        {
            return;
        }

        bill.Status = VendorBillStatus.Cancelled;
        bill.CancelledOn = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTotals(VendorBill bill)
    {
        foreach (var line in bill.Lines)
        {
            line.Total = Math.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero);
        }

        bill.TotalAmount = bill.Lines.Sum(line => line.Total);
        UpdatePaymentStatus(bill);
    }

    private void UpdatePaymentStatus(VendorBill bill)
    {
        if (bill.AmountPaid < 0)
        {
            bill.AmountPaid = 0;
        }

        if (bill.AmountPaid > bill.TotalAmount && bill.TotalAmount > 0)
        {
            bill.AmountPaid = bill.TotalAmount;
        }

        if (bill.AmountPaid <= 0)
        {
            bill.PaymentStatus = VendorBillPaymentStatus.NotPaid;
        }
        else if (bill.AmountPaid >= bill.TotalAmount)
        {
            bill.PaymentStatus = VendorBillPaymentStatus.Paid;
        }
        else
        {
            bill.PaymentStatus = VendorBillPaymentStatus.Partial;
        }
    }

    private async Task<string> GenerateNextBillNumberAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var prefix = $"BILL-{now:yyyyMM}-";
        var lastNumber = await _dbContext.VendorBills
            .Where(bill => bill.BillNumber.StartsWith(prefix))
            .OrderByDescending(bill => bill.BillNumber)
            .Select(bill => bill.BillNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var sequence = 1;
        if (!string.IsNullOrWhiteSpace(lastNumber) && lastNumber.Length >= prefix.Length)
        {
            var numericPart = lastNumber[prefix.Length..];
            if (int.TryParse(numericPart, out var parsed))
            {
                sequence = parsed + 1;
            }
        }

        return prefix + sequence.ToString("D4");
    }
}
