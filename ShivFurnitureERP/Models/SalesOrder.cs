using System.ComponentModel.DataAnnotations;

namespace ShivFurnitureERP.Models;

public class SalesOrder
{
    public int SalesOrderId { get; set; }

    [MaxLength(30)]
    public string SONumber { get; set; } = string.Empty;

    [Required]
    public int CustomerId { get; set; }
    public Contact? Customer { get; set; }

    public DateTime SODate { get; set; } = DateTime.UtcNow.Date;

    [MaxLength(150)]
    public string? Reference { get; set; }

    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedOn { get; set; }
    public DateTime? CancelledOn { get; set; }

    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
}
