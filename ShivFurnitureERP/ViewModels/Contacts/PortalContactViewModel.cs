using System;
using System.Collections.Generic;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.Contacts;

public class PortalContactViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? Pincode { get; set; }
    public ContactType Type { get; set; }
    public IReadOnlyCollection<string> Tags { get; set; } = Array.Empty<string>();
    public string? ImagePath { get; set; }
    public bool IsArchived { get; set; }
}
