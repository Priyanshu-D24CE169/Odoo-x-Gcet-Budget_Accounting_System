using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.VendorBills;

public class PayModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public PayModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public VendorBill VendorBill { get; set; } = default!;

    [BindProperty]
    public Payment Payment { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var bill = await _context.VendorBills
            .Include(v => v.Vendor)
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
        var bill = await _context.VendorBills.FindAsync(Payment.VendorBillId);
        if (bill == null)
        {
            return NotFound();
        }

        VendorBill = bill;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var amountDue = bill.TotalAmount - bill.PaidAmount;
        if (Payment.Amount > amountDue)
        {
            ModelState.AddModelError("Payment.Amount", $"Payment amount cannot exceed amount due ({amountDue:C})");
            return Page();
        }

        if (Payment.Amount <= 0)
        {
            ModelState.AddModelError("Payment.Amount", "Payment amount must be greater than zero.");
            return Page();
        }

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
        bill.PaidAmount += Payment.Amount;
        bill.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Payment of {Payment.Amount:C} recorded successfully for {bill.BillNumber}.";
        return RedirectToPage("./Details", new { id = bill.Id });
    }
}

