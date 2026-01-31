using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public interface ICustomerInvoiceService
{
    Task<IReadOnlyList<CustomerInvoice>> GetInvoicesAsync(
        string? search,
        CustomerInvoiceStatus? status,
        CustomerInvoicePaymentStatus? paymentStatus,
        CancellationToken cancellationToken = default);

    Task<CustomerInvoice?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerInvoice>> GetCustomerInvoicesAsync(int customerId, CancellationToken cancellationToken = default);
    Task<CustomerInvoice?> GetForCustomerAsync(int invoiceId, int customerId, CancellationToken cancellationToken = default);

    Task<CustomerInvoice> CreateFromOrderAsync(
        int salesOrderId,
        DateTime invoiceDate,
        DateTime dueDate,
        CancellationToken cancellationToken = default);

    Task ConfirmAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task CancelAsync(int invoiceId, CancellationToken cancellationToken = default);

    Task<CustomerInvoicePayment> RecordPaymentAsync(
        int invoiceId,
        DateTime paymentDate,
        decimal amount,
        PaymentMode paymentMode,
        string? note,
        CancellationToken cancellationToken = default);
}
