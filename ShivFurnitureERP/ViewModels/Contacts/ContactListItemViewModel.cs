using System;

namespace ShivFurnitureERP.ViewModels.Contacts;

public class ContactListItemViewModel
{
    public int ContactId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? City { get; set; }
}
