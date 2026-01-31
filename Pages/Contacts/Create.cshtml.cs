using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Pages.Contacts;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Contact Contact { get; set; } = default!;

    public IActionResult OnGet()
    {
        Contact = new Contact
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

        Contact.CreatedDate = DateTime.UtcNow;
        _context.Contacts.Add(Contact);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Contact '{Contact.Name}' has been created successfully.";
        return RedirectToPage("./Index");
    }
}
