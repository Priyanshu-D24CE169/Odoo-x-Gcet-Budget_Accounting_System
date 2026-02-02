using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.Options;
using ShivFurnitureERP.Services;
using ShivFurnitureERP.ViewModels.CustomerInvoices;
using ShivFurnitureERP.ViewModels.Payments;

namespace ShivFurnitureERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class CustomerInvoicesController : Controller
{
    private readonly ICustomerInvoiceService _invoiceService;
    private readonly ISalesOrderService _salesOrderService;
    private readonly RazorpayPaymentService _razorpayService;
    private readonly ILogger<CustomerInvoicesController> _logger;
    private readonly bool _useRazorpay;

    public CustomerInvoicesController(
        ICustomerInvoiceService invoiceService,
        ISalesOrderService salesOrderService,
        RazorpayPaymentService razorpayService,
        ILogger<CustomerInvoicesController> logger,
        IOptions<RazorpayOptions> razorpayOptions)
    {
        _invoiceService = invoiceService;
        _salesOrderService = salesOrderService;
        _razorpayService = razorpayService;
        _logger = logger;
        _useRazorpay = !string.IsNullOrWhiteSpace(razorpayOptions.Value.KeyId);
    }

    public async Task<IActionResult> Index(
        string? search,
        CustomerInvoiceStatus? status,
        CustomerInvoicePaymentStatus? paymentStatus,
        CancellationToken cancellationToken)
    {
        var invoices = await _invoiceService.GetInvoicesAsync(search, status, paymentStatus, cancellationToken);
        var model = invoices.Select(MapToListItem).ToList();

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["PaymentStatus"] = paymentStatus;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int salesOrderId, CancellationToken cancellationToken)
    {
        if (salesOrderId <= 0)
        {
            return BadRequest("salesOrderId is required.");
        }

        var order = await _salesOrderService.GetByIdAsync(salesOrderId, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var viewModel = BuildFormViewModel(order);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerInvoiceFormViewModel model, CancellationToken cancellationToken)
    {
        var order = await _salesOrderService.GetByIdAsync(model.SalesOrderId, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var invalidViewModel = BuildFormViewModel(order);
            invalidViewModel.InvoiceDate = model.InvoiceDate;
            invalidViewModel.DueDate = model.DueDate;
            return View(invalidViewModel);
        }

        try
        {
            var invoice = await _invoiceService.CreateFromOrderAsync(model.SalesOrderId, model.InvoiceDate, model.DueDate, cancellationToken);
            TempData["StatusMessage"] = "Customer invoice created.";
            return RedirectToAction(nameof(Details), new { id = invoice.CustomerInvoiceId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var errorModel = BuildFormViewModel(order);
            errorModel.InvoiceDate = model.InvoiceDate;
            errorModel.DueDate = model.DueDate;
            return View(errorModel);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        var viewModel = MapToDetailsViewModel(invoice);
        ViewBag.UseRazorpay = _useRazorpay;
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, CancellationToken cancellationToken)
    {
        await _invoiceService.ConfirmAsync(id, cancellationToken);
        TempData["StatusMessage"] = "Invoice confirmed.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        await _invoiceService.CancelAsync(id, cancellationToken);
        TempData["StatusMessage"] = "Invoice cancelled.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordPayment(CustomerInvoicePaymentFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await ReturnDetailsWithModelErrorsAsync(model.CustomerInvoiceId, cancellationToken);
        }

        try
        {
            await _invoiceService.RecordPaymentAsync(
                model.CustomerInvoiceId,
                model.PaymentDate,
                model.Amount,
                model.PaymentMode,
                model.Note,
                cancellationToken);
            TempData["StatusMessage"] = "Payment recorded.";
            return RedirectToAction(nameof(Details), new { id = model.CustomerInvoiceId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await ReturnDetailsWithModelErrorsAsync(model.CustomerInvoiceId, cancellationToken);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRazorpayOrder(int invoiceId, decimal amount, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetByIdAsync(invoiceId, cancellationToken);
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
            amount = (int)(orderResponse.Amount * 100),
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
        var invoice = await _invoiceService.GetByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            return Json(new { success = false, message = "Invoice not found" });
        }

        var isValid = _razorpayService.VerifyPaymentSignature(razorpayOrderId, razorpayPaymentId, razorpaySignature);
        if (!isValid)
        {
            _logger.LogWarning("Invalid Razorpay signature for invoice {InvoiceId}, payment {PaymentId}", invoiceId, razorpayPaymentId);
            return Json(new { success = false, message = "Payment verification failed" });
        }

        var paymentDetails = await _razorpayService.FetchPaymentDetailsAsync(razorpayPaymentId, cancellationToken);
        if (!paymentDetails.Succeeded)
        {
            _logger.LogError("Razorpay payment {PaymentId} not successful for invoice {InvoiceId}", razorpayPaymentId, invoiceId);
            return Json(new { success = false, message = "Payment not successful" });
        }

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
                message = "Payment successful",
                paymentId = razorpayPaymentId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Razorpay payment {PaymentId} for invoice {InvoiceId}", razorpayPaymentId, invoiceId);
            return Json(new { success = false, message = "Failed to record payment" });
        }
    }

    private async Task<IActionResult> ReturnDetailsWithModelErrorsAsync(int invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        var viewModel = MapToDetailsViewModel(invoice);
        viewModel.PaymentForm.Amount = 0;
        return View("Details", viewModel);
    }

    private static CustomerInvoiceFormViewModel BuildFormViewModel(SalesOrder order)
    {
        var viewModel = new CustomerInvoiceFormViewModel
        {
            SalesOrderId = order.SalesOrderId,
            SalesOrderNumber = order.SONumber,
            CustomerName = order.Customer?.Name ?? "-"
        };

        foreach (var line in order.Lines)
        {
            var total = Math.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero);
            viewModel.Lines.Add(new CustomerInvoiceLineViewModel
            {
                ProductName = line.Product?.Name ?? $"Product #{line.ProductId}",
                AnalyticalAccountName = line.AnalyticalAccount?.Name,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                Total = total
            });
            viewModel.TotalAmount += total;
        }

        viewModel.TotalAmount = Math.Round(viewModel.TotalAmount, 2, MidpointRounding.AwayFromZero);
        return viewModel;
    }

    private static CustomerInvoiceListItemViewModel MapToListItem(CustomerInvoice invoice)
    {
        return new CustomerInvoiceListItemViewModel
        {
            CustomerInvoiceId = invoice.CustomerInvoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
            SalesOrderNumber = invoice.SalesOrder?.SONumber ?? "-",
            CustomerName = invoice.Customer?.Name ?? "-",
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            Status = invoice.Status,
            PaymentStatus = invoice.PaymentStatus,
            TotalAmount = invoice.TotalAmount,
            AmountPaid = invoice.AmountPaid
        };
    }

    private static CustomerInvoiceDetailsViewModel MapToDetailsViewModel(CustomerInvoice invoice)
    {
        var model = new CustomerInvoiceDetailsViewModel
        {
            CustomerInvoiceId = invoice.CustomerInvoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
            SalesOrderNumber = invoice.SalesOrder?.SONumber ?? "-",
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
                    AnalyticalAccountName = line.AnalyticalAccount?.Name,
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
            PaymentForm = new CustomerInvoicePaymentFormViewModel
            {
                CustomerInvoiceId = invoice.CustomerInvoiceId,
                PaymentDate = DateTime.UtcNow.Date
            },
            CanConfirm = invoice.Status == CustomerInvoiceStatus.Draft,
            CanCancel = invoice.Status != CustomerInvoiceStatus.Cancelled,
            CanRecordPayment = invoice.Status == CustomerInvoiceStatus.Confirmed && invoice.PaymentStatus != CustomerInvoicePaymentStatus.Paid
        };

        return model;
    }
}
