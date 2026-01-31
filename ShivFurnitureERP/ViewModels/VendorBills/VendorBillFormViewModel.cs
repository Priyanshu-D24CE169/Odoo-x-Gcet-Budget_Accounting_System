using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.ViewModels.BillPayments;

namespace ShivFurnitureERP.ViewModels.VendorBills;

public class VendorBillFormViewModel
{
    public int? VendorBillId { get; set; }

    [Display(Name = "Bill Number")]
    public string? BillNumber { get; set; }

    [Required]
    [Display(Name = "Vendor")]
    public int? VendorId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Bill Date")]
    public DateTime BillDate { get; set; } = DateTime.UtcNow.Date;

    [DataType(DataType.Date)]
    [Display(Name = "Due Date")]
    public DateTime DueDate { get; set; } = DateTime.UtcNow.Date.AddDays(30);

    public VendorBillStatus Status { get; set; } = VendorBillStatus.Draft;
    public VendorBillPaymentStatus PaymentStatus { get; set; } = VendorBillPaymentStatus.NotPaid;

    public decimal AmountPaid { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountDue => Math.Max(TotalAmount - AmountPaid, 0);

    public int? PurchaseOrderId { get; set; }

    public List<VendorBillLineViewModel> Lines { get; set; } = new();
    public List<BillPaymentListItemViewModel> Payments { get; set; } = new();

    public IEnumerable<SelectListItem> Vendors { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Products { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> AnalyticalAccounts { get; set; } = Enumerable.Empty<SelectListItem>();

    public bool CanEdit => Status == VendorBillStatus.Draft;
    public bool CanConfirm => Status == VendorBillStatus.Draft;
    public bool CanCancel => Status != VendorBillStatus.Cancelled;
    public bool CanPay => Status == VendorBillStatus.Confirmed && AmountDue > 0;
}
