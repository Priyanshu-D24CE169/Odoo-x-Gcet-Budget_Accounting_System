using System.ComponentModel.DataAnnotations;

namespace ShivFurnitureERP.Models;

public class CustomerInvoiceLine
{
    public int CustomerInvoiceLineId { get; set; }

    public int CustomerInvoiceId { get; set; }
    public CustomerInvoice? CustomerInvoice { get; set; }

    [Required]
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    public decimal Total { get; set; }

    public int? AnalyticalAccountId { get; set; }
    public AnalyticalAccount? AnalyticalAccount { get; set; }
}
