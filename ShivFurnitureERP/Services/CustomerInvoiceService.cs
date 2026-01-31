using System.Linq;
using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public class CustomerInvoiceService : ICustomerInvoiceService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CustomerInvoiceService> _logger;

    public CustomerInvoiceService(ApplicationDbContext dbContext, ILogger<CustomerInvoiceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CustomerInvoice>> GetInvoicesAsync(
        string? search,
        CustomerInvoiceStatus? status,
        CustomerInvoicePaymentStatus? paymentStatus,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CustomerInvoices
            .Include(invoice => invoice.Customer)
            .Include(invoice => invoice.SalesOrder)
            .OrderByDescending(invoice => invoice.CustomerInvoiceId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(invoice =>
                invoice.InvoiceNumber.Contains(term) ||
                (invoice.Customer != null && invoice.Customer.Name.Contains(term)) ||
                (invoice.SalesOrder != null && invoice.SalesOrder.SONumber.Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(invoice => invoice.Status == status.Value);
        }

        if (paymentStatus.HasValue)
        {
            query = query.Where(invoice => invoice.PaymentStatus == paymentStatus.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task<CustomerInvoice?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.CustomerInvoices
            .Include(invoice => invoice.Customer)
            .Include(invoice => invoice.SalesOrder)
            .Include(invoice => invoice.Lines)
                .ThenInclude(line => line.Product)
            .Include(invoice => invoice.Lines)
                .ThenInclude(line => line.AnalyticalAccount)
            .Include(invoice => invoice.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(invoice => invoice.CustomerInvoiceId == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerInvoice>> GetCustomerInvoicesAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var invoices = await _dbContext.CustomerInvoices
            .Include(invoice => invoice.Customer)
            .Include(invoice => invoice.SalesOrder)
            .Where(invoice => invoice.CustomerId == customerId)
            .OrderByDescending(invoice => invoice.CustomerInvoiceId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return invoices;
    }

    public Task<CustomerInvoice?> GetForCustomerAsync(int invoiceId, int customerId, CancellationToken cancellationToken = default)
    {
        return _dbContext.CustomerInvoices
            .Include(invoice => invoice.Customer)
            .Include(invoice => invoice.SalesOrder)
            .Include(invoice => invoice.Lines)
                .ThenInclude(line => line.Product)
            .Include(invoice => invoice.Lines)
                .ThenInclude(line => line.AnalyticalAccount)
            .Include(invoice => invoice.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(invoice => invoice.CustomerInvoiceId == invoiceId && invoice.CustomerId == customerId, cancellationToken);
    }

    public async Task<CustomerInvoice> CreateFromOrderAsync(int salesOrderId, DateTime invoiceDate, DateTime dueDate, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
                .ThenInclude(line => line.Product)
            .FirstOrDefaultAsync(o => o.SalesOrderId == salesOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Sales Order {salesOrderId} not found.");

        if (order.Status != SalesOrderStatus.Confirmed)
        {
            throw new InvalidOperationException("Only confirmed sales orders can be invoiced.");
        }

        var existingInvoice = await _dbContext.CustomerInvoices
            .AnyAsync(invoice => invoice.SalesOrderId == salesOrderId, cancellationToken);
        if (existingInvoice)
        {
            throw new InvalidOperationException("An invoice already exists for this sales order.");
        }

        var invoice = new CustomerInvoice
        {
            SalesOrderId = order.SalesOrderId,
            CustomerId = order.CustomerId,
            InvoiceDate = invoiceDate,
            DueDate = dueDate,
            InvoiceNumber = await GenerateNextInvoiceNumberAsync(cancellationToken),
            CreatedOn = DateTime.UtcNow,
            Status = CustomerInvoiceStatus.Draft,
            PaymentStatus = CustomerInvoicePaymentStatus.NotPaid
        };

        foreach (var line in order.Lines)
        {
            var total = Math.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero);
            invoice.Lines.Add(new CustomerInvoiceLine
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                Total = total,
                AnalyticalAccountId = line.AnalyticalAccountId
            });
            invoice.TotalAmount += total;
        }

        invoice.TotalAmount = Math.Round(invoice.TotalAmount, 2, MidpointRounding.AwayFromZero);

        _dbContext.CustomerInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return invoice;
    }

    public async Task ConfirmAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _dbContext.CustomerInvoices
            .FirstOrDefaultAsync(i => i.CustomerInvoiceId == invoiceId, cancellationToken)
            ?? throw new InvalidOperationException($"Customer Invoice {invoiceId} not found.");

        if (invoice.Status == CustomerInvoiceStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled invoices cannot be confirmed.");
        }

        if (invoice.Status == CustomerInvoiceStatus.Confirmed)
        {
            return;
        }

        invoice.Status = CustomerInvoiceStatus.Confirmed;
        invoice.ConfirmedOn = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _dbContext.CustomerInvoices
            .FirstOrDefaultAsync(i => i.CustomerInvoiceId == invoiceId, cancellationToken)
            ?? throw new InvalidOperationException($"Customer Invoice {invoiceId} not found.");

        if (invoice.Status == CustomerInvoiceStatus.Cancelled)
        {
            return;
        }

        invoice.Status = CustomerInvoiceStatus.Cancelled;
        invoice.CancelledOn = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CustomerInvoicePayment> RecordPaymentAsync(
        int invoiceId,
        DateTime paymentDate,
        decimal amount,
        PaymentMode paymentMode,
        string? note,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Payment amount must be greater than zero.");
        }

        var invoice = await _dbContext.CustomerInvoices
            .FirstOrDefaultAsync(i => i.CustomerInvoiceId == invoiceId, cancellationToken)
            ?? throw new InvalidOperationException($"Customer Invoice {invoiceId} not found.");

        if (invoice.Status == CustomerInvoiceStatus.Cancelled)
        {
            throw new InvalidOperationException("Payments cannot be recorded for cancelled invoices.");
        }

        if (invoice.Status == CustomerInvoiceStatus.Draft)
        {
            throw new InvalidOperationException("Confirm the invoice before recording payments.");
        }

        var payment = new CustomerInvoicePayment
        {
            CustomerInvoiceId = invoice.CustomerInvoiceId,
            CustomerId = invoice.CustomerId,
            PaymentDate = paymentDate,
            Amount = amount,
            PaymentMode = paymentMode,
            Note = note,
            PaymentNumber = await GenerateNextPaymentNumberAsync(cancellationToken),
            CreatedOn = DateTime.UtcNow
        };

        invoice.AmountPaid = Math.Round(invoice.AmountPaid + amount, 2, MidpointRounding.AwayFromZero);
        UpdatePaymentStatus(invoice);

        _dbContext.CustomerInvoicePayments.Add(payment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return payment;
    }

    private async Task<string> GenerateNextInvoiceNumberAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var prefix = $"INV-{now:yyyyMM}-";
        var lastNumber = await _dbContext.CustomerInvoices
            .Where(invoice => invoice.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(invoice => invoice.InvoiceNumber)
            .Select(invoice => invoice.InvoiceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var sequence = 1;
        if (!string.IsNullOrEmpty(lastNumber) && int.TryParse(lastNumber[prefix.Length..], out var parsed))
        {
            sequence = parsed + 1;
        }

        return prefix + sequence.ToString("D4");
    }

    private async Task<string> GenerateNextPaymentNumberAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var prefix = $"RCPT-{now:yyyyMM}-";
        var lastNumber = await _dbContext.CustomerInvoicePayments
            .Where(payment => payment.PaymentNumber.StartsWith(prefix))
            .OrderByDescending(payment => payment.PaymentNumber)
            .Select(payment => payment.PaymentNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var sequence = 1;
        if (!string.IsNullOrEmpty(lastNumber) && int.TryParse(lastNumber[prefix.Length..], out var parsed))
        {
            sequence = parsed + 1;
        }

        return prefix + sequence.ToString("D4");
    }

    private static void UpdatePaymentStatus(CustomerInvoice invoice)
    {
        if (invoice.AmountPaid <= 0)
        {
            invoice.PaymentStatus = CustomerInvoicePaymentStatus.NotPaid;
            return;
        }

        invoice.PaymentStatus = invoice.AmountPaid >= invoice.TotalAmount
            ? CustomerInvoicePaymentStatus.Paid
            : CustomerInvoicePaymentStatus.Partial;
    }
}
