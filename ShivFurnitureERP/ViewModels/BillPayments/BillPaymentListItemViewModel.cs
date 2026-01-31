using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.BillPayments;

public class BillPaymentListItemViewModel
{
    public int BillPaymentId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentMode PaymentMode { get; set; }
    public string? Note { get; set; }
}
