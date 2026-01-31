using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.SalesOrders;

public class SalesOrderFormViewModel
{
    public int? SalesOrderId { get; set; }

    [Display(Name = "SO Number")]
    public string? SONumber { get; set; }

    [Required]
    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "SO Date")]
    [DataType(DataType.Date)]
    public DateTime SODate { get; set; } = DateTime.UtcNow.Date;

    [MaxLength(150)]
    public string? Reference { get; set; }

    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;

    public List<SalesOrderLineViewModel> Lines { get; set; } = new();

    public IEnumerable<SelectListItem> Customers { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Products { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> AnalyticalAccounts { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public bool CanConfirm => Status == SalesOrderStatus.Draft;
    public bool CanCancel => Status != SalesOrderStatus.Cancelled;
    public bool CanCreateInvoice => Status == SalesOrderStatus.Confirmed;
}
