using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using System.ComponentModel.DataAnnotations;

namespace Budget_Accounting_System.Pages.PurchaseOrders;

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

    public class InputModel
    {
        [Required]
        [Display(Name = "Vendor")]
        public int VendorId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "PO Number")]
        public string PONumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "PO Date")]
        [DataType(DataType.Date)]
        public DateTime PODate { get; set; } = DateTime.Today;

        [StringLength(100)]
        [Display(Name = "Reference")]
        public string? Reference { get; set; }

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

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadDropdownsAsync();

        var lastPO = await _context.PurchaseOrders
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        var nextNumber = lastPO != null 
            ? int.Parse(lastPO.PONumber.Replace("PO", "")) + 1 
            : 1;

        Input = new InputModel
        {
            PONumber = $"PO{nextNumber:D4}",
            PODate = DateTime.Today,
            Lines = new List<LineItemInput>()
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        _logger.LogInformation("PO POST - Action: {Action}, Lines Count: {Count}", 
            action, Input.Lines?.Count ?? 0);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid");
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogWarning("Validation Error: {Error}", error.ErrorMessage);
            }
            await LoadDropdownsAsync();
            return Page();
        }

        if (Input.Lines == null || !Input.Lines.Any())
        {
            ModelState.AddModelError("", "Please add at least one product line");
            await LoadDropdownsAsync();
            return Page();
        }

        var exists = await _context.PurchaseOrders
            .AnyAsync(p => p.PONumber == Input.PONumber);

        if (exists)
        {
            ModelState.AddModelError("Input.PONumber", "This PO number already exists");
            await LoadDropdownsAsync();
            return Page();
        }

        try
        {
            var purchaseOrder = new PurchaseOrder
            {
                VendorId = Input.VendorId,
                PONumber = Input.PONumber,
                PODate = Input.PODate,
                Reference = Input.Reference,
                Status = action == "confirm" ? POStatus.Confirmed : POStatus.Draft,
                TotalAmount = 0,
                CreatedDate = DateTime.UtcNow
            };

            foreach (var lineInput in Input.Lines)
            {
                var line = new PurchaseOrderLine
                {
                    ProductId = lineInput.ProductId,
                    Quantity = lineInput.Quantity,
                    UnitPrice = lineInput.UnitPrice,
                    LineTotal = lineInput.Quantity * lineInput.UnitPrice,
                    AnalyticalAccountId = lineInput.AnalyticalAccountId
                };

                purchaseOrder.Lines.Add(line);
            }

            purchaseOrder.TotalAmount = purchaseOrder.Lines.Sum(l => l.LineTotal);

            _context.PurchaseOrders.Add(purchaseOrder);
            await _context.SaveChangesAsync();

            _logger.LogInformation("PO {PONumber} created successfully with {LineCount} lines", 
                purchaseOrder.PONumber, purchaseOrder.Lines.Count);

            // Auto-create Vendor Bill if PO is confirmed
            if (action == "confirm")
            {
                var billCreated = await CreateVendorBillFromPOAsync(purchaseOrder);
                if (billCreated)
                {
                    TempData["SuccessMessage"] = $"Purchase Order {purchaseOrder.PONumber} confirmed and Vendor Bill automatically created!";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Purchase Order {purchaseOrder.PONumber} confirmed successfully!";
                }
            }
            else
            {
                TempData["SuccessMessage"] = $"Purchase Order {purchaseOrder.PONumber} created successfully!";
            }
            
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase order");
            ModelState.AddModelError("", "An error occurred while creating the purchase order. Please try again.");
            await LoadDropdownsAsync();
            return Page();
        }
    }

    private async Task<bool> CreateVendorBillFromPOAsync(PurchaseOrder po)
    {
        try
        {
            // Generate bill number
            var lastBill = await _context.VendorBills
                .OrderByDescending(b => b.Id)
                .FirstOrDefaultAsync();

            var nextNumber = lastBill != null 
                ? int.Parse(lastBill.BillNumber.Split('/')[^1]) + 1 
                : 1;

            var billNumber = $"BILL/{DateTime.Now.Year}/{nextNumber:D4}";

            // Create vendor bill
            var vendorBill = new VendorBill
            {
                VendorId = po.VendorId,
                PurchaseOrderId = po.Id,
                BillNumber = billNumber,
                BillDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(30),
                Reference = $"Auto-created from {po.PONumber}",
                Notes = $"Automatically generated from Purchase Order {po.PONumber}",
                Status = BillStatus.Draft,
                TotalAmount = 0,
                PaidAmount = 0,
                PaymentStatus = PaymentStatus.NotPaid,
                CreatedDate = DateTime.UtcNow
            };

            // Copy all lines from PO
            foreach (var poLine in po.Lines)
            {
                var billLine = new VendorBillLine
                {
                    ProductId = poLine.ProductId,
                    Quantity = poLine.Quantity,
                    UnitPrice = poLine.UnitPrice,
                    LineTotal = poLine.LineTotal,
                    AnalyticalAccountId = poLine.AnalyticalAccountId
                };

                vendorBill.Lines.Add(billLine);
            }

            // Calculate total
            vendorBill.TotalAmount = vendorBill.Lines.Sum(l => l.LineTotal);

            _context.VendorBills.Add(vendorBill);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Auto-created Vendor Bill {BillNumber} from PO {PONumber}", 
                billNumber, po.PONumber);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-creating vendor bill from PO {PONumber}", po.PONumber);
            return false;
        }
    }

    private async Task LoadDropdownsAsync()
    {
        Vendors = await _context.Contacts
            .Where(c => c.State == ContactState.Confirmed && (c.Type == ContactType.Vendor || c.Type == ContactType.Both))
            .OrderBy(c => c.Name)
            .ToListAsync();

        Products = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.State == ProductState.Confirmed)
            .OrderBy(p => p.Name)
            .ToListAsync();

        AnalyticalAccounts = await _context.AnalyticalAccounts
            .Where(a => a.IsActive)
            .OrderBy(a => a.Code)
            .ToListAsync();
    }

    public async Task<JsonResult> OnGetProductDetailsAsync(int productId)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId);
        
        if (product == null)
            return new JsonResult(new { success = false });

        return new JsonResult(new
        {
            success = true,
            purchasePrice = product.PurchasePrice,
            unit = product.Unit,
            description = product.Description,
            categoryName = product.Category?.Name
        });
    }
}
