using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using System.ComponentModel.DataAnnotations;

namespace Budget_Accounting_System.Pages.VendorBills;

[Authorize(Roles = "Admin")]
public class CreateFromPOModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CreateFromPOModel> _logger;

    public CreateFromPOModel(ApplicationDbContext context, ILogger<CreateFromPOModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public PurchaseOrder? PurchaseOrder { get; set; }

    public class InputModel
    {
        [Required]
        public int VendorId { get; set; }

        public int PurchaseOrderId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Bill Number")]
        public string BillNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Bill Date")]
        [DataType(DataType.Date)]
        public DateTime BillDate { get; set; } = DateTime.Today;

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [StringLength(100)]
        [Display(Name = "Reference")]
        public string? Reference { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        public List<LineItemInput> Lines { get; set; } = new();
    }

    public class LineItemInput
    {
        public int ProductId { get; set; }
        public int? AnalyticalAccountId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;
    }

    public async Task<IActionResult> OnGetAsync(int? poId)
    {
        if (poId == null)
        {
            TempData["ErrorMessage"] = "Purchase Order ID is required.";
            return RedirectToPage("/PurchaseOrders/Index");
        }

        var po = await _context.PurchaseOrders
            .Include(p => p.Vendor)
            .Include(p => p.Lines)
                .ThenInclude(l => l.Product)
            .Include(p => p.Lines)
                .ThenInclude(l => l.AnalyticalAccount)
            .FirstOrDefaultAsync(p => p.Id == poId.Value);

        if (po == null)
        {
            TempData["ErrorMessage"] = "Purchase Order not found.";
            return RedirectToPage("/PurchaseOrders/Index");
        }

        if (po.Status != POStatus.Confirmed)
        {
            TempData["ErrorMessage"] = "Only confirmed purchase orders can be billed.";
            return RedirectToPage("/PurchaseOrders/Details", new { id = poId });
        }

        PurchaseOrder = po;

        // Generate bill number
        var lastBill = await _context.VendorBills
            .OrderByDescending(b => b.Id)
            .FirstOrDefaultAsync();

        var nextNumber = lastBill != null 
            ? int.Parse(lastBill.BillNumber.Split('/')[^1]) + 1 
            : 1;

        Input = new InputModel
        {
            VendorId = po.VendorId,
            PurchaseOrderId = po.Id,
            BillNumber = $"BILL/{DateTime.Now.Year}/{nextNumber:D4}",
            BillDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            Reference = $"PO-{po.PONumber}",
            Notes = $"Bill created from Purchase Order {po.PONumber}",
            Lines = po.Lines.Select(l => new LineItemInput
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                AnalyticalAccountId = l.AnalyticalAccountId
            }).ToList()
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        _logger.LogInformation("Creating Vendor Bill from PO - Action: {Action}, PO ID: {POId}", 
            action, Input.PurchaseOrderId);

        // Reload PurchaseOrder for display
        PurchaseOrder = await _context.PurchaseOrders
            .Include(p => p.Vendor)
            .Include(p => p.Lines)
                .ThenInclude(l => l.Product)
            .Include(p => p.Lines)
                .ThenInclude(l => l.AnalyticalAccount)
            .FirstOrDefaultAsync(p => p.Id == Input.PurchaseOrderId);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid");
            return Page();
        }

        if (Input.Lines == null || !Input.Lines.Any())
        {
            ModelState.AddModelError("", "At least one line item is required.");
            return Page();
        }

        // Check if bill number already exists
        var exists = await _context.VendorBills
            .AnyAsync(b => b.BillNumber == Input.BillNumber);

        if (exists)
        {
            ModelState.AddModelError("Input.BillNumber", "This bill number already exists.");
            return Page();
        }

        try
        {
            var vendorBill = new VendorBill
            {
                VendorId = Input.VendorId,
                PurchaseOrderId = Input.PurchaseOrderId,
                BillNumber = Input.BillNumber,
                BillDate = Input.BillDate,
                DueDate = Input.DueDate,
                Reference = Input.Reference,
                Notes = Input.Notes,
                Status = action == "post" ? BillStatus.Posted : BillStatus.Draft,
                TotalAmount = 0,
                PaidAmount = 0,
                PaymentStatus = PaymentStatus.NotPaid,
                CreatedDate = DateTime.UtcNow
            };

            foreach (var lineInput in Input.Lines)
            {
                var line = new VendorBillLine
                {
                    ProductId = lineInput.ProductId,
                    Quantity = lineInput.Quantity,
                    UnitPrice = lineInput.UnitPrice,
                    LineTotal = lineInput.Quantity * lineInput.UnitPrice,
                    AnalyticalAccountId = lineInput.AnalyticalAccountId
                };

                vendorBill.Lines.Add(line);
            }

            vendorBill.TotalAmount = vendorBill.Lines.Sum(l => l.LineTotal);

            _context.VendorBills.Add(vendorBill);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Vendor Bill {BillNumber} created from PO {PONumber}", 
                vendorBill.BillNumber, PurchaseOrder?.PONumber);

            TempData["SuccessMessage"] = $"Vendor Bill {vendorBill.BillNumber} created successfully from PO {PurchaseOrder?.PONumber}!";
            return RedirectToPage("/VendorBills/Details", new { id = vendorBill.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vendor bill from PO");
            ModelState.AddModelError("", "An error occurred while creating the bill. Please try again.");
            return Page();
        }
    }
}
