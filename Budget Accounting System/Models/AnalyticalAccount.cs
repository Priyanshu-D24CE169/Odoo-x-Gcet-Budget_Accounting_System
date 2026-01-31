namespace Budget_Accounting_System.Models;

public class AnalyticalAccount
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }

    public AnalyticalAccount? Parent { get; set; }
    public ICollection<AnalyticalAccount> Children { get; set; } = new List<AnalyticalAccount>();
    public ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();
    public ICollection<VendorBillLine> VendorBillLines { get; set; } = new List<VendorBillLine>();
    public ICollection<SalesOrderLine> SalesOrderLines { get; set; } = new List<SalesOrderLine>();
    public ICollection<CustomerInvoiceLine> CustomerInvoiceLines { get; set; } = new List<CustomerInvoiceLine>();
}

