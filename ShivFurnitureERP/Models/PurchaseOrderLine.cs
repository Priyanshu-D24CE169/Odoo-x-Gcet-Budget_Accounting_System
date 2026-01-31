using System.ComponentModel.DataAnnotations.Schema;

namespace ShivFurnitureERP.Models;

public class PurchaseOrderLine
{
    public int PurchaseOrderLineId { get; set; }
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    public int? AnalyticalAccountId { get; set; }
    public AnalyticalAccount? AnalyticalAccount { get; set; }
}
