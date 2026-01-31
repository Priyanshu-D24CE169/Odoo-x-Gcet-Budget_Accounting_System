using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.Products;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Product Product { get; set; } = default!;

    public IActionResult OnGet()
    {
        Product = new Product
        {
            IsActive = true
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Product.CreatedDate = DateTime.UtcNow;
        _context.Products.Add(Product);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Product '{Product.Name}' has been created successfully.";
        return RedirectToPage("./Index");
    }
}
