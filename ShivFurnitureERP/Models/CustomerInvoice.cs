using System.ComponentModel.DataAnnotations;

namespace ShivFurnitureERP.Models;

public class CustomerInvoice
{
    public int CustomerInvoiceId { get; set; }

    [MaxLength(30)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    public int SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    [Required]
    public int CustomerId { get; set; }
    public Contact? Customer { get; set; }

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime DueDate { get; set; } = DateTime.UtcNow.Date.AddDays(30);

    public CustomerInvoiceStatus Status { get; set; } = CustomerInvoiceStatus.Draft;
    public CustomerInvoicePaymentStatus PaymentStatus { get; set; } = CustomerInvoicePaymentStatus.NotPaid;

    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedOn { get; set; }
    public DateTime? CancelledOn { get; set; }

    public ICollection<CustomerInvoiceLine> Lines { get; set; } = new List<CustomerInvoiceLine>();
    public ICollection<CustomerInvoicePayment> Payments { get; set; } = new List<CustomerInvoicePayment>();
}
