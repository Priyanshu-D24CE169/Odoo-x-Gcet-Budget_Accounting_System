using System;
using System.Collections.Generic;

namespace ShivFurnitureERP.ViewModels;

public class ProfileViewModel
{
    public string DisplayName { get; set; } = string.Empty;
    public string LoginId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
}
