using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;
using System.ComponentModel.DataAnnotations;

namespace Budget_Accounting_System.Pages.Products;

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
    
    public List<SelectListItem> Categories { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200)]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int? CategoryId { get; set; }
        
        [Display(Name = "New Category Name")]
        public string? NewCategoryName { get; set; }
        
        [Required(ErrorMessage = "Sales price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Sales price must be zero or greater")]
        [Display(Name = "Sales Price")]
        public decimal SalesPrice { get; set; }
        
        [Required(ErrorMessage = "Purchase price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Purchase price must be zero or greater")]
        [Display(Name = "Purchase Price")]
        public decimal PurchasePrice { get; set; }
        
        [StringLength(50)]
        public string? Unit { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadCategoriesAsync();
        
        Input = new InputModel
        {
            SalesPrice = 0,
            PurchasePrice = 0
        };
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Handle create new category on-the-fly
        if (!string.IsNullOrWhiteSpace(Input.NewCategoryName))
        {
            // Check if category already exists
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == Input.NewCategoryName.Trim());
            
            if (existingCategory != null)
            {
                Input.CategoryId = existingCategory.Id;
                _logger.LogInformation("Using existing category: {CategoryName}", Input.NewCategoryName);
            }
            else
            {
                // Create new category
                var newCategory = new Category
                {
                    Name = Input.NewCategoryName.Trim(),
                    CreatedDate = DateTime.UtcNow
                };
                
                _context.Categories.Add(newCategory);
                await _context.SaveChangesAsync();
                
                Input.CategoryId = newCategory.Id;
                _logger.LogInformation("Created new category: {CategoryName} with ID: {CategoryId}", 
                    newCategory.Name, newCategory.Id);
                
                TempData["SuccessMessage"] = $"New category '{newCategory.Name}' created!";
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return Page();
        }

        if (!Input.CategoryId.HasValue)
        {
            ModelState.AddModelError("Input.CategoryId", "Please select a category or create a new one.");
            await LoadCategoriesAsync();
            return Page();
        }

        var product = new Product
        {
            Name = Input.Name,
            Description = Input.Description,
            CategoryId = Input.CategoryId.Value,
            SalesPrice = Input.SalesPrice,
            PurchasePrice = Input.PurchasePrice,
            Unit = Input.Unit,
            State = ProductState.New,
            CreatedDate = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product created: {ProductName} in category {CategoryId}", 
            product.Name, product.CategoryId);

        TempData["SuccessMessage"] = $"Product '{product.Name}' has been created successfully.";
        return RedirectToPage("./Index");
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();
        
        Categories = categories.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name
        }).ToList();
        
        // Add "Create New" option at the top
        Categories.Insert(0, new SelectListItem
        {
            Value = "",
            Text = "-- Select Category or Create New Below --"
        });
    }
}

