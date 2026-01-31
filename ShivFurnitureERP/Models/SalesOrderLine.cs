using System.ComponentModel.DataAnnotations.Schema;

namespace ShivFurnitureERP.Models;

public class SalesOrderLine
{
    public int SalesOrderLineId { get; set; }

    public int SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public int ProductId { get; set; }
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
