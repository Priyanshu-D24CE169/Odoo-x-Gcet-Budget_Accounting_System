using Microsoft.AspNetCore.Http;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public interface IContactService
{
    Task<List<Contact>> GetContactsAsync(string? searchTerm, bool includeArchived, CancellationToken cancellationToken);
    Task<Contact?> GetByIdAsync(int contactId, CancellationToken cancellationToken);
    Task<Contact> CreateAsync(Contact contact, IEnumerable<string> tags, IFormFile? imageFile, CancellationToken cancellationToken);
    Task UpdateAsync(Contact contact, IEnumerable<string> tags, IFormFile? imageFile, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}
