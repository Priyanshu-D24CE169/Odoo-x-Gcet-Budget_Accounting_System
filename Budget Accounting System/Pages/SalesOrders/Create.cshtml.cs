using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using System.ComponentModel.DataAnnotations;

namespace Budget_Accounting_System.Pages.SalesOrders;

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

    public List<Contact> Customers { get; set; } = new();
    public List<Product> Products { get; set; } = new();
    public List<AnalyticalAccount> AnalyticalAccounts { get; set; } = new();

    public class InputModel
    {
        [Required]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "SO Number")]
        public string SONumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "SO Date")]
        [DataType(DataType.Date)]
        public DateTime SODate { get; set; } = DateTime.Today;

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

        // Generate next SO number
        var lastSO = await _context.SalesOrders
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync();

        var nextNumber = lastSO != null 
            ? int.Parse(lastSO.SONumber.Replace("SO", "")) + 1 
            : 1;

        Input = new InputModel
        {
            SONumber = $"SO{nextNumber:D4}",
            SODate = DateTime.Today,
            Lines = new List<LineItemInput>()
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        _logger.LogInformation("Sales Order POST - Action: {Action}, Lines Count: {Count}", 
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

        // Validate lines
        if (Input.Lines == null || !Input.Lines.Any())
        {
            ModelState.AddModelError("", "Please add at least one product line");
            await LoadDropdownsAsync();
            return Page();
        }

        // Check for duplicate SO number
        var exists = await _context.SalesOrders
            .AnyAsync(s => s.SONumber == Input.SONumber);

        if (exists)
        {
            ModelState.AddModelError("Input.SONumber", "This SO number already exists");
            await LoadDropdownsAsync();
            return Page();
        }

        try
        {
            // Create Sales Order
            var salesOrder = new SalesOrder
            {
                CustomerId = Input.CustomerId,
                SONumber = Input.SONumber,
                SODate = Input.SODate,
                Reference = Input.Reference,
                Status = action == "confirm" ? SOStatus.Confirmed : SOStatus.Draft,
                TotalAmount = 0,
                CreatedDate = DateTime.UtcNow
            };

            // Add lines
            foreach (var lineInput in Input.Lines)
            {
                var line = new SalesOrderLine
                {
                    ProductId = lineInput.ProductId,
                    Quantity = lineInput.Quantity,
                    UnitPrice = lineInput.UnitPrice,
                    LineTotal = lineInput.Quantity * lineInput.UnitPrice,
                    AnalyticalAccountId = lineInput.AnalyticalAccountId
                };

                salesOrder.Lines.Add(line);
            }

            // Calculate totals
            salesOrder.TotalAmount = salesOrder.Lines.Sum(l => l.LineTotal);

            _context.SalesOrders.Add(salesOrder);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Sales Order {SONumber} created successfully with {LineCount} lines", 
                salesOrder.SONumber, salesOrder.Lines.Count);

            // Auto-create Customer Invoice if SO is confirmed
            if (action == "confirm")
            {
                var invoiceCreated = await CreateCustomerInvoiceFromSOAsync(salesOrder);
                if (invoiceCreated)
                {
                    TempData["SuccessMessage"] = $"Sales Order {salesOrder.SONumber} confirmed and Customer Invoice automatically created!";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Sales Order {salesOrder.SONumber} confirmed successfully!";
                }
            }
            else
            {
                TempData["SuccessMessage"] = $"Sales Order {salesOrder.SONumber} created successfully!";
            }
            
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sales order");
            ModelState.AddModelError("", "An error occurred while creating the sales order. Please try again.");
            await LoadDropdownsAsync();
            return Page();
        }
    }

    private async Task<bool> CreateCustomerInvoiceFromSOAsync(SalesOrder so)
    {
        try
        {
            // Generate invoice number
            var lastInvoice = await _context.CustomerInvoices
                .OrderByDescending(i => i.Id)
                .FirstOrDefaultAsync();

            var nextNumber = lastInvoice != null 
                ? int.Parse(lastInvoice.InvoiceNumber.Split('/')[^1]) + 1 
                : 1;

            var invoiceNumber = $"INV/{DateTime.Now.Year}/{nextNumber:D4}";

            // Create customer invoice
            var customerInvoice = new CustomerInvoice
            {
                CustomerId = so.CustomerId,
                SalesOrderId = so.Id,
                InvoiceNumber = invoiceNumber,
                InvoiceDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(30),
                Reference = $"Auto-created from {so.SONumber}",
                Notes = $"Automatically generated from Sales Order {so.SONumber}",
                Status = InvoiceStatus.Draft,
                TotalAmount = 0,
                PaidAmount = 0,
                PaymentStatus = PaymentStatus.NotPaid,
                CreatedDate = DateTime.UtcNow
            };

            // Copy all lines from SO
            foreach (var soLine in so.Lines)
            {
                var invoiceLine = new CustomerInvoiceLine
                {
                    ProductId = soLine.ProductId,
                    Quantity = soLine.Quantity,
                    UnitPrice = soLine.UnitPrice,
                    LineTotal = soLine.LineTotal,
                    AnalyticalAccountId = soLine.AnalyticalAccountId
                };

                customerInvoice.Lines.Add(invoiceLine);
            }

            // Calculate total
            customerInvoice.TotalAmount = customerInvoice.Lines.Sum(l => l.LineTotal);

            _context.CustomerInvoices.Add(customerInvoice);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Auto-created Customer Invoice {InvoiceNumber} from SO {SONumber}", 
                invoiceNumber, so.SONumber);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-creating customer invoice from SO {SONumber}", so.SONumber);
            return false;
        }
    }

    private async Task LoadDropdownsAsync()
    {
        Customers = await _context.Contacts
            .Where(c => c.State == ContactState.Confirmed && (c.Type == ContactType.Customer || c.Type == ContactType.Both))
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
            salesPrice = product.SalesPrice,
            unit = product.Unit,
            description = product.Description,
            categoryName = product.Category?.Name
        });
    }
}
