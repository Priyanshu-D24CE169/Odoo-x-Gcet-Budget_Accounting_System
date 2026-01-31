using System.ComponentModel.DataAnnotations;

namespace ShivFurnitureERP.ViewModels.SalesOrders;

public class SalesOrderLineViewModel
{
    public int? SalesOrderLineId { get; set; }

    [Required]
    [Display(Name = "Product")]
    public int? ProductId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; } = 1;

    [Range(0, double.MaxValue)]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    public decimal LineTotal => Math.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);

    [Display(Name = "Analytical Account")]
    public int? AnalyticalAccountId { get; set; }
}
