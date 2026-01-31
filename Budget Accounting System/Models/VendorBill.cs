namespace Budget_Accounting_System.Models;

public class VendorBill
{
    public int Id { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public int VendorId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public DateTime BillDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public string? Reference { get; set; }
    public BillStatus Status { get; set; } = BillStatus.Draft;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.NotPaid;
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }

    public Contact Vendor { get; set; } = null!;
    public PurchaseOrder? PurchaseOrder { get; set; }
    public ICollection<VendorBillLine> Lines { get; set; } = new List<VendorBillLine>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class VendorBillLine
{
    public int Id { get; set; }
    public int VendorBillId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int? AnalyticalAccountId { get; set; }

    public VendorBill VendorBill { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public AnalyticalAccount? AnalyticalAccount { get; set; }
}

public enum BillStatus
{
    Draft,
    Posted,
    Cancelled
}

public enum PaymentStatus
{
    NotPaid,
    Partial,
    Paid
}

