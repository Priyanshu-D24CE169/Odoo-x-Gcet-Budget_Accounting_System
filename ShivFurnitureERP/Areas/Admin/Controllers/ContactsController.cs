using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.Services;
using ShivFurnitureERP.ViewModels.Contacts;

namespace ShivFurnitureERP.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class ContactsController : Controller
{
    private readonly IContactService _contactService;

    public ContactsController(IContactService contactService)
    {
        _contactService = contactService;
    }

    public async Task<IActionResult> Index(string? search, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var contacts = await _contactService.GetContactsAsync(search, includeArchived, cancellationToken);
        var model = contacts.Select(contact => new ContactListItemViewModel
        {
            ContactId = contact.ContactId,
            Name = contact.Name,
            Email = contact.Email,
            Phone = contact.Phone,
            Type = contact.Type.ToString(),
            Tags = string.Join(", ", contact.ContactTags.Select(ct => ct.Tag.Name)),
            IsArchived = contact.IsArchived,
            CreatedOn = contact.CreatedOn,
            City = contact.City
        }).ToList();

        ViewData["Search"] = search;
        ViewData["IncludeArchived"] = includeArchived;
        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new ContactFormViewModel
        {
            CreatedOn = DateTime.UtcNow
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContactFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var contact = MapToEntity(model);
        var tags = ParseTags(model.TagsInput);
        await _contactService.CreateAsync(contact, tags, model.ImageFile, cancellationToken);

        TempData["StatusMessage"] = "Contact created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var contact = await _contactService.GetByIdAsync(id, cancellationToken);
        if (contact is null)
        {
            return NotFound();
        }

        var model = MapToViewModel(contact);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ContactFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.ContactId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var contact = MapToEntity(model);
        var tags = ParseTags(model.TagsInput);
        await _contactService.UpdateAsync(contact, tags, model.ImageFile, cancellationToken);

        TempData["StatusMessage"] = "Contact updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private static Contact MapToEntity(ContactFormViewModel model)
    {
        return new Contact
        {
            ContactId = model.ContactId ?? 0,
            Name = model.Name,
            Email = model.Email,
            Phone = model.Phone,
            Street = model.Street,
            City = model.City,
            State = model.State,
            Country = model.Country,
            Pincode = model.Pincode,
            Type = model.Type,
            IsArchived = model.IsArchived
        };
    }

    private static ContactFormViewModel MapToViewModel(Contact contact)
    {
        return new ContactFormViewModel
        {
            ContactId = contact.ContactId,
            Name = contact.Name,
            Email = contact.Email,
            Phone = contact.Phone,
            Street = contact.Street,
            City = contact.City,
            State = contact.State,
            Country = contact.Country,
            Pincode = contact.Pincode,
            Type = contact.Type,
            IsArchived = contact.IsArchived,
            TagsInput = string.Join(", ", contact.ContactTags.Select(ct => ct.Tag.Name)),
            ExistingImagePath = contact.ImagePath,
            CreatedOn = contact.CreatedOn
        };
    }

    private static IEnumerable<string> ParseTags(string? tagsInput)
    {
        if (string.IsNullOrWhiteSpace(tagsInput))
        {
            return Array.Empty<string>();
        }

        return tagsInput
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
