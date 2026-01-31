using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.Contacts;

public class ArchiveModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ArchiveModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Contact Contact { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var contact = await _context.Contacts.FirstOrDefaultAsync(m => m.Id == id);
        if (contact == null)
        {
            return NotFound();
        }

        Contact = contact;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var contact = await _context.Contacts.FindAsync(Contact.Id);
        if (contact == null)
        {
            return NotFound();
        }

        contact.IsActive = false;
        contact.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Contact '{contact.Name}' has been archived.";
        return RedirectToPage("./Index");
    }
}
