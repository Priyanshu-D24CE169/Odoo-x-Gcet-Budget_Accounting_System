using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Budget_Accounting_System.Models;

public class Contact
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Contact name is required")]
    [StringLength(200)]
    [Display(Name = "Contact Name")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Phone is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;
    
    [StringLength(500)]
    [Display(Name = "Profile Image")]
    public string? ImagePath { get; set; }
    
    [Required]
    public ContactState State { get; set; } = ContactState.New;
    
    // Address Fields
    [StringLength(200)]
    public string? Street { get; set; }
    
    [StringLength(100)]
    public string? City { get; set; }
    
    [StringLength(100)]
    [Display(Name = "State/Province")]
    public string? StateName { get; set; }
    
    [StringLength(100)]
    public string? Country { get; set; }
    
    [StringLength(20)]
    [Display(Name = "PIN Code")]
    public string? Pincode { get; set; }
    
    [Required]
    public ContactType Type { get; set; }
    
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
    
    // Deprecated - kept for backward compatibility
    [Obsolete("Use State property instead")]
    [NotMapped]
    public bool IsActive => State == ContactState.Confirmed;
    
    [Obsolete("Use UpdatedDate instead")]
    [NotMapped]
    public DateTime? ModifiedDate => UpdatedDate;
    
    // Old Address field - kept for backward compatibility
    [Obsolete("Use Street, City, StateName, Country, Pincode instead")]
    [NotMapped]
    public string? Address => $"{Street}, {City}, {StateName}, {Country} - {Pincode}".Trim(' ', ',', '-');

    // User account association (one-to-one relationship)
    public ApplicationUser? User { get; set; }

    // Tags (many-to-many)
    public ICollection<ContactTag> ContactTags { get; set; } = new List<ContactTag>();
    
    [NotMapped]
    public List<int> SelectedTagIds { get; set; } = new();

    // Transactions
    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
    public ICollection<CustomerInvoice> CustomerInvoices { get; set; } = new List<CustomerInvoice>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<VendorBill> VendorBills { get; set; } = new List<VendorBill>();
}

public enum ContactState
{
    New,
    Confirmed,
    Archived
}

public enum ContactType
{
    Customer,
    Vendor,
    Both
}



