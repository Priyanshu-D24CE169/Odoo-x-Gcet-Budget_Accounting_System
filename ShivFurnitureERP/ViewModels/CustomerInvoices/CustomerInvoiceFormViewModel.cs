using System.ComponentModel.DataAnnotations;

namespace ShivFurnitureERP.ViewModels.CustomerInvoices;

public class CustomerInvoiceFormViewModel
{
    [Required]
    public int SalesOrderId { get; set; }

    public string SalesOrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Invoice Date")]
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow.Date;

    [DataType(DataType.Date)]
    [Display(Name = "Due Date")]
    public DateTime DueDate { get; set; } = DateTime.UtcNow.Date.AddDays(30);

    public decimal TotalAmount { get; set; }

    public List<CustomerInvoiceLineViewModel> Lines { get; set; } = new();
}
