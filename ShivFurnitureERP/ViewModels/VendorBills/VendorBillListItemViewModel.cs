using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.VendorBills;

public class VendorBillListItemViewModel
{
    public int VendorBillId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
    public DateTime DueDate { get; set; }
    public VendorBillStatus Status { get; set; }
    public VendorBillPaymentStatus PaymentStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue => Math.Max(TotalAmount - AmountPaid, 0);
}
