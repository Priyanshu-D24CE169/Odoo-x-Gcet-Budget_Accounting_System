using System.ComponentModel.DataAnnotations;

namespace ShivFurnitureERP.Models;

public class BillPayment
{
    public int BillPaymentId { get; set; }

    [MaxLength(30)]
    public string PaymentNumber { get; set; } = string.Empty;

    public int VendorBillId { get; set; }
    public VendorBill? VendorBill { get; set; }

    public int VendorId { get; set; }
    public Contact? Vendor { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
