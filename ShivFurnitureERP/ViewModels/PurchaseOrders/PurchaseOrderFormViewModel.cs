using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.PurchaseOrders;

public class PurchaseOrderFormViewModel
{
    public int? PurchaseOrderId { get; set; }

    [Display(Name = "PO Number")]
    public string? PONumber { get; set; }

    [Required]
    [Display(Name = "Vendor")]
    public int? VendorId { get; set; }

    [MaxLength(150)]
    public string? Reference { get; set; }

    [Display(Name = "PO Date")]
    [DataType(DataType.Date)]
    public DateTime PODate { get; set; } = DateTime.UtcNow.Date;

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    public List<PurchaseOrderLineViewModel> Lines { get; set; } = new();

    public IEnumerable<SelectListItem> Vendors { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Products { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> AnalyticalAccounts { get; set; } = Enumerable.Empty<SelectListItem>();

    public bool CanConfirm => Status == PurchaseOrderStatus.Draft;
    public bool CanCreateBill => Status == PurchaseOrderStatus.Confirmed;
}
