using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public class BillPaymentService : IBillPaymentService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<BillPaymentService> _logger;

    public BillPaymentService(ApplicationDbContext dbContext, ILogger<BillPaymentService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BillPayment>> GetPaymentsForBillAsync(int vendorBillId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.BillPayments
            .Where(payment => payment.VendorBillId == vendorBillId)
            .OrderByDescending(payment => payment.PaymentDate)
            .ThenByDescending(payment => payment.BillPaymentId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<BillPayment> CreateAsync(BillPayment payment, CancellationToken cancellationToken = default)
    {
        var bill = await _dbContext.VendorBills
            .FirstOrDefaultAsync(b => b.VendorBillId == payment.VendorBillId, cancellationToken)
            ?? throw new InvalidOperationException($"Vendor Bill {payment.VendorBillId} not found.");

        if (bill.Status == VendorBillStatus.Cancelled)
        {
            throw new InvalidOperationException("Payments cannot be recorded for cancelled bills.");
        }

        if (bill.Status != VendorBillStatus.Confirmed)
        {
            throw new InvalidOperationException("Only confirmed bills can receive payments.");
        }

        var amountDue = Math.Max(bill.TotalAmount - bill.AmountPaid, 0);
        if (amountDue <= 0)
        {
            throw new InvalidOperationException("This bill is already fully paid.");
        }

        if (payment.Amount <= 0)
        {
            throw new InvalidOperationException("Payment amount must be positive.");
        }

        if (payment.Amount > amountDue)
        {
            payment.Amount = amountDue;
        }

        payment.PaymentNumber = await GenerateNextPaymentNumberAsync(cancellationToken);
        payment.VendorId = bill.VendorId;
        payment.PaymentDate = payment.PaymentDate == default ? DateTime.UtcNow.Date : payment.PaymentDate;

        _dbContext.BillPayments.Add(payment);
        bill.AmountPaid += payment.Amount;
        UpdatePaymentStatus(bill);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Recorded payment {PaymentNumber} for bill {BillNumber}.", payment.PaymentNumber, bill.BillNumber);
        return payment;
    }

    public Task<string> PeekNextPaymentNumberAsync(CancellationToken cancellationToken = default)
    {
        return GenerateNextPaymentNumberAsync(cancellationToken);
    }

    private void UpdatePaymentStatus(VendorBill bill)
    {
        if (bill.AmountPaid <= 0)
        {
            bill.PaymentStatus = VendorBillPaymentStatus.NotPaid;
        }
        else if (bill.AmountPaid >= bill.TotalAmount)
        {
            bill.PaymentStatus = VendorBillPaymentStatus.Paid;
            bill.AmountPaid = Math.Min(bill.AmountPaid, bill.TotalAmount);
        }
        else
        {
            bill.PaymentStatus = VendorBillPaymentStatus.Partial;
        }
    }

    private async Task<string> GenerateNextPaymentNumberAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var prefix = $"PAY-{now:yyyyMM}-";
        var lastNumber = await _dbContext.BillPayments
            .Where(payment => payment.PaymentNumber.StartsWith(prefix))
            .OrderByDescending(payment => payment.PaymentNumber)
            .Select(payment => payment.PaymentNumber)
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
