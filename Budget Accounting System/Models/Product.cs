using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Budget_Accounting_System.Models;

public class Product
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Product name is required")]
    [StringLength(200)]
    [Display(Name = "Product Name")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [Required(ErrorMessage = "Category is required")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }
    
    [Required(ErrorMessage = "Sales price is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Sales price must be zero or greater")]
    [Display(Name = "Sales Price")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal SalesPrice { get; set; }
    
    [Required(ErrorMessage = "Purchase price is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Purchase price must be zero or greater")]
    [Display(Name = "Purchase Price")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PurchasePrice { get; set; }
    
    [StringLength(50)]
    public string? Unit { get; set; }
    
    [Required]
    public ProductState State { get; set; } = ProductState.New;
    
    // System Fields
    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    [Display(Name = "Updated Date")]
    public DateTime? UpdatedDate { get; set; }
    
    [Display(Name = "Confirmed Date")]
    public DateTime? ConfirmedDate { get; set; }
    
    [Display(Name = "Archived Date")]
    public DateTime? ArchivedDate { get; set; }
    
    [Display(Name = "Confirmed By")]
    public string? ConfirmedBy { get; set; }
    
    [Display(Name = "Archived By")]
    public string? ArchivedBy { get; set; }
    
    // Navigation
    public Category Category { get; set; } = null!;
    
    // Deprecated - kept for backward compatibility
    [Obsolete("Use State property instead")]
    [NotMapped]
    public bool IsActive => State == ProductState.Confirmed;
    
    [Obsolete("Use UpdatedDate instead")]
    [NotMapped]
    public DateTime? ModifiedDate => UpdatedDate;
    
    [Obsolete("Use SalesPrice instead")]
    [NotMapped]
    public decimal UnitPrice => SalesPrice;

    // Transactions
    public ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();
    public ICollection<VendorBillLine> VendorBillLines { get; set; } = new List<VendorBillLine>();
    public ICollection<SalesOrderLine> SalesOrderLines { get; set; } = new List<SalesOrderLine>();
    public ICollection<CustomerInvoiceLine> CustomerInvoiceLines { get; set; } = new List<CustomerInvoiceLine>();
}

public enum ProductState
{
    New,
    Confirmed,
    Archived
}

