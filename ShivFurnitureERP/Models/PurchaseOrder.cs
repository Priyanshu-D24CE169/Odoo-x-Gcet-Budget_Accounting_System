using System.ComponentModel.DataAnnotations;

namespace ShivFurnitureERP.Models;

public class PurchaseOrder
{
    public int PurchaseOrderId { get; set; }

    [MaxLength(30)]
    public string PONumber { get; set; } = string.Empty;

    [Required]
    public int VendorId { get; set; }
    public Contact? Vendor { get; set; }

    [MaxLength(150)]
    public string? Reference { get; set; }

    public DateTime PODate { get; set; } = DateTime.UtcNow;

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedOn { get; set; }
    public DateTime? CancelledOn { get; set; }

    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}
