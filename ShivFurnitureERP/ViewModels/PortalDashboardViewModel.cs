using System;
using System.Collections.Generic;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels;

public class PortalDashboardViewModel
{
    public string DisplayName { get; set; } = "there";
    public DateTime GeneratedOn { get; set; } = DateTime.UtcNow;
    public int? ContactId { get; set; }
    public bool HasContact => ContactId.HasValue;

    public decimal OutstandingAmount { get; set; }
    public decimal PaidThisYear { get; set; }
    public int OverdueInvoices { get; set; }
    public int OpenInvoices { get; set; }

    public int TotalSalesOrders { get; set; }
    public int ActiveSalesOrders { get; set; }
    public decimal SalesOrderValue { get; set; }

    public int TotalPurchaseOrders { get; set; }
    public int ActivePurchaseOrders { get; set; }
    public decimal PurchaseOrderValue { get; set; }

    public IReadOnlyList<PortalDashboardInvoiceItem> UpcomingInvoices { get; set; } = Array.Empty<PortalDashboardInvoiceItem>();
    public IReadOnlyList<PortalDashboardInvoiceItem> RecentInvoices { get; set; } = Array.Empty<PortalDashboardInvoiceItem>();
    public IReadOnlyList<PortalDashboardOrderItem> RecentSalesOrders { get; set; } = Array.Empty<PortalDashboardOrderItem>();
    public IReadOnlyList<PortalDashboardOrderItem> RecentPurchaseOrders { get; set; } = Array.Empty<PortalDashboardOrderItem>();
    public IReadOnlyList<PortalDashboardAlertItem> Alerts { get; set; } = Array.Empty<PortalDashboardAlertItem>();
}

public class PortalDashboardInvoiceItem
{
    public int CustomerInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal OutstandingAmount { get; set; }
    public CustomerInvoiceStatus Status { get; set; }
    public CustomerInvoicePaymentStatus PaymentStatus { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsPaid => PaymentStatus == CustomerInvoicePaymentStatus.Paid;
}

public class PortalDashboardOrderItem
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

public class PortalDashboardAlertItem
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Tone { get; set; } = "info";
}
