using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.Services;
using ShivFurnitureERP.ViewModels.Contacts;

namespace ShivFurnitureERP.Areas.Portal.Controllers;

[Area("Portal")]
[Authorize(Policy = "PortalOnly")]
public class ContactsController : Controller
{
    private readonly IContactService _contactService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ContactsController(IContactService contactService, UserManager<ApplicationUser> userManager)
    {
        _contactService = contactService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.ContactId is null)
        {
            return View(model: null);
        }

        var contact = await _contactService.GetByIdAsync(user.ContactId.Value, cancellationToken);
        if (contact is null)
        {
            return View(model: null);
        }

        var model = new PortalContactViewModel
        {
            Name = contact.Name,
            Email = contact.Email,
            Phone = contact.Phone,
            Street = contact.Street,
            City = contact.City,
            State = contact.State,
            Country = contact.Country,
            Pincode = contact.Pincode,
            Type = contact.Type,
            Tags = contact.ContactTags.Select(ct => ct.Tag.Name).ToArray(),
            ImagePath = contact.ImagePath,
            IsArchived = contact.IsArchived
        };

        return View(model);
    }
}
