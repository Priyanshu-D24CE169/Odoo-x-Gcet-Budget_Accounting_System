using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Budget_Accounting_System.Services;

public interface IPaymentService
{
    Task RecordPayment(Payment payment);
    Task UpdatePaymentStatus(int? invoiceId, int? billId);
}

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;

    public PaymentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task RecordPayment(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        await UpdatePaymentStatus(payment.CustomerInvoiceId, payment.VendorBillId);
    }

    public async Task UpdatePaymentStatus(int? invoiceId, int? billId)
    {
        if (invoiceId.HasValue)
        {
            var invoice = await _context.CustomerInvoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == invoiceId.Value);

            if (invoice != null)
            {
                var totalPaid = invoice.Payments.Sum(p => p.Amount);
                invoice.PaidAmount = totalPaid;

                if (totalPaid >= invoice.TotalAmount)
                {
                    invoice.PaymentStatus = PaymentStatus.Paid;
                }
                else if (totalPaid > 0)
                {
                    invoice.PaymentStatus = PaymentStatus.Partial;
                }
                else
                {
                    invoice.PaymentStatus = PaymentStatus.NotPaid;
                }

                await _context.SaveChangesAsync();
            }
        }


        if (billId.HasValue)
        {
            var bill = await _context.VendorBills
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == billId.Value);

            if (bill != null)
            {
                var totalPaid = bill.Payments.Sum(p => p.Amount);
                bill.PaidAmount = totalPaid;

                if (totalPaid >= bill.TotalAmount)
                {
                    bill.PaymentStatus = PaymentStatus.Paid;
                }
                else if (totalPaid > 0)
                {
                    bill.PaymentStatus = PaymentStatus.Partial;
                }
                else
                {
                    bill.PaymentStatus = PaymentStatus.NotPaid;
                }


                await _context.SaveChangesAsync();
            }
        }
    }
}
