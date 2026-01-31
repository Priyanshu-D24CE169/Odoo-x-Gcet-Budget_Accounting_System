using System.ComponentModel.DataAnnotations;

namespace ShivFurnitureERP.Models;

public class CustomerInvoicePayment
{
    public int CustomerInvoicePaymentId { get; set; }

    [MaxLength(30)]
    public string PaymentNumber { get; set; } = string.Empty;

    public int CustomerInvoiceId { get; set; }
    public CustomerInvoice? CustomerInvoice { get; set; }

    public int CustomerId { get; set; }
    public Contact? Customer { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
