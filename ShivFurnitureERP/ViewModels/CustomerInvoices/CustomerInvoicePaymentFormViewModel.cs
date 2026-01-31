using System.ComponentModel.DataAnnotations;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.CustomerInvoices;

public class CustomerInvoicePaymentFormViewModel
{
    [Required]
    public int CustomerInvoiceId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Payment Date")]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Display(Name = "Payment Mode")]
    public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;

    [MaxLength(500)]
    public string? Note { get; set; }
}
