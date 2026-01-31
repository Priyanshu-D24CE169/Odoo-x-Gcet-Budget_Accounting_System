using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.BillPayments;

public class BillPaymentFormViewModel
{
    public int VendorBillId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public decimal AmountDue { get; set; }
    public string PaymentNumberPreview { get; set; } = string.Empty;

    [Display(Name = "Payment Date")]
    [DataType(DataType.Date)]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Display(Name = "Payment Mode")]
    public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;

    [MaxLength(500)]
    public string? Note { get; set; }

    public IEnumerable<SelectListItem> PaymentModes { get; set; } = Enumerable.Empty<SelectListItem>();
}
