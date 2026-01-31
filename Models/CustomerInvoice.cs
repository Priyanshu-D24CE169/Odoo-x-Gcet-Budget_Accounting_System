namespace Budget_Accounting_System.Models;

public class CustomerInvoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int? SalesOrderId { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public string? Reference { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.NotPaid;
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }

    public Contact Customer { get; set; } = null!;
    public SalesOrder? SalesOrder { get; set; }
    public ICollection<CustomerInvoiceLine> Lines { get; set; } = new List<CustomerInvoiceLine>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class CustomerInvoiceLine
{
    public int Id { get; set; }
    public int CustomerInvoiceId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int? AnalyticalAccountId { get; set; }

    public CustomerInvoice CustomerInvoice { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public AnalyticalAccount? AnalyticalAccount { get; set; }
}

public enum InvoiceStatus
{
    Draft,
    Posted,
    Cancelled
}

