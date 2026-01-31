using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Budget_Accounting_System.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(12, MinimumLength = 6)]
    public string LoginId { get; set; } = string.Empty;
    
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int? ContactId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation property
    public Contact? Contact { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string PortalUser = "PortalUser";
    
    public static readonly string[] AllRoles = { Admin, PortalUser };
}

public static class AreaNames
{
    public const string Admin = "Admin";
    public const string Portal = "Portal";
}
