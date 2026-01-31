namespace ShivFurnitureERP.ViewModels.CustomerInvoices;

public class CustomerInvoiceLineViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public string? AnalyticalAccountName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}
