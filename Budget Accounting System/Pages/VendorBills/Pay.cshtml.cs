using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using Budget_Accounting_System.Services;

namespace Budget_Accounting_System.Pages.VendorBills;

public class PayModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IBudgetActualService _budgetActualService;
    private readonly ILogger<PayModel> _logger;

    public PayModel(
        ApplicationDbContext context, 
        IBudgetActualService budgetActualService,
        ILogger<PayModel> logger)
    {
        _context = context;
        _budgetActualService = budgetActualService;
        _logger = logger;
    }

    public VendorBill VendorBill { get; set; } = default!;

    [BindProperty]
    public Payment Payment { get; set; } = default!;
    
    public List<Budget> AffectedBudgets { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var bill = await _context.VendorBills
            .Include(v => v.Vendor)
            .Include(v => v.Lines)
                .ThenInclude(l => l.AnalyticalAccount)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (bill == null)
        {
            return NotFound();
        }

        if (bill.Status != BillStatus.Posted)
        {
            TempData["ErrorMessage"] = "Can only record payment for posted bills.";
            return RedirectToPage("./Details", new { id });
        }

        var amountDue = bill.TotalAmount - bill.PaidAmount;
        if (amountDue <= 0)
        {
            TempData["ErrorMessage"] = "This bill is already fully paid.";
            return RedirectToPage("./Details", new { id });
        }

        // Get affected budgets
        AffectedBudgets = await _budgetActualService.GetAffectedBudgetsForVendorBillAsync(bill.Id);

        VendorBill = bill;
        Payment = new Payment
        {
            VendorBillId = bill.Id,
            PaymentDate = DateTime.Today,
            Type = PaymentType.Paid,
            Amount = amountDue
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var bill = await _context.VendorBills
            .Include(v => v.Vendor)
            .FirstOrDefaultAsync(v => v.Id == Payment.VendorBillId);
            
        if (bill == null)
        {
            return NotFound();
        }

        VendorBill = bill;

        if (!ModelState.IsValid)
        {
            AffectedBudgets = await _budgetActualService.GetAffectedBudgetsForVendorBillAsync(bill.Id);
            return Page();
        }

        var amountDue = bill.TotalAmount - bill.PaidAmount;
        if (Payment.Amount > amountDue)
        {
            ModelState.AddModelError("Payment.Amount", $"Payment amount cannot exceed amount due (?{amountDue:N2})");
            AffectedBudgets = await _budgetActualService.GetAffectedBudgetsForVendorBillAsync(bill.Id);
            return Page();
        }

        if (Payment.Amount <= 0)
        {
            ModelState.AddModelError("Payment.Amount", "Payment amount must be greater than zero.");
            AffectedBudgets = await _budgetActualService.GetAffectedBudgetsForVendorBillAsync(bill.Id);
            return Page();
        }

        try
        {
            // Generate payment number
            var lastPayment = await _context.Payments
                .OrderByDescending(p => p.PaymentNumber)
                .FirstOrDefaultAsync();

            string nextNumber;
            if (lastPayment != null && lastPayment.PaymentNumber.StartsWith("PAY"))
            {
                var numPart = lastPayment.PaymentNumber.Substring(3);
                if (int.TryParse(numPart, out int lastNum))
                {
                    nextNumber = $"PAY{(lastNum + 1):D4}";
                }
                else
                {
                    nextNumber = "PAY0001";
                }
            }
            else
            {
                nextNumber = "PAY0001";
            }

            // Create payment record
            Payment.PaymentNumber = nextNumber;
            Payment.Type = PaymentType.Paid;
            Payment.CreatedDate = DateTime.UtcNow;
            _context.Payments.Add(Payment);

            // Update bill paid amount
            var previousPaidAmount = bill.PaidAmount;
            bill.PaidAmount += Payment.Amount;
            bill.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // **UPDATE BUDGETS** - This is the key feature!
            await _budgetActualService.UpdateBudgetOnVendorBillPaymentAsync(bill.Id);

            _logger.LogInformation(
                "Payment {PaymentNumber} recorded for vendor bill {BillNumber}. " +
                "Amount: ?{Amount}, Previous Paid: ?{PreviousPaid}, New Paid: ?{NewPaid}, Total: ?{Total}. " +
                "Budgets updated.",
                Payment.PaymentNumber, bill.BillNumber, Payment.Amount, 
                previousPaidAmount, bill.PaidAmount, bill.TotalAmount);

            TempData["SuccessMessage"] = $"Payment of ?{Payment.Amount:N2} recorded successfully for {bill.BillNumber}. Budgets have been updated!";

            return RedirectToPage("./Details", new { id = bill.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording payment for vendor bill {BillId}", Payment.VendorBillId);
            ModelState.AddModelError("", "An error occurred while recording the payment. Please try again.");
            AffectedBudgets = await _budgetActualService.GetAffectedBudgetsForVendorBillAsync(bill.Id);
            return Page();
        }
    }
}


