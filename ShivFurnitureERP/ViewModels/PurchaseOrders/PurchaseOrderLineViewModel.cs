using System.ComponentModel.DataAnnotations;

namespace ShivFurnitureERP.ViewModels.PurchaseOrders;

public class PurchaseOrderLineViewModel
{
    public int? PurchaseOrderLineId { get; set; }

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

    public bool HasBudgetWarning { get; set; }
    public string? BudgetWarningMessage { get; set; }
}
