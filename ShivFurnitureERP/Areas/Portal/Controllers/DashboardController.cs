using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.Services;
using ShivFurnitureERP.ViewModels;

namespace ShivFurnitureERP.Areas.Portal.Controllers;

[Area("Portal")]
[Authorize(Policy = "PortalOnly")]
public class DashboardController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICustomerInvoiceService _invoiceService;
    private readonly ISalesOrderService _salesOrderService;
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        UserManager<ApplicationUser> userManager,
        ICustomerInvoiceService invoiceService,
        ISalesOrderService salesOrderService,
        IPurchaseOrderService purchaseOrderService,
        ILogger<DashboardController> logger)
    {
        _userManager = userManager;
        _invoiceService = invoiceService;
        _salesOrderService = salesOrderService;
        _purchaseOrderService = purchaseOrderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        var model = await BuildDashboardAsync(user, cancellationToken);
        return View(model);
    }

    private async Task<PortalDashboardViewModel> BuildDashboardAsync(ApplicationUser? user, CancellationToken cancellationToken)
    {
        var model = new PortalDashboardViewModel
        {
            DisplayName = ResolveDisplayName(user),
            GeneratedOn = DateTime.UtcNow,
            ContactId = user?.ContactId
        };

        if (!model.HasContact)
        {
            _logger.LogWarning("Portal user {UserId} is missing a linked contact record.", user?.Id);
            return model;
        }

        var contactId = model.ContactId!.Value;

        var invoices = await _invoiceService.GetCustomerInvoicesAsync(contactId, cancellationToken);
        var salesOrders = (await _salesOrderService.GetOrdersAsync(null, null, cancellationToken))
            .Where(order => order.CustomerId == contactId)
            .ToList();
        var purchaseOrders = (await _purchaseOrderService.GetOrdersAsync(null, null, cancellationToken))
            .Where(order => order.VendorId == contactId)
            .ToList();

        PopulateInvoiceMetrics(model, invoices);
        PopulateSalesOrderMetrics(model, salesOrders);
        PopulatePurchaseOrderMetrics(model, purchaseOrders);
        model.Alerts = BuildAlerts(model);

        return model;
    }

    private static void PopulateInvoiceMetrics(PortalDashboardViewModel model, IReadOnlyList<CustomerInvoice> invoices)
    {
        var today = DateTime.UtcNow.Date;

        var outstanding = invoices
            .Where(invoice => invoice.Status == CustomerInvoiceStatus.Confirmed
                              && invoice.PaymentStatus != CustomerInvoicePaymentStatus.Paid)
            .ToList();

        model.OutstandingAmount = outstanding.Sum(invoice => Math.Max(0, invoice.TotalAmount - invoice.AmountPaid));
        model.OpenInvoices = outstanding.Count;
        model.OverdueInvoices = outstanding.Count(invoice => invoice.DueDate.Date < today);
        model.PaidThisYear = invoices
            .Where(invoice => invoice.InvoiceDate.Year == today.Year)
            .Sum(invoice => invoice.AmountPaid);

        model.UpcomingInvoices = outstanding
            .OrderBy(invoice => invoice.DueDate)
            .Take(4)
            .Select(invoice => MapInvoice(invoice, today))
            .ToList();

        model.RecentInvoices = invoices
            .OrderByDescending(invoice => invoice.InvoiceDate)
            .Take(6)
            .Select(invoice => MapInvoice(invoice, today))
            .ToList();
    }

    private static void PopulateSalesOrderMetrics(PortalDashboardViewModel model, IReadOnlyList<SalesOrder> orders)
    {
        model.TotalSalesOrders = orders.Count;
        model.ActiveSalesOrders = orders.Count(order => order.Status == SalesOrderStatus.Confirmed);
        model.SalesOrderValue = orders.Sum(order => order.Lines.Sum(line => line.Total));

        model.RecentSalesOrders = orders
            .OrderByDescending(order => order.SODate)
            .Take(4)
            .Select(order => new PortalDashboardOrderItem
            {
                Id = order.SalesOrderId,
                Number = order.SONumber,
                Date = order.SODate,
                StatusLabel = FormatSalesOrderStatus(order.Status),
                StatusCode = GetStatusCode(order.Status),
                TotalAmount = order.Lines.Sum(line => line.Total)
            })
            .ToList();
    }

    private static void PopulatePurchaseOrderMetrics(PortalDashboardViewModel model, IReadOnlyList<PurchaseOrder> orders)
    {
        model.TotalPurchaseOrders = orders.Count;
        model.ActivePurchaseOrders = orders.Count(order => order.Status == PurchaseOrderStatus.Confirmed);
        model.PurchaseOrderValue = orders.Sum(order => order.Lines.Sum(line => line.Total));

        model.RecentPurchaseOrders = orders
            .OrderByDescending(order => order.PODate)
            .Take(4)
            .Select(order => new PortalDashboardOrderItem
            {
                Id = order.PurchaseOrderId,
                Number = order.PONumber,
                Date = order.PODate,
                StatusLabel = FormatPurchaseOrderStatus(order.Status),
                StatusCode = GetStatusCode(order.Status),
                TotalAmount = order.Lines.Sum(line => line.Total)
            })
            .ToList();
    }

    private static PortalDashboardInvoiceItem MapInvoice(CustomerInvoice invoice, DateTime today)
    {
        return new PortalDashboardInvoiceItem
        {
            CustomerInvoiceId = invoice.CustomerInvoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            TotalAmount = invoice.TotalAmount,
            AmountPaid = invoice.AmountPaid,
            OutstandingAmount = Math.Max(0, invoice.TotalAmount - invoice.AmountPaid),
            Status = invoice.Status,
            PaymentStatus = invoice.PaymentStatus,
            IsOverdue = invoice.PaymentStatus != CustomerInvoicePaymentStatus.Paid && invoice.DueDate.Date < today
        };
    }

    private static IReadOnlyList<PortalDashboardAlertItem> BuildAlerts(PortalDashboardViewModel model)
    {
        var alerts = new List<PortalDashboardAlertItem>();

        if (model.OverdueInvoices > 0)
        {
            alerts.Add(new PortalDashboardAlertItem
            {
                Title = "Overdue invoices",
                Message = $"{model.OverdueInvoices} invoice(s) are past due. Please review payments.",
                Tone = "danger"
            });
        }

        if (model.OutstandingAmount <= 0 && model.OpenInvoices == 0)
        {
            alerts.Add(new PortalDashboardAlertItem
            {
                Title = "All caught up",
                Message = "There are no outstanding invoices at the moment.",
                Tone = "success"
            });
        }

        if (model.TotalSalesOrders > 0 && model.ActiveSalesOrders == 0)
        {
            alerts.Add(new PortalDashboardAlertItem
            {
                Title = "Sales orders waiting",
                Message = "Your sales orders are still in draft. Confirm them to move forward.",
                Tone = "warning"
            });
        }

        if (alerts.Count == 0)
        {
            alerts.Add(new PortalDashboardAlertItem
            {
                Title = "Up to date",
                Message = "You're all set. Nothing critical right now.",
                Tone = "info"
            });
        }

        return alerts;
    }

    private static string GetStatusCode(SalesOrderStatus status) => status switch
    {
        SalesOrderStatus.Confirmed => "confirmed",
        SalesOrderStatus.Cancelled => "cancelled",
        _ => "draft"
    };

    private static string GetStatusCode(PurchaseOrderStatus status) => status switch
    {
        PurchaseOrderStatus.Confirmed => "confirmed",
        PurchaseOrderStatus.Cancelled => "cancelled",
        _ => "draft"
    };

    private static string FormatSalesOrderStatus(SalesOrderStatus status) => status switch
    {
        SalesOrderStatus.Draft => "Draft",
        SalesOrderStatus.Confirmed => "Confirmed",
        SalesOrderStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    private static string FormatPurchaseOrderStatus(PurchaseOrderStatus status) => status switch
    {
        PurchaseOrderStatus.Draft => "Draft",
        PurchaseOrderStatus.Confirmed => "Confirmed",
        PurchaseOrderStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    private static string ResolveDisplayName(ApplicationUser? user)
    {
        if (user is null)
        {
            return "there";
        }

        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            return user.FullName;
        }

        if (!string.IsNullOrWhiteSpace(user.UserName))
        {
            return user.UserName;
        }

        return user.LoginId;
    }
}
