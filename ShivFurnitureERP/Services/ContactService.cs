using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public class ContactService : IContactService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly IWebHostEnvironment _environment;
	private readonly IConfiguration _configuration;
    private readonly ILogger<ContactService> _logger;

	public ContactService(
		ApplicationDbContext dbContext,
		UserManager<ApplicationUser> userManager,
		IEmailNotificationService emailNotificationService,
		IWebHostEnvironment environment,
		IConfiguration configuration,
		ILogger<ContactService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _emailNotificationService = emailNotificationService;
        _environment = environment;
		_configuration = configuration;
        _logger = logger;
    }

    public async Task<List<Contact>> GetContactsAsync(string? searchTerm, bool includeArchived, CancellationToken cancellationToken)
    {
        var query = _dbContext.Contacts
            .Include(c => c.ContactTags)
                .ThenInclude(ct => ct.Tag)
            .AsNoTracking();

        if (!includeArchived)
        {
            query = query.Where(c => !c.IsArchived);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalized = searchTerm.Trim();
            query = query.Where(c =>
                c.Name.Contains(normalized) ||
                c.Email.Contains(normalized) ||
                (c.Phone != null && c.Phone.Contains(normalized)));
        }

        return await query
            .OrderByDescending(c => c.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public Task<Contact?> GetByIdAsync(int contactId, CancellationToken cancellationToken)
    {
        return _dbContext.Contacts
            .Include(c => c.ContactTags)
                .ThenInclude(ct => ct.Tag)
            .Include(c => c.PortalUser)
            .FirstOrDefaultAsync(c => c.ContactId == contactId, cancellationToken);
    }

    public async Task<Contact> CreateAsync(Contact contact, IEnumerable<string> tags, IFormFile? imageFile, CancellationToken cancellationToken)
    {
        contact.ImagePath = await SaveImageAsync(imageFile, contact.ImagePath, cancellationToken);
        contact.CreatedOn = DateTime.UtcNow;

        await SetTagsAsync(contact, tags, cancellationToken);

        _dbContext.Contacts.Add(contact);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await EnsurePortalAccountAsync(contact, cancellationToken);
        return contact;
    }

    public async Task UpdateAsync(Contact updatedContact, IEnumerable<string> tags, IFormFile? imageFile, CancellationToken cancellationToken)
    {
        var existingContact = await _dbContext.Contacts
            .Include(c => c.ContactTags)
                .ThenInclude(ct => ct.Tag)
            .Include(c => c.PortalUser)
            .FirstOrDefaultAsync(c => c.ContactId == updatedContact.ContactId, cancellationToken);

        if (existingContact is null)
        {
            throw new InvalidOperationException($"Contact {updatedContact.ContactId} was not found.");
        }

        existingContact.Name = updatedContact.Name;
        existingContact.Email = updatedContact.Email;
        existingContact.Phone = updatedContact.Phone;
        existingContact.Street = updatedContact.Street;
        existingContact.City = updatedContact.City;
        existingContact.State = updatedContact.State;
        existingContact.Country = updatedContact.Country;
        existingContact.Pincode = updatedContact.Pincode;
        existingContact.Type = updatedContact.Type;
        existingContact.IsArchived = updatedContact.IsArchived;

        existingContact.ImagePath = await SaveImageAsync(imageFile, existingContact.ImagePath, cancellationToken);

        await SetTagsAsync(existingContact, tags, cancellationToken);

        if (existingContact.PortalUser is not null)
        {
            existingContact.PortalUser.Email = existingContact.Email;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await EnsurePortalAccountAsync(existingContact, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult(false);
        }

        var normalized = email.Trim().ToLower();

        return _dbContext.Contacts
            .AsNoTracking()
            .AnyAsync(c => c.Email.ToLower() == normalized, cancellationToken);
    }

    private async Task EnsurePortalAccountAsync(Contact contact, CancellationToken cancellationToken)
    {
        if (!RequiresPortalAccount(contact.Type) || contact.PortalUser is not null)
        {
            return;
        }

        var loginId = await GenerateUniqueLoginIdAsync(contact.Name, cancellationToken);
        var tempPassword = PasswordGenerator.Generate();

		var portalUser = new ApplicationUser
        {
            UserName = loginId,
            Email = contact.Email,
            LoginId = loginId,
			EmailConfirmed = true,
			ContactId = contact.ContactId,
			FullName = contact.Name,
			MustChangePassword = true
        };

        var createResult = await _userManager.CreateAsync(portalUser, tempPassword);
        if (!createResult.Succeeded)
        {
            _logger.LogError("Failed to create portal user for Contact {ContactId}: {Errors}", contact.ContactId, string.Join(',', createResult.Errors.Select(e => e.Description)));
            return;
        }

        var roleResult = await _userManager.AddToRoleAsync(portalUser, "PortalUser");
        if (!roleResult.Succeeded)
        {
            _logger.LogWarning("Portal user role assignment failed for Contact {ContactId}: {Errors}", contact.ContactId, string.Join(',', roleResult.Errors.Select(e => e.Description)));
        }

        contact.PortalUser = portalUser;
        await _dbContext.SaveChangesAsync(cancellationToken);

		await _emailNotificationService.SendContactInviteAsync(contact.Email, loginId, tempPassword, GetPortalLoginUrl(), cancellationToken);
    }

	private string GetPortalLoginUrl()
	{
		var configured = _configuration["Portal:LoginUrl"];
		return string.IsNullOrWhiteSpace(configured) ? "/Portal/Account/Login" : configured;
	}

    private static bool RequiresPortalAccount(ContactType type)
        => type is ContactType.Customer or ContactType.Vendor or ContactType.Both;

    private async Task SetTagsAsync(Contact contact, IEnumerable<string> tags, CancellationToken cancellationToken)
    {
        contact.ContactTags ??= new List<ContactTag>();

        var tagNames = tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (contact.ContactTags.Count > 0)
        {
            _dbContext.ContactTags.RemoveRange(contact.ContactTags);
            contact.ContactTags.Clear();
        }

        if (tagNames.Count == 0)
        {
            return;
        }

        var existingTags = await _dbContext.Tags
            .Where(t => tagNames.Contains(t.Name))
            .ToListAsync(cancellationToken);

        foreach (var tagName in tagNames)
        {
            var tag = existingTags.FirstOrDefault(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
            if (tag is null)
            {
                tag = new Tag { Name = tagName };
                _dbContext.Tags.Add(tag);
            }

            contact.ContactTags.Add(new ContactTag
            {
                Contact = contact,
                Tag = tag
            });
        }
    }

    private async Task<string?> SaveImageAsync(IFormFile? imageFile, string? existingPath, CancellationToken cancellationToken)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            return existingPath;
        }

        var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", "contacts");
        Directory.CreateDirectory(uploadsRoot);

        if (!string.IsNullOrWhiteSpace(existingPath))
        {
            var sanitized = existingPath.TrimStart('/', '\\');
            var absoluteExisting = Path.Combine(_environment.WebRootPath, sanitized);
            if (File.Exists(absoluteExisting))
            {
                File.Delete(absoluteExisting);
            }
        }

        var fileExtension = Path.GetExtension(imageFile.FileName);
        var safeExtension = string.IsNullOrWhiteSpace(fileExtension) ? ".jpg" : fileExtension;
        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = File.Create(filePath))
        {
            await imageFile.CopyToAsync(stream, cancellationToken);
        }

        return $"/uploads/contacts/{fileName}";
    }

    private async Task<string> GenerateUniqueLoginIdAsync(string source, CancellationToken cancellationToken)
    {
        var prefix = new string(source.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (prefix.Length < 4)
        {
            prefix = prefix.PadRight(4, 'X');
        }

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var randomSegment = Random.Shared.Next(100000, 999999).ToString();
            var combined = prefix + randomSegment;
            if (combined.Length < 6)
            {
                combined = combined.PadRight(6, 'X');
            }

            if (combined.Length > 12)
            {
                combined = combined[..12];
            }

            var loginId = combined;

            var exists = await _dbContext.Users.AnyAsync(u => u.LoginId == loginId, cancellationToken);
            if (!exists)
            {
                return loginId;
            }
        }

        var fallback = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();
        return fallback;
    }
}

internal static class PasswordGenerator
{
    private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    private const string Special = "!@#$%^&*_-";
    private const string Digits = "0123456789";

    public static string Generate()
    {
        Span<char> buffer = stackalloc char[10];
        buffer[0] = Uppercase[Random.Shared.Next(Uppercase.Length)];
        buffer[1] = Lowercase[Random.Shared.Next(Lowercase.Length)];
        buffer[2] = Special[Random.Shared.Next(Special.Length)];
        buffer[3] = Digits[Random.Shared.Next(Digits.Length)];

        var allChars = Uppercase + Lowercase + Special + Digits;
        for (var i = 4; i < buffer.Length; i++)
        {
            buffer[i] = allChars[Random.Shared.Next(allChars.Length)];
        }

        var randomized = buffer.ToArray().OrderBy(_ => Random.Shared.Next()).ToArray();
        return new string(randomized);
    }
}
