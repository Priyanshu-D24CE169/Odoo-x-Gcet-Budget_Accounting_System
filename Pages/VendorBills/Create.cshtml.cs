using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using System.ComponentModel.DataAnnotations;

namespace Budget_Accounting_System.Pages.VendorBills;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(ApplicationDbContext context, ILogger<CreateModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public List<Contact> Vendors { get; set; } = new();
    public List<Product> Products { get; set; } = new();
    public List<AnalyticalAccount> AnalyticalAccounts { get; set; } = new();
    public List<PurchaseOrder> PurchaseOrders { get; set; } = new();

    public class InputModel
    {
        [Required]
        [Display(Name = "Vendor")]
        public int VendorId { get; set; }

        [Display(Name = "Purchase Order (Optional)")]
        public int? PurchaseOrderId { get; set; }

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
        [Required]
        public int ProductId { get; set; }

        public int? AnalyticalAccountId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int? purchaseOrderId = null)
    {
        await LoadDropdownsAsync();

        var lastBill = await _context.VendorBills
            .OrderByDescending(b => b.Id)
            .FirstOrDefaultAsync();

        var nextNumber = lastBill != null 
            ? int.Parse(lastBill.BillNumber.Split('/')[^1]) + 1 
            : 1;

        Input = new InputModel
        {
            BillNumber = $"BILL/{DateTime.Now.Year}/{nextNumber:D4}",
            BillDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            Lines = new List<LineItemInput>()
        };

        if (purchaseOrderId.HasValue)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == purchaseOrderId.Value);

            if (po != null)
            {
                Input.VendorId = po.VendorId;
                Input.PurchaseOrderId = purchaseOrderId.Value;
                Input.Reference = $"PO-{po.PONumber}";

                foreach (var line in po.Lines)
                {
                    Input.Lines.Add(new LineItemInput
                    {
                        ProductId = line.ProductId,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        AnalyticalAccountId = line.AnalyticalAccountId
                    });
                }
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        _logger.LogInformation("Vendor Bill POST - Action: {Action}, Lines Count: {Count}", 
            action, Input.Lines?.Count ?? 0);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid");
            await LoadDropdownsAsync();
            return Page();
        }

        if (Input.Lines == null || !Input.Lines.Any())
        {
            ModelState.AddModelError("", "Please add at least one product line");
            await LoadDropdownsAsync();
            return Page();
        }

        var exists = await _context.VendorBills
            .AnyAsync(b => b.BillNumber == Input.BillNumber);

        if (exists)
        {
            ModelState.AddModelError("Input.BillNumber", "This bill number already exists");
            await LoadDropdownsAsync();
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
                Status = action == "confirm" ? BillStatus.Posted : BillStatus.Draft,
                TotalAmount = 0,
                PaidAmount = 0,
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

            _logger.LogInformation("Vendor Bill {BillNumber} created successfully", vendorBill.BillNumber);

            TempData["SuccessMessage"] = $"Vendor Bill {vendorBill.BillNumber} {(action == "confirm" ? "posted" : "created")} successfully!";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vendor bill");
            ModelState.AddModelError("", "An error occurred. Please try again.");
            await LoadDropdownsAsync();
            return Page();
        }
    }

    private async Task LoadDropdownsAsync()
    {
        Vendors = await _context.Contacts
            .Where(c => c.IsActive && (c.Type == ContactType.Vendor || c.Type == ContactType.Both))
            .OrderBy(c => c.Name)
            .ToListAsync();

        Products = await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

        AnalyticalAccounts = await _context.AnalyticalAccounts
            .Where(a => a.IsActive)
            .OrderBy(a => a.Code)
            .ToListAsync();

        PurchaseOrders = await _context.PurchaseOrders
            .Where(p => p.Status == POStatus.Confirmed)
            .OrderByDescending(p => p.PODate)
            .ToListAsync();
    }

    public async Task<JsonResult> OnGetProductDetailsAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        
        if (product == null)
            return new JsonResult(new { success = false });

        return new JsonResult(new
        {
            success = true,
            unitPrice = product.UnitPrice,
            unit = product.Unit
        });
    }
}
