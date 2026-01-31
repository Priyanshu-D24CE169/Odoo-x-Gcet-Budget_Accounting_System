using System;
using System.ComponentModel.DataAnnotations;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.PortalInvoices;

public class PortalInvoicePaymentViewModel
{
    [Required]
    public int CustomerInvoiceId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? PaymentNumberPreview { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Payment Date")]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be positive.")]
    public decimal Amount { get; set; }

    [Display(Name = "Payment Mode")]
    public PaymentMode PaymentMode { get; set; } = PaymentMode.Online;
}
