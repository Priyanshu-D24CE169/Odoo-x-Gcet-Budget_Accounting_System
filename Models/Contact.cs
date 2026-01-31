namespace Budget_Accounting_System.Models;

public class Contact
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public ContactType Type { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }

    // User account association (one-to-one relationship)
    public ApplicationUser? User { get; set; }

    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
    public ICollection<CustomerInvoice> CustomerInvoices { get; set; } = new List<CustomerInvoice>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<VendorBill> VendorBills { get; set; } = new List<VendorBill>();
}

public enum ContactType
{
    Customer,
    Vendor,
    Both
}


