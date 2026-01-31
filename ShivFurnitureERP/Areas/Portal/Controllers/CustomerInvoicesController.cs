using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.Services;
using ShivFurnitureERP.ViewModels.CustomerInvoices;
using ShivFurnitureERP.ViewModels.Payments;
using ShivFurnitureERP.ViewModels.PortalInvoices;

namespace ShivFurnitureERP.Areas.Portal.Controllers;

[Area("Portal")]
[Authorize(Policy = "PortalOnly")]
public class CustomerInvoicesController : Controller
{
    private readonly ICustomerInvoiceService _invoiceService;
    private readonly IPaymentGatewaySimulator _paymentGateway;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CustomerInvoicesController> _logger;

    public CustomerInvoicesController(
        ICustomerInvoiceService invoiceService,
        IPaymentGatewaySimulator paymentGateway,
        UserManager<ApplicationUser> userManager,
        ILogger<CustomerInvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _paymentGateway = paymentGateway;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var contactId = await GetContactIdAsync();
        if (contactId is null)
        {
            return View(Array.Empty<PortalCustomerInvoiceListItemViewModel>());
        }

        var invoices = await _invoiceService.GetCustomerInvoicesAsync(contactId.Value, cancellationToken);
        var model = invoices.Select(invoice => new PortalCustomerInvoiceListItemViewModel
        {
            CustomerInvoiceId = invoice.CustomerInvoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            Status = invoice.Status,
            PaymentStatus = invoice.PaymentStatus,
            TotalAmount = invoice.TotalAmount,
            AmountPaid = invoice.AmountPaid
        }).ToList();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var contactId = await GetContactIdAsync();
        if (contactId is null)
        {
            return NotFound();
        }

        var invoice = await _invoiceService.GetForCustomerAsync(id, contactId.Value, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        var model = MapToDetailsViewModel(invoice);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(PortalInvoicePaymentViewModel model, CancellationToken cancellationToken)
    {
        var contactId = await GetContactIdAsync();
        if (contactId is null)
        {
            return NotFound();
        }

        var invoice = await _invoiceService.GetForCustomerAsync(model.CustomerInvoiceId, contactId.Value, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        if (invoice.PaymentStatus == CustomerInvoicePaymentStatus.Paid)
        {
            TempData["StatusMessage"] = "Invoice already paid.";
            return RedirectToAction(nameof(Details), new { id = invoice.CustomerInvoiceId });
        }

        var amountDue = Math.Max(0, invoice.TotalAmount - invoice.AmountPaid);
        if (model.Amount <= 0 || model.Amount > amountDue)
        {
            ModelState.AddModelError(nameof(model.Amount), "Amount must be between 0 and the outstanding balance.");
        }

        if (!ModelState.IsValid)
        {
            var errorModel = MapToDetailsViewModel(invoice);
            if (errorModel.PaymentForm is not null)
            {
                errorModel.PaymentForm.Amount = model.Amount;
                errorModel.PaymentForm.PaymentDate = model.PaymentDate;
                errorModel.PaymentForm.PaymentMode = model.PaymentMode;
            }

            return View("Details", errorModel);
        }

        var gatewayRequest = new PaymentGatewayRequest
        {
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerName = invoice.Customer?.Name ?? "-",
            Amount = model.Amount,
            Mode = model.PaymentMode
        };

        var gatewayResult = await _paymentGateway.CapturePaymentAsync(gatewayRequest, cancellationToken);
        if (!gatewayResult.Succeeded)
        {
            _logger.LogWarning("Gateway declined payment for invoice {Invoice}", invoice.InvoiceNumber);
            ModelState.AddModelError(string.Empty, gatewayResult.Message);
            var declinedModel = MapToDetailsViewModel(invoice);
            return View("Details", declinedModel);
        }

        await _invoiceService.RecordPaymentAsync(
            invoice.CustomerInvoiceId,
            model.PaymentDate,
            model.Amount,
            model.PaymentMode,
            $"Portal txn {gatewayResult.TransactionId}",
            cancellationToken);

        TempData["StatusMessage"] = $"Payment successful. Reference: {gatewayResult.TransactionId}.";
        return RedirectToAction(nameof(Details), new { id = invoice.CustomerInvoiceId });
    }

    private async Task<int?> GetContactIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.ContactId;
    }

    private static PortalCustomerInvoiceDetailsViewModel MapToDetailsViewModel(CustomerInvoice invoice)
    {
        var canPay = invoice.Status == CustomerInvoiceStatus.Confirmed && invoice.PaymentStatus != CustomerInvoicePaymentStatus.Paid;
        var paymentForm = canPay
            ? new PortalInvoicePaymentViewModel
            {
                CustomerInvoiceId = invoice.CustomerInvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                CustomerName = invoice.Customer?.Name ?? "-",
                PaymentNumberPreview = "Generated after payment",
                PaymentDate = DateTime.UtcNow.Date,
                Amount = Math.Max(0, invoice.TotalAmount - invoice.AmountPaid),
                PaymentMode = PaymentMode.Online
            }
            : null;

        return new PortalCustomerInvoiceDetailsViewModel
        {
            CustomerInvoiceId = invoice.CustomerInvoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerName = invoice.Customer?.Name ?? "-",
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            Status = invoice.Status,
            PaymentStatus = invoice.PaymentStatus,
            TotalAmount = invoice.TotalAmount,
            AmountPaid = invoice.AmountPaid,
            Lines = invoice.Lines
                .OrderBy(line => line.CustomerInvoiceLineId)
                .Select(line => new CustomerInvoiceLineViewModel
                {
                    ProductName = line.Product?.Name ?? $"Product #{line.ProductId}",
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    Total = line.Total
                }).ToList(),
            Payments = invoice.Payments
                .OrderByDescending(payment => payment.PaymentDate)
                .Select(payment => new CustomerInvoicePaymentListItemViewModel
                {
                    CustomerInvoicePaymentId = payment.CustomerInvoicePaymentId,
                    PaymentNumber = payment.PaymentNumber,
                    PaymentDate = payment.PaymentDate,
                    Amount = payment.Amount,
                    PaymentMode = payment.PaymentMode,
                    Note = payment.Note
                }).ToList(),
            CanRecordPayment = canPay,
            PaymentForm = paymentForm
        };
    }
}
