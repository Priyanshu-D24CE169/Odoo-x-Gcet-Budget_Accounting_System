using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.Products;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<Product> Products { get; set; } = default!;
    public List<string> Categories { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string? SearchString { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterCategory { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.Products.Include(p => p.Category).AsQueryable();

        // Get all categories for filter dropdown
        Categories = await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync();

        if (!string.IsNullOrEmpty(SearchString))
        {
            query = query.Where(p => p.Name.Contains(SearchString) || 
                                    (p.Description != null && p.Description.Contains(SearchString)));
        }

        if (!string.IsNullOrEmpty(FilterCategory))
        {
            query = query.Where(p => p.Category.Name == FilterCategory);
        }

        Products = await query
            .OrderBy(p => p.State)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }
}
