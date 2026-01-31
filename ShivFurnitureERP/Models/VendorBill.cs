using System.ComponentModel.DataAnnotations;

namespace ShivFurnitureERP.Models;

public class VendorBill
{
    public int VendorBillId { get; set; }

    [MaxLength(30)]
    public string BillNumber { get; set; } = string.Empty;

    [Required]
    public int VendorId { get; set; }
    public Contact? Vendor { get; set; }

    public int? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public DateTime BillDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime DueDate { get; set; } = DateTime.UtcNow.Date.AddDays(30);

    public VendorBillStatus Status { get; set; } = VendorBillStatus.Draft;
    public VendorBillPaymentStatus PaymentStatus { get; set; } = VendorBillPaymentStatus.NotPaid;

    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedOn { get; set; }
    public DateTime? CancelledOn { get; set; }

    public ICollection<VendorBillLine> Lines { get; set; } = new List<VendorBillLine>();
    public ICollection<BillPayment> Payments { get; set; } = new List<BillPayment>();
}
