using System;
using System.Collections.Generic;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.ViewModels.CustomerInvoices;

namespace ShivFurnitureERP.ViewModels.PortalInvoices;

public class PortalCustomerInvoiceDetailsViewModel
{
    public int CustomerInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public CustomerInvoiceStatus Status { get; set; }
    public CustomerInvoicePaymentStatus PaymentStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue => Math.Max(0, TotalAmount - AmountPaid);

    public List<CustomerInvoiceLineViewModel> Lines { get; set; } = new();
    public List<CustomerInvoicePaymentListItemViewModel> Payments { get; set; } = new();

    public bool CanRecordPayment { get; set; }
    public PortalInvoicePaymentViewModel? PaymentForm { get; set; }
}
