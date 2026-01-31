using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.Contacts;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<Contact> Contacts { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string? SearchString { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterType { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.Contacts.AsQueryable();

        if (!string.IsNullOrEmpty(SearchString))
        {
            query = query.Where(c => c.Name.Contains(SearchString) || c.Email.Contains(SearchString));
        }

        if (!string.IsNullOrEmpty(FilterType))
        {
            if (Enum.TryParse<ContactType>(FilterType, out var type))
            {
                query = query.Where(c => c.Type == type);
            }
        }

        Contacts = await query
            .OrderByDescending(c => c.IsActive)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostRestoreAsync(int id)
    {
        var contact = await _context.Contacts.FindAsync(id);
        if (contact == null)
        {
            return NotFound();
        }

        contact.IsActive = true;
        contact.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Contact '{contact.Name}' has been restored.";
        return RedirectToPage();
    }
}
