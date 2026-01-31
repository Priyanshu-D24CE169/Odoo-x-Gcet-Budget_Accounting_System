using System.ComponentModel.DataAnnotations.Schema;

namespace ShivFurnitureERP.Models;

public class VendorBillLine
{
    public int VendorBillLineId { get; set; }
    public int VendorBillId { get; set; }
    public VendorBill? VendorBill { get; set; }

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
