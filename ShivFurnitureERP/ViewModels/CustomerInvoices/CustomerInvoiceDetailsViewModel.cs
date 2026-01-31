using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.CustomerInvoices;

public class CustomerInvoiceDetailsViewModel
{
    public int CustomerInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string SalesOrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public CustomerInvoiceStatus Status { get; set; }
    public CustomerInvoicePaymentStatus PaymentStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue => Math.Max(0, TotalAmount - AmountPaid);

    public List<CustomerInvoiceLineViewModel> Lines { get; set; } = new();
    public List<CustomerInvoicePaymentListItemViewModel> Payments { get; set; } = new();
    public CustomerInvoicePaymentFormViewModel PaymentForm { get; set; } = new();

    public bool CanConfirm { get; set; }
    public bool CanCancel { get; set; }
    public bool CanRecordPayment { get; set; }
}
