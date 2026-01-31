using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.CustomerInvoices;

public class CustomerInvoicePaymentListItemViewModel
{
    public int CustomerInvoicePaymentId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentMode PaymentMode { get; set; }
    public string? Note { get; set; }
}
