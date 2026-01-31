namespace Budget_Accounting_System.Models;

public class SalesOrder
{
    public int Id { get; set; }
    public string SONumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public DateTime SODate { get; set; } = DateTime.UtcNow;
    public string? Reference { get; set; }
    public SOStatus Status { get; set; } = SOStatus.Draft;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }

    public Contact Customer { get; set; } = null!;
    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
    public ICollection<CustomerInvoice> CustomerInvoices { get; set; } = new List<CustomerInvoice>();
}

public class SalesOrderLine
{
    public int Id { get; set; }
    public int SalesOrderId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int? AnalyticalAccountId { get; set; }

    public SalesOrder SalesOrder { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public AnalyticalAccount? AnalyticalAccount { get; set; }
}

public enum SOStatus
{
    Draft,
    Confirmed,
    Cancelled
}
