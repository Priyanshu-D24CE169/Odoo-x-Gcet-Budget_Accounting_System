using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using System.ComponentModel.DataAnnotations;

namespace Budget_Accounting_System.Pages.CustomerInvoices;

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
    public List<SalesOrder> SalesOrders { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Customer is required")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        [Display(Name = "Sales Order (Optional)")]
        public int? SalesOrderId { get; set; }

        [Required(ErrorMessage = "Invoice number is required")]
        [StringLength(50)]
        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Invoice date is required")]
        [Display(Name = "Invoice Date")]
        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Due date is required")]
        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

        [Display(Name = "Reference")]
        [StringLength(100)]
        public string? Reference { get; set; }

        [Display(Name = "Notes")]
        [StringLength(500)]
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

    public async Task<IActionResult> OnGetAsync(int? salesOrderId = null)
    {
        await LoadDropdownsAsync();

        var lastInvoice = await _context.CustomerInvoices
            .OrderByDescending(i => i.Id)
            .FirstOrDefaultAsync();

        var nextNumber = lastInvoice != null 
            ? int.Parse(lastInvoice.InvoiceNumber.Replace("INV-", "")) + 1 
            : 1;

        Input = new InputModel
        {
            InvoiceNumber = $"INV-{nextNumber:D6}",
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            Lines = new List<LineItemInput>()
        };

        if (salesOrderId.HasValue)
        {
            var salesOrder = await _context.SalesOrders
                .Include(so => so.Lines)
                .FirstOrDefaultAsync(so => so.Id == salesOrderId.Value);

            if (salesOrder != null)
            {
                Input.CustomerId = salesOrder.CustomerId;
                Input.SalesOrderId = salesOrderId.Value;
                Input.Reference = $"SO-{salesOrder.SONumber}";

                foreach (var line in salesOrder.Lines)
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

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        if (Input.Lines == null || !Input.Lines.Any())
        {
            ModelState.AddModelError("", "At least one product line is required");
            await LoadDropdownsAsync();
            return Page();
        }

        if (Input.DueDate < Input.InvoiceDate)
        {
            ModelState.AddModelError("Input.DueDate", "Due date cannot be before invoice date");
            await LoadDropdownsAsync();
            return Page();
        }

        var exists = await _context.CustomerInvoices
            .AnyAsync(i => i.InvoiceNumber == Input.InvoiceNumber);

        if (exists)
        {
            ModelState.AddModelError("Input.InvoiceNumber", "This invoice number already exists");
            await LoadDropdownsAsync();
            return Page();
        }

        try
        {
            var invoice = new CustomerInvoice
            {
                CustomerId = Input.CustomerId,
                SalesOrderId = Input.SalesOrderId,
                InvoiceNumber = Input.InvoiceNumber,
                InvoiceDate = Input.InvoiceDate,
                DueDate = Input.DueDate,
                Reference = Input.Reference,
                Notes = Input.Notes,
                TotalAmount = 0,
                PaidAmount = 0,
                CreatedDate = DateTime.UtcNow
            };

            foreach (var lineInput in Input.Lines)
            {
                var line = new CustomerInvoiceLine
                {
                    ProductId = lineInput.ProductId,
                    Quantity = lineInput.Quantity,
                    UnitPrice = lineInput.UnitPrice,
                    LineTotal = lineInput.Quantity * lineInput.UnitPrice,
                    AnalyticalAccountId = lineInput.AnalyticalAccountId
                };

                invoice.Lines.Add(line);
            }

            invoice.TotalAmount = invoice.Lines.Sum(l => l.LineTotal);

            _context.CustomerInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Customer Invoice {InvoiceNumber} created", invoice.InvoiceNumber);

            TempData["SuccessMessage"] = $"Customer Invoice {invoice.InvoiceNumber} created successfully!";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer invoice");
            ModelState.AddModelError("", "An error occurred. Please try again.");
            await LoadDropdownsAsync();
            return Page();
        }
    }

    private async Task LoadDropdownsAsync()
    {
        Customers = await _context.Contacts
            .Where(c => c.State == ContactState.Confirmed && (c.Type == ContactType.Customer || c.Type == ContactType.Both))
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

        SalesOrders = await _context.SalesOrders
            .Where(so => so.Status == SOStatus.Confirmed)
            .OrderByDescending(so => so.SODate)
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
