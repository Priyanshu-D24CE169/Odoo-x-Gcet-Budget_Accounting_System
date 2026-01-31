namespace Budget_Accounting_System.Models;

public class PurchaseOrder
{
    public int Id { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public int VendorId { get; set; }
    public DateTime PODate { get; set; } = DateTime.UtcNow;
    public string? Reference { get; set; }
    public POStatus Status { get; set; } = POStatus.Draft;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }

    public Contact Vendor { get; set; } = null!;
    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
    public ICollection<VendorBill> VendorBills { get; set; } = new List<VendorBill>();
}

public class PurchaseOrderLine
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int? AnalyticalAccountId { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public AnalyticalAccount? AnalyticalAccount { get; set; }
}

public enum POStatus
{
    Draft,
    Confirmed,
    Cancelled
}
