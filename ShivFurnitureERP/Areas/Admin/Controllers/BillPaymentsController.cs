using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.Services;
using ShivFurnitureERP.ViewModels.BillPayments;
using ShivFurnitureERP.ViewModels.VendorBills;
using System.Linq;

namespace ShivFurnitureERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class BillPaymentsController : Controller
{
    private readonly IVendorBillService _vendorBillService;
    private readonly IBillPaymentService _billPaymentService;

    public BillPaymentsController(IVendorBillService vendorBillService, IBillPaymentService billPaymentService)
    {
        _vendorBillService = vendorBillService;
        _billPaymentService = billPaymentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, VendorBillPaymentStatus? paymentStatus, CancellationToken cancellationToken)
    {
        var bills = await _vendorBillService.GetBillsAsync(search, null, paymentStatus, cancellationToken);
        var filtered = paymentStatus.HasValue
            ? bills
            : bills.Where(b => b.PaymentStatus != VendorBillPaymentStatus.Paid).ToList();

        var model = filtered.Select(bill => new VendorBillListItemViewModel
        {
            VendorBillId = bill.VendorBillId,
            BillNumber = bill.BillNumber,
            VendorName = bill.Vendor?.Name ?? "-",
            BillDate = bill.BillDate,
            DueDate = bill.DueDate,
            Status = bill.Status,
            PaymentStatus = bill.PaymentStatus,
            TotalAmount = bill.TotalAmount,
            AmountPaid = bill.AmountPaid
        }).ToList();

        ViewData["Search"] = search;
        ViewData["PaymentStatus"] = paymentStatus;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int billId, CancellationToken cancellationToken)
    {
        var bill = await _vendorBillService.GetByIdAsync(billId, cancellationToken);
        if (bill is null)
        {
            return NotFound();
        }

        var amountDue = Math.Max(bill.TotalAmount - bill.AmountPaid, 0);
        if (amountDue <= 0)
        {
            TempData["StatusMessage"] = "This bill is already fully paid.";
            return RedirectToAction("Edit", "VendorBills", new { area = "Admin", id = billId });
        }

        var nextNumber = await _billPaymentService.PeekNextPaymentNumberAsync(cancellationToken);

        var model = new BillPaymentFormViewModel
        {
            VendorBillId = bill.VendorBillId,
            BillNumber = bill.BillNumber,
            VendorName = bill.Vendor?.Name ?? "-",
            AmountDue = amountDue,
            Amount = amountDue,
            PaymentDate = DateTime.UtcNow.Date,
            PaymentModes = BuildPaymentModeList(),
            PaymentNumberPreview = nextNumber
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BillPaymentFormViewModel model, CancellationToken cancellationToken)
    {
        var bill = await _vendorBillService.GetByIdAsync(model.VendorBillId, cancellationToken);
        if (bill is null)
        {
            return NotFound();
        }

        model.BillNumber = bill.BillNumber;
        model.VendorName = bill.Vendor?.Name ?? "-";
        model.AmountDue = Math.Max(bill.TotalAmount - bill.AmountPaid, 0);
        model.PaymentModes = BuildPaymentModeList();
        model.PaymentNumberPreview = await _billPaymentService.PeekNextPaymentNumberAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var payment = new BillPayment
            {
                VendorBillId = model.VendorBillId,
                PaymentDate = model.PaymentDate,
                Amount = model.Amount,
                PaymentMode = model.PaymentMode,
                Note = model.Note
            };

            await _billPaymentService.CreateAsync(payment, cancellationToken);
            TempData["StatusMessage"] = "Payment recorded.";
            return RedirectToAction("Edit", "VendorBills", new { area = "Admin", id = model.VendorBillId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    private static IEnumerable<SelectListItem> BuildPaymentModeList()
    {
        return Enum.GetValues<PaymentMode>()
            .Select(mode => new SelectListItem
            {
                Text = mode.ToString(),
                Value = mode.ToString()
            });
    }
}
