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
using Microsoft.Extensions.Options;
using ShivFurnitureERP.Options;

namespace ShivFurnitureERP.Areas.Portal.Controllers;

[Area("Portal")]
[Authorize(Policy = "PortalOnly")]
public class CustomerInvoicesController : Controller
{
    private readonly ICustomerInvoiceService _invoiceService;
    private readonly IPaymentGatewaySimulator _paymentGateway;
    private readonly RazorpayPaymentService _razorpayService;
    private readonly IInvoicePdfService _pdfService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CustomerInvoicesController> _logger;
    private readonly bool _useRazorpay;

    public CustomerInvoicesController(
        ICustomerInvoiceService invoiceService,
        IPaymentGatewaySimulator paymentGateway,
        RazorpayPaymentService razorpayService,
        IInvoicePdfService pdfService,
        UserManager<ApplicationUser> userManager,
        ILogger<CustomerInvoicesController> logger,
        IOptions<RazorpayOptions> razorpayOptions)
    {
        _invoiceService = invoiceService;
        _paymentGateway = paymentGateway;
        _razorpayService = razorpayService;
        _pdfService = pdfService;
        _userManager = userManager;
        _logger = logger;
        _useRazorpay = !string.IsNullOrWhiteSpace(razorpayOptions.Value.KeyId);
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
        ViewBag.UseRazorpay = _useRazorpay;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRazorpayOrder(int invoiceId, decimal amount, CancellationToken cancellationToken)
    {
        var contactId = await GetContactIdAsync();
        if (contactId is null)
        {
            return Json(new { success = false, message = "User not authenticated" });
        }

        var invoice = await _invoiceService.GetForCustomerAsync(invoiceId, contactId.Value, cancellationToken);
        if (invoice is null)
        {
            return Json(new { success = false, message = "Invoice not found" });
        }

        if (invoice.PaymentStatus == CustomerInvoicePaymentStatus.Paid)
        {
            return Json(new { success = false, message = "Invoice already paid" });
        }

        var amountDue = Math.Max(0, invoice.TotalAmount - invoice.AmountPaid);
        if (amount <= 0 || amount > amountDue)
        {
            return Json(new { success = false, message = "Invalid payment amount" });
        }

        var request = new PaymentGatewayRequest
        {
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerName = invoice.Customer?.Name ?? "Customer",
            Amount = amount,
            Mode = PaymentMode.Online
        };

        var orderResponse = await _razorpayService.CreateOrderAsync(request, cancellationToken);
        if (!orderResponse.Success)
        {
            _logger.LogError("Failed to create Razorpay order for invoice {InvoiceId}: {Error}", invoiceId, orderResponse.ErrorMessage);
            return Json(new { success = false, message = orderResponse.ErrorMessage });
        }

        return Json(new
        {
            success = true,
            orderId = orderResponse.OrderId,
            amount = (int)(orderResponse.Amount * 100), // Convert to paise
            currency = orderResponse.Currency,
            keyId = orderResponse.KeyId,
            companyName = orderResponse.CompanyName,
            companyLogo = orderResponse.CompanyLogo,
            themeColor = orderResponse.ThemeColor,
            invoiceNumber = orderResponse.InvoiceNumber,
            customerName = orderResponse.CustomerName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyRazorpayPayment(
        int invoiceId,
        string razorpayOrderId,
        string razorpayPaymentId,
        string razorpaySignature,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var contactId = await GetContactIdAsync();
        if (contactId is null)
        {
            return Json(new { success = false, message = "User not authenticated" });
        }

        var invoice = await _invoiceService.GetForCustomerAsync(invoiceId, contactId.Value, cancellationToken);
        if (invoice is null)
        {
            return Json(new { success = false, message = "Invoice not found" });
        }

        // Verify signature
        var isValid = _razorpayService.VerifyPaymentSignature(razorpayOrderId, razorpayPaymentId, razorpaySignature);
        if (!isValid)
        {
            _logger.LogWarning("Invalid Razorpay signature for invoice {InvoiceId}, payment {PaymentId}", invoiceId, razorpayPaymentId);
            return Json(new { success = false, message = "Payment verification failed" });
        }

        // Fetch payment details
        var paymentDetails = await _razorpayService.FetchPaymentDetailsAsync(razorpayPaymentId, cancellationToken);
        if (!paymentDetails.Succeeded)
        {
            _logger.LogError("Razorpay payment {PaymentId} not successful for invoice {InvoiceId}", razorpayPaymentId, invoiceId);
            return Json(new { success = false, message = "Payment not successful" });
        }

        // Record payment
        try
        {
            await _invoiceService.RecordPaymentAsync(
                invoice.CustomerInvoiceId,
                DateTime.UtcNow.Date,
                amount,
                PaymentMode.Online,
                $"Razorpay: {razorpayPaymentId}",
                cancellationToken);

            _logger.LogInformation("Razorpay payment {PaymentId} recorded for invoice {InvoiceNumber}, amount: {Amount}",
                razorpayPaymentId, invoice.InvoiceNumber, amount);

            return Json(new
            {
                success = true,
                message = $"Payment successful! Transaction ID: {razorpayPaymentId}",
                transactionId = razorpayPaymentId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Razorpay payment {PaymentId} for invoice {InvoiceId}", razorpayPaymentId, invoiceId);
            return Json(new { success = false, message = "Failed to record payment. Please contact support." });
        }
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

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(int id, CancellationToken cancellationToken)
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

        try
        {
            var pdfBytes = _pdfService.GenerateInvoicePdf(invoice);
            var fileName = $"Invoice-{invoice.InvoiceNumber}.pdf";
            
            _logger.LogInformation("Generated PDF for invoice {InvoiceNumber}", invoice.InvoiceNumber);
            
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF for invoice {InvoiceId}", id);
            TempData["ErrorMessage"] = "Failed to generate PDF. Please try again.";
            return RedirectToAction(nameof(Details), new { id });
        }
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
