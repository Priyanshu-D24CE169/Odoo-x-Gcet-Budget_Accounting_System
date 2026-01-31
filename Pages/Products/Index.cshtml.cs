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
        var query = _context.Products.AsQueryable();

        // Get all unique categories for filter dropdown
        Categories = await _context.Products
            .Where(p => !string.IsNullOrEmpty(p.Category))
            .Select(p => p.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        if (!string.IsNullOrEmpty(SearchString))
        {
            query = query.Where(p => p.Name.Contains(SearchString) || 
                                    (p.Description != null && p.Description.Contains(SearchString)));
        }

        if (!string.IsNullOrEmpty(FilterCategory))
        {
            query = query.Where(p => p.Category == FilterCategory);
        }

        Products = await query
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }
}
